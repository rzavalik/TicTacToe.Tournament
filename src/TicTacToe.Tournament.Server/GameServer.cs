using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Hubs;

namespace TicTacToe.Tournament.Server;

public class GameServer : IGameServer
{
    private readonly Guid _tournamentId;
    private readonly IHubContext<TournamentHub> _hubContext;
    private readonly Dictionary<Guid, IPlayerBot> _players = new();
    private readonly Action<Guid, MatchScore> _updateLeaderboard;
    private readonly Func<Task> _saveTournament;
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> _pendingMoves = new();

    public GameServer(
        Guid tournamentId,
        IHubContext<TournamentHub> hubContext,
        Action<Guid, MatchScore> updateLeaderboard,
        Func<Task> saveTournament)
    {
        _tournamentId = tournamentId;
        _hubContext = hubContext;
        _updateLeaderboard = updateLeaderboard;
        _saveTournament = saveTournament;
    }

    public IReadOnlyDictionary<Guid, IPlayerBot> RegisteredPlayers => _players;

    public void RegisterPlayer(IPlayerBot player)
    {
        if (_players.ContainsKey(player.Id))
        {
            Console.WriteLine($"[GameServer] Player {player.Name} ({player.Id}) re-registered. Replacing instance.");
        }
        else
        {
            Console.WriteLine($"[GameServer] Player {player.Name} registered successfully.");
        }

        _players[player.Id] = player;

        player.OnRegistered(_tournamentId);
    }

    public ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> GetPendingMoves()
    {
        return _pendingMoves;
    }

    public void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> moves)
    {
        _pendingMoves.Clear();

        foreach (var entry in moves)
        {
            _pendingMoves[entry.Key] = new ConcurrentQueue<(int Row, int Col)>(entry.Value);
        }
    }

    public async Task StartTournamentAsync(
        Models.Tournament tournament,
        Dictionary<Guid, IPlayerBot> registeredBots)
    {
        Console.WriteLine($"[GameServer] Creating matches for tournament {tournament.Id}...");

        GenerateMatches(tournament, registeredBots);

        Console.WriteLine($"[GameServer] Created {tournament.Matches.Count} matches.");

        await Task.WhenAll(
            OnTournamentStarted(tournament),
            RunMatches(tournament)
        );
    }

    public void SubmitMove(Guid playerId, int row, int col)
    {
        Console.WriteLine($"[GameServer] Move received from {playerId}: ({row},{col})");

        var queue = _pendingMoves
            .GetOrAdd(playerId, _ => new ConcurrentQueue<(int, int)>());

        queue.Enqueue((row, col));
    }

    public IPlayerBot? GetBotById(Guid playerId)
    {
        return _players.TryGetValue(playerId, out var bot) ? bot : null;
    }

    private void GenerateMatches(
        Models.Tournament tournament,
        Dictionary<Guid, IPlayerBot> registeredBots)
    {
        var ids = registeredBots.Keys.ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            for (int j = 0; j < ids.Count; j++)
            {
                if (i == j) continue;

                tournament.Matches.Add(new Models.Match
                {
                    PlayerA = ids[i],
                    PlayerB = ids[j],
                    Status = MatchStatus.Planned
                });
            }
        }
    }

    private async Task<GameResult> PlayMatchAsync(Models.Match match)
    {
        Console.WriteLine($"[GameServer][Match] Starting match {match.Id}: {match.PlayerA} vs {match.PlayerB}");

        match.Status = MatchStatus.Ongoing;
        match.StartTime = DateTime.UtcNow;

        var board = new Board();
        match.Board = board.GetState();

        var turn = 0;

        var playerX = _players[match.PlayerA];
        var playerO = _players[match.PlayerB];

        var players = new Dictionary<Mark, IPlayerBot>
        {
            [Mark.X] = playerX,
            [Mark.O] = playerO
        };

        Console.WriteLine($"[GameServer][Match:{match.Id}] Notifying players and spectators...");

        await Task.WhenAll(
            OnMatchStarted(match, playerX.Id, playerO.Id, Mark.X, true),
            OnMatchStarted(match, playerO.Id, playerX.Id, Mark.O, false),
            OnTournamentUpdated(_tournamentId)
        );

        while (match.Status == MatchStatus.Ongoing)
        {
            var mark = (turn % 2 == 0) ? Mark.X : Mark.O;
            var currentPlayer = players[mark];
            var opponent = players[mark == Mark.X ? Mark.O : Mark.X];

            Console.WriteLine($"[GameServer][Match:{match.Id}] CurrentPlayer is {currentPlayer.Id} vs {opponent.Id}");

            match.CurrentTurn = currentPlayer.Id;

            Console.WriteLine($"[GameServer] Waiting for move from {currentPlayer.Id}...");

            await OnYourTurn(match.Id, currentPlayer.Id, match.Board);

            (int row, int col)? move = null;

            try
            {
                move = await WaitForMoveAsync(currentPlayer.Id, timeoutInMs: 60000);
                Console.WriteLine($"[Validation] Player {currentPlayer.Id} attempted move at ({move.Value.row},{move.Value.col})");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[GameServer] Timeout! Player {currentPlayer.Id} did not respond.");

                await _hubContext.Clients.All.SendAsync("OnTournamentUpdated", _tournamentId);

                match.Status = MatchStatus.Finished;
                match.WinnerMark = mark == Mark.X ? Mark.O : Mark.X;
                match.Board = board.GetState();

                _updateLeaderboard(match.WinnerMark == Mark.X ? match.PlayerA : match.PlayerB, MatchScore.Win);
                _updateLeaderboard(match.WinnerMark == Mark.X ? match.PlayerB : match.PlayerA, MatchScore.Walkover);

                await Task.WhenAll(
                    OnTournamentUpdated(_tournamentId),
                    _saveTournament()
                );

                return new GameResult
                {
                    MatchId = match.Id,
                    WinnerId = match.PlayerA == currentPlayer.Id
                        ? match.PlayerB
                        : match.PlayerA,
                    Board = board.GetState(),
                    IsDraw = false
                };
            }

            if (move.HasValue)
            {
                var (row, col) = move.Value;

                if (!board.IsValidMove(row, col))
                {
                    Console.WriteLine($"[Validation] Move rejected — Cell occupied or out of bounds.");
                }
                else
                {
                    board.ApplyMove(row, col, mark);

                    match.Board = board.GetState();

                    Console.WriteLine($"[GameServer] Move played by {currentPlayer.Id} at ({row},{col})");

                    await Task.WhenAll(
                        OnOpponentMoved(match.Id, opponent.Id, row, col),
                        OnReceiveBoard(match.Id, match.Board)
                    );

                    turn++;
                }
            }

            DrawBoard(match.Board);

            if (board.IsGameOver())
            {
                match.WinnerMark = board.GetWinner();
                break;
            }

            await Task.Delay(100);
        }

        Console.WriteLine($"[GameServer] Game loop for match {match.Id} completed.");

        match.Status = MatchStatus.Finished;
        match.EndTime = DateTime.UtcNow;
        match.Board = board.GetState();

        Console.WriteLine($"[GameServer] Match {match.Id} finished.");

        var gameResult = new GameResult
        {
            MatchId = match.Id,
            WinnerId = match.WinnerMark.HasValue ? players[match.WinnerMark.Value].Id : null,
            Board = board.GetState(),
            IsDraw = !match.WinnerMark.HasValue
        };

        if (gameResult.WinnerId != null)
        {
            _updateLeaderboard(gameResult.WinnerId.Value, MatchScore.Win);
        }
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

    private static void DrawBoard(Mark[][] board)
    {
        Console.WriteLine();
        for (int row = 0; row < 3; row++)
        {
            Console.WriteLine($"{RenderMark(board[row][0])} | {RenderMark(board[row][1])} | {RenderMark(board[row][2])}");
        }
        Console.WriteLine();
    }

    private static string RenderMark(Mark mark)
    {
        return mark switch
        {
            Mark.X => "X",
            Mark.O => "O",
            _ => " "
        };
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

    private Task RunMatches(Models.Tournament tournament)
    {
        return Task.Run(async () =>
        {
            foreach (var match in tournament.Matches)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await RunMatchAsync(tournament.Id, match);
            }

            tournament.Status = TournamentStatus.Finished;
            tournament.EndTime = DateTime.UtcNow;

            await Task.WhenAll(
                OnTournamentUpdated(tournament),
                _saveTournament()
            );

            Console.WriteLine($"[GameServer] Tournament {_tournamentId} finished.");
        });
    }

    private async Task RunMatchAsync(Guid tournamentId, Models.Match match)
    {
        Console.WriteLine($"[GameServer] Starting match {match.Id} ({match.PlayerA} vs {match.PlayerB})");

        match.Status = MatchStatus.Ongoing;
        match.StartTime = DateTime.UtcNow;
        match.Board = new Mark[3][];
        for (int i = 0; i < 3; i++)
        {
            match.Board[i] = new Mark[3];
        }

        var result = await PlayMatchAsync(match);

        match.EndTime = DateTime.UtcNow;
        match.Status = MatchStatus.Finished;
        await OnTournamentUpdated(tournamentId);

        Console.WriteLine($"[GameServer] Match {match.Id} finished. Winner: {result.WinnerId?.ToString() ?? "Draw"}");
    }

    private Task OnMatchStarted(
        Models.Match match,
        Guid playerId,
        Guid opponentId,
        Mark playerMark,
        bool yourTurn)
    {
        return _hubContext
            .Clients
            .User(playerId.ToString())
            .SendAsync("OnMatchStarted",
                match.Id,
                playerId,
                opponentId,
                playerMark.ToString("G"),
                yourTurn);
    }

    private Task OnMatchEnded(GameResult gameResult, Guid playerToNotify)
    {
        return _hubContext
            .Clients
            .User(playerToNotify.ToString())
            .SendAsync("OnMatchEnded", gameResult);
    }

    private Task OnTournamentStarted(Models.Tournament tournament)
    {
        return _hubContext
            .Clients
            .Group(_tournamentId.ToString())
            .SendAsync("OnTournamentStarted", new
            {
                tournamentId = tournament.Id,
                playerIds = tournament.RegisteredPlayers.Keys,
                totalPlayers = tournament.RegisteredPlayers.Count,
                matches = tournament.Matches.Select(m => new
                {
                    m.Id,
                    m.PlayerA,
                    m.PlayerB,
                    m.Status
                })
            });
    }

    private Task OnReceiveBoard(Guid matchId, Mark[][] board)
    {
        return _hubContext
            .Clients
            .Group(_tournamentId.ToString())
            .SendAsync("OnReceiveBoard", matchId, board);
    }

    private Task OnOpponentMoved(Guid matchId, Guid opponentId, int row, int col)
    {
        return _hubContext
            .Clients
            .User(opponentId.ToString())
            .SendAsync("OnOpponentMoved", matchId, row, col);
    }

    private Task OnYourTurn(Guid matchId, Guid playerId, Mark[][] board)
    {
        return _hubContext
            .Clients
            .User(playerId.ToString())
            .SendAsync("OnYourTurn", matchId, playerId, board);
    }

    private Task OnTournamentUpdated(Guid tournamentId)
    {
        return _hubContext
            .Clients
            .Group(_tournamentId.ToString())
            .SendAsync("OnTournamentUpdated", tournamentId);
    }

    private Task OnTournamentUpdated(Models.Tournament tournament)
    {
        return OnTournamentUpdated(tournament.Id);
    }
}