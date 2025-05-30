namespace TicTacToe.Tournament.Server
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Microsoft.AspNetCore.SignalR;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.Interfaces;
    using TicTacToe.Tournament.Server.Hubs;

    public class GameServer : IGameServer
    {
        private readonly Models.Tournament _tournament;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly Dictionary<Guid, IPlayerBot> _players = new();
        private readonly Action<Guid, MatchScore> _updateLeaderboard;
        private readonly Action _saveState;
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>> _pendingMoves = new();
        private readonly TimeSpan _walkoverTimeout;
        public Models.Tournament Tournament => _tournament;

        public GameServer(
            Models.Tournament tournament,
            IHubContext<TournamentHub> hubContext,
            Action<Guid, MatchScore> updateLeaderboard,
            Action saveState,
            TimeSpan walkoverTimeout)
        {
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _updateLeaderboard = updateLeaderboard ?? throw new ArgumentNullException(nameof(updateLeaderboard));
            _walkoverTimeout = walkoverTimeout;
            _saveState = saveState;
        }

        public static TimeSpan DefaultTimeout => TimeSpan.FromMinutes(1);

        public IReadOnlyDictionary<Guid, IPlayerBot> RegisteredPlayers => _players;

        public void RegisterPlayer(IPlayerBot player)
        {
            _players[player.Id] = player;
            _tournament.RegisterPlayer(player.Id, player.Name);

            InitializeLeaderboard();
            GenerateMatches();
        }

        public void InitializeLeaderboard()
        {
            _tournament.InitializeLeaderboard();
        }

        public ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>> GetPendingMoves() => _pendingMoves;

        public void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>> moves)
        {
            _pendingMoves.Clear();
            foreach (var entry in moves)
            {
                _pendingMoves[entry.Key] = new ConcurrentQueue<(byte, byte)>(entry.Value);
            }
        }

        public async Task StartTournamentAsync(Models.Tournament tournament)
        {
            InitializeLeaderboard();
            GenerateMatches();

            await Task.WhenAll(
                RunMatches(tournament)
            );

            SaveState();
        }

        public void SubmitMove(Guid playerId, byte row, byte col)
        {
            _pendingMoves.AddOrUpdate(playerId, new ConcurrentQueue<(byte, byte)>(new[] { (row, col) }), (key, oldValue) =>
            {
                oldValue.Enqueue((row, col));
                return oldValue;
            });
        }

        public IPlayerBot? GetBotById(Guid playerId) =>
            _players.TryGetValue(playerId, out var bot) ? bot : null;

        public void GenerateMatches()
        {
            _tournament.Matches.Clear();

            var ids = _tournament.RegisteredPlayers.Keys.ToList();
            for (var i = 0; i < ids.Count; i++)
            {
                for (var j = 0; j < ids.Count; j++)
                {
                    if (i == j) { continue; }

                    for (var r = 0; r < _tournament.MatchRepetition; r++)
                    {
                        var match = new Models.Match(ids[i], ids[j])
                        {
                            Status = MatchStatus.Planned
                        };
                        _tournament.Matches.Add(match);
                    }
                }
            }
        }

        private async Task<GameResult> PlayMatchAsync(Models.Match match)
        {
            match.Start();

            var turn = 0;
            var playerX = _players[match.PlayerA];
            var playerO = _players[match.PlayerB];
            var players = new Dictionary<Mark, IPlayerBot> { [Mark.X] = playerX, [Mark.O] = playerO };

            playerX.OnMatchStarted(match.Id, playerX.Id, playerO.Id, Mark.X, true);

            playerO.OnMatchStarted(match.Id, playerO.Id, playerX.Id, Mark.O, false);

            await OnTournamentUpdated(_tournament.Id);

            while (match.Status == MatchStatus.Ongoing)
            {
                var mark = (turn % 2 == 0) ? Mark.X : Mark.O;
                var currentPlayer = players[mark];
                var opponent = players[mark == Mark.X ? Mark.O : Mark.X];

                match.CurrentTurn = currentPlayer.Id;

                await OnYourTurn(match.Id, currentPlayer.Id, match.Board.State);

                (byte row, byte col)? move = null;
                try
                {
                    move = await WaitForMoveAsync(currentPlayer.Id, _walkoverTimeout.TotalMilliseconds);
                }
                catch (TimeoutException)
                {
                    match.Walkover(mark);

                    var winnerId = match.WinnerMark == Mark.X ? match.PlayerA : match.PlayerB;
                    var walkoverId = winnerId == match.PlayerA ? match.PlayerB : match.PlayerA;
                    _updateLeaderboard(winnerId, MatchScore.Win);
                    _updateLeaderboard(walkoverId, MatchScore.Walkover);

                    await OnTournamentUpdated(_tournament.Id);

                    return new GameResult(match);
                }

                if (move.HasValue)
                {
                    if (match.Board.IsValidMove(move.Value.row, move.Value.col))
                    {
                        match.Board.ApplyMove(move.Value.row, move.Value.col, mark);

                        await Task.WhenAll(
                            OnOpponentMoved(match.Id, opponent.Id, move.Value.row, move.Value.col),
                            OnReceiveBoard(match.Id, match.Board.GetState())
                        );
                        turn++;
                    }
                }

                if (match.Board.IsGameOver())
                {
                    var winnerMark = match.Board.GetWinner();
                    if (winnerMark == null || winnerMark == Mark.Empty)
                    {
                        _updateLeaderboard(match.PlayerA, MatchScore.Draw);
                        _updateLeaderboard(match.PlayerB, MatchScore.Draw);
                        match.Draw();
                    }
                    else
                    {
                        match.WinnerMark = winnerMark.Value;
                        var winnerId = winnerMark == Mark.X ? match.PlayerA : match.PlayerB;
                        var looserId = winnerMark == Mark.X ? match.PlayerB : match.PlayerA;
                        _updateLeaderboard(winnerId, MatchScore.Win);
                        _updateLeaderboard(looserId, MatchScore.Lose);
                        match.Finish();
                    }

                    break;
                }

                await Task.Delay(100);
            }

            var gameResult = new GameResult(match);
            await Task.WhenAll(
                OnMatchEnded(gameResult, match.PlayerA),
                OnMatchEnded(gameResult, match.PlayerB),
                OnTournamentUpdated(_tournament.Id)
            );

            return gameResult;
        }

        private async Task<(byte row, byte col)?> WaitForMoveAsync(Guid playerId, double timeoutInMs)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[GameServer] Waiting for player {playerId} to submit the movement.");

            while (sw.ElapsedMilliseconds < timeoutInMs)
            {
                if (_pendingMoves.TryGetValue(playerId, out var queue) && queue.TryDequeue(out var move))
                {
                    return move;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException("You lose if not responding in time.");
        }

        private async Task RunMatches(Models.Tournament tournament)
        {
            foreach (var match in tournament.Matches)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await RunMatchAsync(tournament.Id, match);
            }

            tournament.Finish();

            await OnTournamentUpdated(tournament.Id);
        }

        private async Task RunMatchAsync(Guid tournamentId, Models.Match match)
        {
            match.Start();

            await PlayMatchAsync(match);

            match.Finish();

            await OnTournamentUpdated(tournamentId);
        }

        private void SaveState()
        {
            try
            {
                _saveState?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task OnMatchEnded(GameResult gameResult, Guid playerToNotify)
        {
            SaveState();
            await _hubContext
                .Clients
                .User(playerToNotify.ToString())
                .SendAsync("OnMatchEnded", gameResult);
        }

        private Task OnReceiveBoard(Guid matchId, Mark[][] board) =>
            _hubContext.Clients.Group(_tournament.Id.ToString()).SendAsync("OnReceiveBoard", matchId, board);

        private Task OnOpponentMoved(Guid matchId, Guid opponentId, int row, int col) =>
            _hubContext.Clients.User(opponentId.ToString()).SendAsync("OnOpponentMoved", matchId, row, col);

        private Task OnYourTurn(Guid matchId, Guid playerId, Mark[][] board) =>
            _hubContext.Clients.User(playerId.ToString()).SendAsync("OnYourTurn", matchId, playerId, board);

        private async Task OnTournamentUpdated(Guid tournamentId)
        {
            SaveState();
            await _hubContext
                .Clients
                .Group(_tournament.Id.ToString())
                .SendAsync("OnTournamentUpdated", tournamentId);
        }
    }
}
