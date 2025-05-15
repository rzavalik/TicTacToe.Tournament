using Microsoft.AspNetCore.SignalR;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.DTOs;
using TicTacToe.Tournament.Server.Bots;
using TicTacToe.Tournament.Server.Interfaces;

namespace TicTacToe.Tournament.Server.Hubs;

public class TournamentHub : Hub
{
    private readonly ITournamentManager _tournamentManager;

    public TournamentHub(ITournamentManager tournamentManager)
    {
        _tournamentManager = tournamentManager;
    }

    public async Task<TournamentDto?> GetTournamentAsync(Guid tournamentId)
    {
        var tournament = _tournamentManager.GetTournament(tournamentId);
        if (tournament == null)
        {
            return null;
        }

        return new TournamentDto(tournament);
    }

    public async Task RenamePlayerAsync(Guid tournamentId, Guid playerId, string newName)
    {
        var tContext = await _tournamentManager.GetTournamentContextAsync(tournamentId);
        if (tContext == null)
        {
            throw new InvalidOperationException($"Tournament {tournamentId} not found.");
        }

        await _tournamentManager.RenamePlayerAsync(tournamentId, playerId, newName);


        await Task.WhenAll(
            OnTournamentUpdated(tContext.Tournament.Id),
            OnRefreshLeaderboard(tContext.Tournament)
        );
    }

    public async Task RenameTournamentAsync(Guid tournamentId, string newName)
    {
        var tContext = await _tournamentManager.GetTournamentContextAsync(tournamentId);
        if (tContext == null)
        {
            throw new InvalidOperationException($"Tournament {tournamentId} not found.");
        }

        tContext.Tournament.Name = newName;

        await _tournamentManager.RenameTournamentAsync(tournamentId, newName);
        await OnTournamentUpdated(tournamentId);
    }

    public async Task DeleteTournamentAsync(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Request to delete tournament {tournamentId}");

        await _tournamentManager.DeleteTournamentAsync(tournamentId);

        await OnTournamentDeleted(tournamentId);
    }

    public Task<IEnumerable<TournamentSummaryDto>> GetAllTournamentsAsync()
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
            .Select(t => new TournamentSummaryDto(t));

        return Task.FromResult(result);
    }

    public async Task CreateTournamentAsync(Guid tournamentId, string name = null, uint? matchRepetition = null)
    {
        Console.WriteLine($"[TournamentHub] Received CreateTournament for ID: {tournamentId}");

        var tournament = new Models.Tournament(
            tournamentId,
            name ?? "Tournament",
            matchRepetition ?? 1);

        await _tournamentManager.SaveTournamentAsync(tournament);

        await _tournamentManager.InitializeTournamentAsync(tournamentId, name, matchRepetition.Value);

        await Task.WhenAll(
            OnTournamentCreatedCaller(tournamentId),
            OnTournamentCreated(tournamentId)
        );
    }

    public async Task StartTournamentAsync(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Request to start tournament {tournamentId}");

        _ = Task.Run(() => _tournamentManager.StartTournamentAsync(tournamentId));

        await OnTournamentUpdated(tournamentId);
    }

    public async Task CancelTournamentAsync(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Request to cancel tournament {tournamentId}");

        await _tournamentManager.CancelTournamentAsync(tournamentId);

        await OnTournamentCancelled(tournamentId);
    }

    public async Task<bool> TournamentExistsAsync(Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] Checking if tournament {tournamentId} exists...");

        return await _tournamentManager.TournamentExistsAsync(tournamentId);
    }

    public async Task<TournamentDto> SpectateTournamentAsync(Guid tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tournamentId.ToString());
        Console.WriteLine($"[TournamentHub] UI client joined group {tournamentId}");
        return await GetTournamentAsync(tournamentId);
    }

    public async Task RegisterPlayerAsync(string playerName, Guid tournamentId)
    {
        Console.WriteLine($"[TournamentHub] RegisterPlayer called: {playerName} in tournament {tournamentId}");

        var playerId = Guid.Parse(Context.UserIdentifier!);

        var remoteBot = new RemotePlayerBot(
            playerId,
            playerName,
            Clients.Caller,
            tournamentId);

        await _tournamentManager.RegisterPlayerAsync(tournamentId, remoteBot);
        await Groups.AddToGroupAsync(Context.ConnectionId, tournamentId.ToString());

        await Task.WhenAll(
            OnPlayerRegistered(tournamentId, playerId),
            OnRegistered(playerId)
        );
    }

    public async Task<IList<MatchDto>> GetMatchesAsync(Guid tournamentId)
    {
        var tContext = await _tournamentManager.GetTournamentContextAsync(tournamentId);
        if (tContext == null)
        {
            throw new ArgumentNullException(nameof(tournamentId), $"Tournament {tournamentId} not found.");
        }

        var tournament = tContext.Tournament;
        return tournament.Matches.Select(m => new MatchDto(tournament, m)).ToList();
    }

    public async Task SubmitMoveAsync(Guid tournamentId, byte row, byte col)
    {
        Console.WriteLine($"[TournamentHub] SubmitMove called: {tournamentId} ({row},{col})");

        if (!RequestingPlayerId.HasValue)
        {
            Console.WriteLine("[TournamentHub] Invalid UserIdentifier.");
            return;
        }

        await _tournamentManager.SubmitMoveAsync(tournamentId, RequestingPlayerId.Value, row, col);

        Console.WriteLine($"[TournamentHub] Move received from {RequestingPlayerId.Value}: ({row},{col})");
    }

    private Guid? RequestingPlayerId
    {
        get
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var playerId))
            {
                Console.WriteLine("[TournamentHub] Invalid UserIdentifier.");
                return null;
            }

            return playerId;
        }
    }

    private Task OnRefreshLeaderboard(Models.Tournament tournament) => Clients.Group(tournament.Id.ToString()).SendAsync("OnRefreshLeaderboard", tournament.Leaderboard);
    private Task OnTournamentUpdated(Guid tournamentId) => Clients.All.SendAsync("OnTournamentUpdated", tournamentId);
    private Task OnTournamentDeleted(Guid tournamentId) => Clients.All.SendAsync("OnTournamentDeleted", tournamentId);
    private Task OnTournamentCreated(Guid tournamentId) => Clients.All.SendAsync("OnTournamentCreated", tournamentId);
    private Task OnTournamentCreatedCaller(Guid tournamentId) => Clients.All.SendAsync("OnTournamentCreated", tournamentId);
    private Task OnTournamentCancelled(Guid tournamentId) => Clients.All.SendAsync("OnTournamentCancelled", tournamentId);
    private Task OnPlayerRegistered(Guid tournamentId, Guid playerId) => Clients.Group(tournamentId.ToString()).SendAsync("OnPlayerRegistered", playerId);
    private Task OnRegistered(Guid playerId) => Clients.Caller.SendAsync("OnRegistered", playerId);

    private static int GetStatusRank(TournamentStatus status) => status switch
    {
        TournamentStatus.Planned => 0,
        TournamentStatus.Ongoing => 1,
        TournamentStatus.Finished => 2,
        TournamentStatus.Cancelled => 3,
        _ => 4
    };
}
