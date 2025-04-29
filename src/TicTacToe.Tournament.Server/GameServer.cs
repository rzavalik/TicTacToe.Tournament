namespace TicTacToe.Tournament.Server
{
    using Microsoft.AspNetCore.SignalR;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.Interfaces;
    using TicTacToe.Tournament.Server.Hubs;

    public class GameServer : IGameServer
    {
        private readonly Models.Tournament _tournament;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly Dictionary<Guid, IPlayerBot> _players = new();
        private readonly Action<Guid, MatchScore> _updateLeaderboard;
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> _pendingMoves = new();

        public Models.Tournament Tournament => _tournament;

        public GameServer(
            Models.Tournament tournament,
            IHubContext<TournamentHub> hubContext,
            Action<Guid, MatchScore> updateLeaderboard)
        {
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _updateLeaderboard = updateLeaderboard ?? throw new ArgumentNullException(nameof(updateLeaderboard));
        }

        public IReadOnlyDictionary<Guid, IPlayerBot> RegisteredPlayers => _players;

        public void RegisterPlayer(IPlayerBot player)
        {
            _players[player.Id] = player;
            _tournament.RegisteredPlayers[player.Id] = player.Name;
            InitializeLeaderboard();
            GenerateMatches();
        }

        public void InitializeLeaderboard()
        {
            _tournament.InitializeLeaderboard();
        }

        public ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> GetPendingMoves() => _pendingMoves;

        public void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> moves)
        {
            _pendingMoves.Clear();
            foreach (var entry in moves)
                _pendingMoves[entry.Key] = new ConcurrentQueue<(int, int)>(entry.Value);
        }

        public async Task StartTournamentAsync(Models.Tournament tournament)
        {
            InitializeLeaderboard();
            GenerateMatches();

            await Task.WhenAll(
                RunMatches(tournament)
            );
        }

        public void SubmitMove(Guid playerId, int row, int col)
        {
            _pendingMoves.AddOrUpdate(playerId, new ConcurrentQueue<(int, int)>(new[] { (row, col) }), (key, oldValue) =>
            {
                oldValue.Enqueue((row, col));
                return oldValue;
            });
        }

        public IPlayerBot? GetBotById(Guid playerId) => _players.TryGetValue(playerId, out var bot) ? bot : null;

        public void GenerateMatches()
        {
            _tournament.Matches = new List<Match>();

            var ids = _tournament.RegisteredPlayers.Keys.ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = 0; j < ids.Count; j++)
                {
                    if (i == j) continue;
                    for (int r = 0; r < _tournament.MatchRepetition; r++)
                    {
                        var match = new Models.Match
                        {
                            PlayerA = ids[i],
                            PlayerB = ids[j],
                            Status = MatchStatus.Planned
                        };
                        match.Board = Board.Empty;
                        _tournament.Matches.Add(match);
                    }
                }
            }
        }

        private async Task<GameResult> PlayMatchAsync(Models.Match match)
        {
            match.Status = MatchStatus.Ongoing;
            match.StartTime = DateTime.UtcNow;
            match.Board = Board.Empty;
            var board = new Board();

            var turn = 0;
            var playerX = _players[match.PlayerA];
            var playerO = _players[match.PlayerB];
            var players = new Dictionary<Mark, IPlayerBot> { [Mark.X] = playerX, [Mark.O] = playerO };

            await Task.WhenAll(
                OnMatchStarted(match, playerX.Id, playerO.Id, Mark.X, true),
                OnMatchStarted(match, playerO.Id, playerX.Id, Mark.O, false),
                OnTournamentUpdated(_tournament.Id)
            );

            while (match.Status == MatchStatus.Ongoing)
            {
                var mark = (turn % 2 == 0) ? Mark.X : Mark.O;
                var currentPlayer = players[mark];
                var opponent = players[mark == Mark.X ? Mark.O : Mark.X];

                match.CurrentTurn = currentPlayer.Id;

                await OnYourTurn(match.Id, currentPlayer.Id, match.Board);

                (int row, int col)? move = null;
                try
                {
                    move = await WaitForMoveAsync(currentPlayer.Id, 60000);
                }
                catch (TimeoutException)
                {
                    match.Status = MatchStatus.Finished;
                    match.WinnerMark = mark == Mark.X ? Mark.O : Mark.X;

                    _updateLeaderboard(match.WinnerMark == Mark.X ? match.PlayerA : match.PlayerB, MatchScore.Win);
                    _updateLeaderboard(match.WinnerMark == Mark.X ? match.PlayerB : match.PlayerA, MatchScore.Walkover);

                    await OnTournamentUpdated(_tournament.Id);

                    return new GameResult
                    {
                        MatchId = match.Id,
                        WinnerId = match.PlayerA == currentPlayer.Id ? match.PlayerB : match.PlayerA,
                        Board = match.Board,
                        IsDraw = false
                    };
                }

                if (move.HasValue)
                {
                    if (board.IsValidMove(move.Value.row, move.Value.col))
                    {
                        board.ApplyMove(move.Value.row, move.Value.col, mark);
                        match.Board = board.GetState();

                        await Task.WhenAll(
                            OnOpponentMoved(match.Id, opponent.Id, move.Value.row, move.Value.col),
                            OnReceiveBoard(match.Id, board.GetState())
                        );
                        turn++;
                    }
                }

                if (board.IsGameOver())
                {
                    match.WinnerMark = board.GetWinner();
                    break;
                }

                await Task.Delay(100);
            }

            match.Status = MatchStatus.Finished;
            match.EndTime = DateTime.UtcNow;

            var gameResult = new GameResult
            {
                MatchId = match.Id,
                WinnerId = match.WinnerMark.HasValue ? players[match.WinnerMark.Value].Id : null,
                Board = match.Board,
                IsDraw = !match.WinnerMark.HasValue
            };

            if (gameResult.WinnerId != null)
                _updateLeaderboard(gameResult.WinnerId.Value, MatchScore.Win);
            else
            {
                _updateLeaderboard(match.PlayerA, MatchScore.Draw);
                _updateLeaderboard(match.PlayerB, MatchScore.Draw);
            }

            await Task.WhenAll(
                OnMatchEnded(gameResult, match.PlayerA),
                OnMatchEnded(gameResult, match.PlayerB)
            );

            return gameResult;
        }

        private async Task<(int row, int col)?> WaitForMoveAsync(Guid playerId, int timeoutInMs)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[GameServer] Waiting for player {playerId} to submit the movement.");

            while (sw.ElapsedMilliseconds < timeoutInMs)
            {
                if (_pendingMoves.TryGetValue(playerId, out var queue) && queue.TryDequeue(out var move))
                    return move;

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

            tournament.Status = TournamentStatus.Finished;
            tournament.EndTime = DateTime.UtcNow;

            await OnTournamentUpdated(tournament);
        }

        private async Task RunMatchAsync(Guid tournamentId, Models.Match match)
        {
            match.Status = MatchStatus.Ongoing;
            match.StartTime = DateTime.UtcNow;
            match.Board = Board.Empty;

            var result = await PlayMatchAsync(match);

            match.EndTime = DateTime.UtcNow;
            match.Status = MatchStatus.Finished;

            await OnTournamentUpdated(tournamentId);
        }


        private Task OnMatchStarted(Models.Match match, Guid playerId, Guid opponentId, Mark playerMark, bool yourTurn) =>
            _hubContext.Clients.User(playerId.ToString()).SendAsync("OnMatchStarted", match.Id, playerId, opponentId, playerMark.ToString("G"), yourTurn);

        private Task OnMatchEnded(GameResult gameResult, Guid playerToNotify) =>
            _hubContext.Clients.User(playerToNotify.ToString()).SendAsync("OnMatchEnded", gameResult);

        private Task OnReceiveBoard(Guid matchId, Mark[][] board) =>
            _hubContext.Clients.Group(_tournament.Id.ToString()).SendAsync("OnReceiveBoard", matchId, board);

        private Task OnOpponentMoved(Guid matchId, Guid opponentId, int row, int col) =>
            _hubContext.Clients.User(opponentId.ToString()).SendAsync("OnOpponentMoved", matchId, row, col);

        private Task OnYourTurn(Guid matchId, Guid playerId, Mark[][] board) =>
            _hubContext.Clients.User(playerId.ToString()).SendAsync("OnYourTurn", matchId, playerId, board);

        private Task OnTournamentUpdated(Guid tournamentId) =>
            _hubContext.Clients.Group(_tournament.Id.ToString()).SendAsync("OnTournamentUpdated", tournamentId);

        private Task OnTournamentUpdated(Models.Tournament tournament) =>
            OnTournamentUpdated(tournament.Id);
    }
}