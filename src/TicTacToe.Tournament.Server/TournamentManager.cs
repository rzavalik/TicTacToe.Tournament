using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Bots;
using TicTacToe.Tournament.Server.Hubs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Services;

namespace TicTacToe.Tournament.Server;

public class TournamentManager : ITournamentManager
{
    private readonly IAzureStorageService _storageService;
    private readonly Dictionary<Guid, Models.Tournament> _tournaments = new();
    private readonly Dictionary<Guid, GameServer> _gameServers = new();
    private readonly Dictionary<Guid, Dictionary<Guid, IPlayerBot>> _players = new();
    private readonly Dictionary<Guid, Guid> _playerTournamentMap = new();
    private readonly Dictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly IHubContext<TournamentHub> _hubContext;

    public TournamentManager(
        IHubContext<TournamentHub> hubContext,
        IAzureStorageService storageService)
    {
        _hubContext = hubContext;
        _storageService = storageService;
    }

    private SemaphoreSlim GetLock(Guid tournamentId)
    {
        lock (_locks)
        {
            if (!_locks.ContainsKey(tournamentId))
                _locks[tournamentId] = new SemaphoreSlim(1, 1);

            return _locks[tournamentId];
        }
    }

    public GameServer? GetGameServerForPlayer(Guid playerId)
    {
        if (_playerTournamentMap.TryGetValue(playerId, out var tournamentId))
        {
            if (_gameServers.TryGetValue(tournamentId, out var server))
            {
                return server;
            }
        }

        return null;
    }

    public async Task InitializeTournamentAsync(Guid tournamentId, string? name = null)
    {
        var sem = GetLock(tournamentId);
        await sem.WaitAsync();

        try
        {
            if (_tournaments.ContainsKey(tournamentId))
            {
                return;
            }

            var (tournament, playerInfos, map, moves) = await _storageService.LoadTournamentStateAsync(tournamentId);

            if (tournament is not null)
            {
                if (!_tournaments.TryAdd(tournamentId, tournament))
                {
                    _tournaments[tournamentId] = tournament;
                }

                _playerTournamentMap.Clear();
                if (map != null)
                {
                    foreach (var kv in map)
                    {
                        _playerTournamentMap[kv.Key] = kv.Value;
                    }
                }

                _players[tournamentId] = new();
                if (playerInfos != null)
                {
                    foreach (var info in playerInfos)
                    {
                        var bot = new DummyPlayerBot(
                            info.PlayerId,
                            info.Name);

                        _players[tournamentId][info.PlayerId] = bot;
                    }
                }

                var gameServer = new GameServer(
                    tournamentId,
                    _hubContext,
                    async (playerId, matchScore) =>
                    {
                        tournament.UpdateLeaderboard(
                            playerId,
                            matchScore);

                        await _hubContext
                            .Clients
                            .Group(tournament.Id.ToString())
                            .SendAsync("OnRefreshLeaderboard", tournament.Leaderboard);
                    },
                    async () =>
                    {
                        await SaveStateAsync(tournament);
                    });

                if (!_gameServers.TryAdd(tournamentId, gameServer))
                {
                    _gameServers[tournamentId] = gameServer;
                }

                if (moves != null)
                {
                    _gameServers[tournamentId].LoadPendingMoves(moves);
                }

                await SaveStateUnsafeAsync(tournament);

                return;
            }

            var newTournament = new Models.Tournament
            {
                Id = tournamentId,
                Name = name ?? $"Tournament {tournamentId}",
                Status = TournamentStatus.Planned
            };

            _tournaments[tournamentId] = newTournament;
            _players[tournamentId] = new();

            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} initialized.");

            await SaveStateUnsafeAsync(newTournament);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task RegisterPlayerAsync(Guid tournamentId, IPlayerBot bot)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
        {
            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
            return;
        }

        if (tournament.Status != TournamentStatus.Planned)
        {
            Console.WriteLine($"[TournamentManager] Cannot register player after tournament started.");
            return;
        }

        if (!tournament.RegisteredPlayers.ContainsKey(bot.Id))
        {
            tournament.RegisteredPlayers[bot.Id] = bot.Name;
            Console.WriteLine($"[TournamentManager] Player {bot.Name} registered with ID {bot.Id}.");
        }

        _playerTournamentMap[bot.Id] = tournamentId;
        _players[tournamentId][bot.Id] = bot;

        if (!_gameServers.ContainsKey(tournamentId))
        {
            _gameServers[tournamentId] = new GameServer(
                tournamentId,
                _hubContext,
                async (playerId, matchScore) =>
                {

                    tournament.UpdateLeaderboard(playerId, matchScore);

                    await _hubContext
                        .Clients
                        .Group(tournament.Id.ToString())
                        .SendAsync("OnRefreshLeaderboard", tournament.Leaderboard);
                },
                async () =>
                {
                    await SaveStateAsync(tournament);
                });
        }

        _gameServers[tournamentId].RegisterPlayer(bot);

        await Task.WhenAll(
            _hubContext.Clients
                .Group(tournament.Id.ToString())
                .SendAsync(
                    "OnRefreshLeaderboard",
                    tournament.Leaderboard),

            _hubContext
                .Clients
                .All
                .SendAsync(
                    "OnTournamentUpdated",
                    tournamentId),

            SaveStateAsync(tournament)
        );
    }

    public async Task StartTournamentAsync(Guid tournamentId)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
        {
            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
            return;
        }

        if (tournament.Status != TournamentStatus.Planned)
        {
            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} already started or finished.");
            return;
        }

        tournament.Status = TournamentStatus.Ongoing;
        tournament.StartTime = DateTime.UtcNow;

        foreach (var playerId in tournament.RegisteredPlayers.Keys)
        {
            tournament.UpdateLeaderboard(playerId, 0);
        }

        Console.WriteLine($"[TournamentManager] Starting tournament {tournamentId} with {tournament.RegisteredPlayers.Count} players.");

        var server = _gameServers[tournamentId];

        await Task.WhenAll(
            server.StartTournamentAsync(tournament, _players[tournamentId]),
            _hubContext.Clients.Group(tournamentId.ToString()).SendAsync("OnRefreshLeaderboard", tournament.Leaderboard),
            SaveStateAsync(tournament)
        );
    }

    public async Task CancelTournament(Guid tournamentId)
    {
        if (_tournaments.TryGetValue(tournamentId, out var tournament))
        {
            tournament.Status = TournamentStatus.Cancelled;
            tournament.EndTime = DateTime.UtcNow;

            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} cancelled.");

            await SaveStateAsync(tournament);
        }

        _gameServers.Remove(tournamentId);
        _players.Remove(tournamentId);
    }

    public async Task<Models.Tournament?> GetOrLoadTournamentAsync(Guid tournamentId)
    {
        if (!_tournaments.ContainsKey(tournamentId))
            await InitializeTournamentAsync(tournamentId);

        return _tournaments.TryGetValue(tournamentId, out var tournament)
            ? tournament
            : null;
    }

    public async Task<GameServer?> GetOrLoadGameServerAsync(Guid tournamentId)
    {
        await GetOrLoadTournamentAsync(tournamentId);

        return _gameServers.TryGetValue(tournamentId, out var server)
            ? server
            : null;
    }

    public bool TournamentExists(Guid tournamentId) => _tournaments.ContainsKey(tournamentId);

    public Models.Tournament? GetTournament(Guid tournamentId)
        => _tournaments.TryGetValue(tournamentId, out var tournament)
        ? tournament
        : null;

    public IReadOnlyCollection<Models.Tournament> GetAllTournaments() => _tournaments.Values;

    public Dictionary<Guid, int> GetLeaderboard(Guid tournamentId)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
        {
            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
            return new();
        }

        var leaderboard = new Dictionary<Guid, int>();

        foreach (var match in tournament.Matches.Where(m => m.Status == MatchStatus.Finished))
        {
            var winner = match.WinnerMark;

            MatchScore scoreA = MatchScore.Draw;
            MatchScore scoreB = MatchScore.Draw;

            if (winner.HasValue)
            {
                if (winner.Value == Mark.X)
                {
                    scoreA = MatchScore.Win;
                    scoreB = MatchScore.Lose;
                }
                else if (winner.Value == Mark.O)
                {
                    scoreA = MatchScore.Lose;
                    scoreB = MatchScore.Win;
                }
            }

            leaderboard[match.PlayerA] = leaderboard.GetValueOrDefault(match.PlayerA) + (int)scoreA;
            leaderboard[match.PlayerB] = leaderboard.GetValueOrDefault(match.PlayerB) + (int)scoreB;
        }

        return leaderboard;
    }

    public GameServer? GetGameServer(Guid tournamentId)
    {
        return _gameServers.TryGetValue(tournamentId, out var server) ? server : null;
    }

    private async Task SaveStateAsync(Models.Tournament tournament)
    {
        var sem = GetLock(tournament.Id);
        await sem.WaitAsync();

        try
        {
            await SaveStateUnsafeAsync(tournament);
        }
        finally
        {
            sem.Release();
        }
    }

    private async Task SaveStateUnsafeAsync(Models.Tournament tournament)
    {
        _gameServers.TryGetValue(tournament.Id, out var gameServer);

        var pendingMoves = gameServer?.GetPendingMoves() ?? new ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>();

        await _storageService.SaveTournamentStateAsync(
            tournament.Id,
            tournament,
            _players[tournament.Id],
            _playerTournamentMap,
            pendingMoves);
    }
}