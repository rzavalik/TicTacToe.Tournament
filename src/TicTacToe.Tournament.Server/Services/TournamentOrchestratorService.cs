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
            "SpectateTournament",
            tournamentId
        );
    }

    public async Task<TournamentDto?> GetTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();

        return await _connection.InvokeAsync<TournamentDto>(
            "GetTournament",
            tournamentId
        );
    }

    public async Task<IEnumerable<TournamentSummaryDto>> GetAllTournamentsAsync()
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<IEnumerable<TournamentSummaryDto>>("GetAllTournaments");
    }

    public async Task CreateTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("CreateTournament", tournamentId);
    }

    public async Task StartTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("StartTournament", tournamentId);
    }

    public async Task CancelTournamentAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        await _connection.InvokeAsync("CancelTournament", tournamentId);
    }

    public async Task<bool> TournamentExistsAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<bool>("TournamentExists", tournamentId);
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
        return await _connection.InvokeAsync<IEnumerable<MatchDto>>("GetMatches", tournamentId);
    }

    public async Task<MatchBoardDto?> GetCurrentMatchBoardAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchBoardDto>("GetCurrentMatchBoard", tournamentId);
    }

    public async Task<MatchPlayersDto?> GetCurrentMatchPlayersAsync(Guid tournamentId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchPlayersDto>("GetCurrentMatchPlayers", tournamentId);
    }

    public async Task<MatchBoardDto?> GetMatchBoardAsync(Guid tournamentId, Guid matchId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchBoardDto>("GetMatchBoard", tournamentId, matchId);
    }

    public async Task<MatchPlayersDto?> GetMatchPlayersAsync(Guid tournamentId, Guid matchId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<MatchPlayersDto>("GetMatchPlayers", tournamentId, matchId);
    }

    public async Task<PlayerDto?> GetPlayerAsync(Guid tournamentId, Guid playerId)
    {
        await EnsureConnectionAsync();
        return await _connection.InvokeAsync<PlayerDto>("GetPlayer", tournamentId, playerId);
    }
}