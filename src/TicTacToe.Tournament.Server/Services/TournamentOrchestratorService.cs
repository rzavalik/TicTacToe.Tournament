using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using TicTacToe.Tournament.Models.DTOs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Security;

namespace TicTacToe.Tournament.Server.Services;

public class TournamentOrchestratorService : ITournamentOrchestratorService
{
    private readonly HubConnection _connection;
    const string TournamentHubName = "tournamentHub";
    private readonly string _signalREndpoint;
    private readonly string _signalRAccessKey;

    public TournamentOrchestratorService(
        IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config), "Configuration service cannot be null.");
        }

        _signalREndpoint = config["Azure:SignalR:Endpoint"]
            ?? throw new ArgumentNullException(nameof(config), "SignalR Endpoint must be present in the ConnectionString.");
        _signalRAccessKey = config["Azure:SignalR:AccessKey"]
            ?? throw new ArgumentNullException(nameof(config), "SignalR AccessKey must be present in the ConnectionString.");

        var authResponse = SignalRAccessHelper.GenerateSignalRAccessToken(
            _signalREndpoint,
            _signalRAccessKey,
            TournamentHubName,
            "Server");

        const string endpoint = "https://tictactoe-signalr.service.signalr.net/client/";

        _connection = new HubConnectionBuilder()
            .WithUrl($"{endpoint}?hub=tournamentHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult((string?)authResponse);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection
            .StartAsync()
            .GetAwaiter()
            .GetResult();
    }

    public async Task SpectateTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();

        await _connection.InvokeAsync<TournamentDto>(
            "SpectateTournamentAsync",
            tournamentId
        );
    }

    public async Task<TournamentDto?> GetTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();

        return await _connection.InvokeAsync<TournamentDto>(
            "GetTournamentAsync",
            tournamentId
        );
    }

    public async Task<IEnumerable<TournamentSummaryDto>> GetAllTournamentsAsync()
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<IEnumerable<TournamentSummaryDto>>("GetAllTournamentsAsync");
    }

    public async Task CreateTournamentAsync(Guid tournamentId, string name, uint matchRepetition)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("CreateTournamentAsync", tournamentId, name, matchRepetition);
    }

    public async Task DeleteTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("DeleteTournamentAsync", tournamentId);
    }

    public async Task StartTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("StartTournamentAsync", tournamentId);
    }

    public async Task CancelTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("CancelTournamentAsync", tournamentId);
    }

    public async Task<bool> TournamentExistsAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<bool>("TournamentExistsAsync", tournamentId);
    }

    public async Task RenameTournamentAsync(Guid tournamentId, string newName)
    {
        var tournament = await GetTournamentAsync(tournamentId);
        if (tournament == null)
        {
            throw new InvalidOperationException($"Tournament {tournamentId} not found.");
        }

        tournament.Name = newName;
        await _connection.InvokeAsync("RenameTournamentAsync", tournamentId, newName);
    }

    public async Task RenamePlayerAsync(Guid tournamentId, Guid playerId, string newName)
    {
        var tournament = await GetTournamentAsync(tournamentId);
        if (tournament == null)
        {
            throw new InvalidOperationException($"Tournament {tournamentId} not found.");
        }

        await _connection.InvokeAsync("RenamePlayerAsync", tournamentId, playerId, newName);
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    public async Task<IEnumerable<MatchDto>> GetMatchesAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<IEnumerable<MatchDto>>("GetMatchesAsync", tournamentId);
    }

    public async Task<MatchBoardDto?> GetCurrentMatchBoardAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchBoardDto>("GetCurrentMatchBoardAsync", tournamentId);
    }

    public async Task<MatchPlayersDto?> GetCurrentMatchPlayersAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchPlayersDto>("GetCurrentMatchPlayersAsync", tournamentId);
    }

    public async Task<MatchBoardDto?> GetMatchBoardAsync(Guid tournamentId, Guid matchId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchBoardDto>("GetMatchBoardAsync", tournamentId, matchId);
    }

    public async Task<MatchPlayersDto?> GetMatchPlayersAsync(Guid tournamentId, Guid matchId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchPlayersDto>("GetMatchPlayersAsync", tournamentId, matchId);
    }

    public async Task<PlayerDto?> GetPlayerAsync(Guid tournamentId, Guid playerId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<PlayerDto>("GetPlayerAsync", tournamentId, playerId);
    }
}