using Microsoft.AspNetCore.SignalR;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Server.Bots;
using TicTacToe.Tournament.Models.DTOs;
using TicTacToe.Tournament.Server.Interfaces;

namespace TicTacToe.Tournament.Server.Hubs;

public class TournamentHub : Hub
{
    private readonly ITournamentManager _tournamentManager;

    public TournamentHub(ITournamentManager tournamentManager)
    {
        _tournamentManager = tournamentManager;
    }

    public async Task<TournamentDto?> GetTournament(Guid tournamentId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
            return null;

        var dto = new TournamentDto
        {
            Id = tournament.Id,
            Name = tournament.Name,
            Status = tournament.Status.ToString(),
            RegisteredPlayers = tournament.RegisteredPlayers,
            Leaderboard = tournament.Leaderboard,
            StartTime = tournament.StartTime,
            Duration = tournament.Duration,
            EndTime = tournament.EndTime,
            Matches = tournament.Matches.Select(m => new MatchDto
            {
                Id = m.Id,
                PlayerAId = m.PlayerA,
                PlayerBId = m.PlayerB,
                Status = m.Status,
                Board = m.Board,
                StartTime = m.StartTime,
                EndTime = m.EndTime
            }).ToList()
        };

        return dto;
    }

    public Task<IEnumerable<TournamentSummaryDto>> GetAllTournaments()
    {
        var result = _tournamentManager
            .GetAllTournaments()
            .OrderByDescending(t => t.Status)
            .OrderBy(t => GetStatusRank(t.Status))
            .ThenByDescending(t =>
                t.Status == TournamentStatus.Ongoing ||
                t.Status == TournamentStatus.Finished ||
                t.Status == TournamentStatus.Cancelled
                ? t.StartTime
                : null)
            .Select(t => new TournamentSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status.ToString(),
                RegisteredPlayersCount = t.RegisteredPlayers.Count,
                MatchCount = t.Matches.Count
            });

        return Task.FromResult(result);
    }

    private static int GetStatusRank(TournamentStatus status)
    {
        return status switch
        {
            TournamentStatus.Planned => 0,
            TournamentStatus.Ongoing => 1,
            TournamentStatus.Finished => 2,
            TournamentStatus.Cancelled => 3,
            _ => 4
        };
    }

    public async Task CreateTournament(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Received CreateTournament for ID: {tournamentId}");

        await Task.WhenAll(
            _tournamentManager.InitializeTournamentAsync(tournamentId),
            Clients.Caller.SendAsync("OnTournamentCreated", tournamentId),
            Clients.All.SendAsync("OnTournamentCreated", tournamentId)
        );
    }

    public async Task StartTournament(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Request to start tournament {tournamentId}");

        _ = Task.Run(() => _tournamentManager.StartTournamentAsync(tournamentId));

        await Clients.All.SendAsync("OnTournamentUpdated", tournamentId);
    }

    public Task CancelTournament(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Request to cancel tournament {tournamentId}");

        _tournamentManager.CancelTournament(tournamentId);

        return Clients.All.SendAsync("OnTournamentCancelled", tournamentId);
    }

    public async Task<bool> TournamentExists(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Checking if tournament {tournamentId} exists...");

        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);

        return tournament != null;
    }

    public async Task SpectateTournament(Guid tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tournamentId.ToString());
        Console.WriteLine($"[TournamentHub] UI client joined group {tournamentId}");
    }

    public async Task RegisterPlayer(string playerName, Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] RegisterPlayer called: {playerName} in tournament {tournamentId}");

        var playerId = Guid.Parse(Context.UserIdentifier!);

        var remoteBot = new RemotePlayerBot(
            playerId,
            playerName,
            Clients.Caller);

        await Task.WhenAll(
            _tournamentManager.RegisterPlayerAsync(tournamentId, remoteBot),
            Groups.AddToGroupAsync(Context.ConnectionId, tournamentId.ToString()),
            Clients.Group(tournamentId.ToString()).SendAsync("OnPlayerRegistered", playerId),
            Clients.Caller.SendAsync("OnRegistered", playerId)
        );
    }

    public async Task<IEnumerable<MatchDto>> GetMatches(Guid tournamentId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
        {
            Console.WriteLine($"[TournamentHub] Tournament {tournamentId} not found in GetMatches().");
            return [];
        }

        var result = tournament.Matches.Select(m => new MatchDto
        {
            Id = m.Id,
            PlayerAId = m.PlayerA,
            PlayerAName = tournament.RegisteredPlayers[m.PlayerA] ?? "Unknown",
            PlayerBId = m.PlayerB,
            PlayerBName = tournament.RegisteredPlayers[m.PlayerB] ?? "Unknown",
            Status = m.Status,
            Board = m.Board,
            StartTime = m.StartTime,
            EndTime = m.EndTime,
            Duration = m.Duration?.ToString(@"hh\:mm\:ss"),
            Winner = m.WinnerMark?.ToString()
        });

        return result;
    }

    public async Task<MatchBoardDto?> GetCurrentMatchBoard(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentManager] Loading tournament {tournamentId} on demand...");

        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
            return null;

        var match = GetCurrentMatch(tournament);

        if (match == null)
        {
            return null;
        }

        return new MatchBoardDto
        {
            MatchId = match.Id,
            Board = match.Board,
            CurrentTurn = match.CurrentTurn
        };
    }

    public async Task<MatchPlayersDto?> GetCurrentMatchPlayers(Guid tournamentId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
        {
            return null;
        }

        var match = GetCurrentMatch(tournament);

        if (match == null)
        {
            return null;
        }

        return new MatchPlayersDto
        {
            MatchId = match.Id,
            PlayerAId = match.PlayerA,
            PlayerAName = tournament!.RegisteredPlayers[match.PlayerA] ?? "Unknown",
            PlayerBId = match.PlayerB,
            PlayerBName = tournament!.RegisteredPlayers[match.PlayerB] ?? "Unknown"
        };
    }

    public async Task<MatchBoardDto?> GetMatchBoard(Guid tournamentId, Guid matchId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
        {
            return null;
        }

        var match = tournament
            .Matches
            .FirstOrDefault(m => m.Id == matchId);

        if (match == null)
        {
            return null;
        }

        return new MatchBoardDto
        {
            MatchId = match.Id,
            Board = match.Board,
            CurrentTurn = match.CurrentTurn
        };
    }

    public async Task<MatchPlayersDto?> GetMatchPlayers(Guid tournamentId, Guid matchId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
        {
            return null;
        }

        var match = tournament?
            .Matches
            .FirstOrDefault(m => m.Id == matchId);

        if (match == null)
        {
            return null;
        }

        return new MatchPlayersDto
        {
            MatchId = match.Id,
            PlayerAId = match.PlayerA,
            PlayerAName = tournament?.RegisteredPlayers[match.PlayerA] ?? "Unknown",
            PlayerBId = match.PlayerB,
            PlayerBName = tournament?.RegisteredPlayers[match.PlayerB] ?? "Unknown"
        };
    }

    public async Task<PlayerDto?> GetPlayer(Guid tournamentId, Guid playerId)
    {
        var tournament = await _tournamentManager.GetOrLoadTournamentAsync(tournamentId);
        if (tournament == null)
        {
            return null;
        }

        if (!tournament.RegisteredPlayers.ContainsKey(playerId))
        {
            return null;
        }

        var name = tournament.RegisteredPlayers[playerId];
        var leaderboard = _tournamentManager.GetLeaderboard(tournamentId);
        var score = leaderboard.GetValueOrDefault(playerId, 0);

        return new PlayerDto
        {
            Id = playerId,
            Name = name,
            Score = score
        };
    }

    public async Task SubmitMove(Guid tournamentId, int row, int col)
    {
        Console.WriteLine($"[TournamentHub] SubmitMove called: {tournamentId} ({row},{col})");

        var playerIdString = Context.UserIdentifier;

        if (!Guid.TryParse(playerIdString, out var playerId))
        {
            Console.WriteLine($"[TournamentHub] Invalid UserIdentifier.");
            return;
        }

        var gameServer = await _tournamentManager.GetOrLoadGameServerAsync(tournamentId);
        if (gameServer == null)
        {
            Console.WriteLine($"[TournamentHub] No game server found for tournament {tournamentId}.");
            return;
        }

        gameServer.SubmitMove(playerId, row, col);

        Console.WriteLine($"[TournamentHub] Move received from {playerId}: ({row},{col})");
    }

    private Match? GetCurrentMatch(Models.Tournament tournament)
    {
        return tournament!
            .Matches
            .Where(m => m.Status == MatchStatus.Ongoing)
            .OrderBy(m => m.StartTime)
            .FirstOrDefault();
    }
}