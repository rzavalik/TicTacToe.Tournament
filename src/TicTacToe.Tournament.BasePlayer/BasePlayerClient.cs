using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Reflection;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer;

public abstract class BasePlayerClient : IBot
{
    private readonly IHttpClient _httpClient;
    private readonly Guid _tournamentId;
    private readonly string _botName;
    private readonly string _webAppEndpoint;
    private readonly string _signalREndpoint;

    private ISignalRClient? _signalRClient;
    private readonly ISignalRClientBuilder _signalRBuilder;

    public Mark[][] CurrentBoard { get; private set; } = new Mark[3][];

    public bool Authenticated { get; private set; } = false;

    public Guid PlayerId { get; private set; } = default;

    public string? Token { get; private set; } = null;

    public string BotName => _botName;

    public Mark Mark { get; private set; } = Mark.Empty;

    protected BasePlayerClient(
        string botName,
        Guid tournamentId,
        string webAppEndpoint,
        string signalrEndpoint,
        IHttpClient httpClient,
        ISignalRClientBuilder signalRBuilder)
    {
        _botName = botName;
        _tournamentId = tournamentId;
        _webAppEndpoint = webAppEndpoint.TrimEnd('/');
        _signalREndpoint = signalrEndpoint;
        _httpClient = httpClient;
        _signalRBuilder = signalRBuilder;
    }

    public async Task StartAsync()
    {
        await ConnectToSignalRAsync();
        await RegisterAsync();

        Console.WriteLine($"{_botName} is ready. PlayerId = {PlayerId}");

        await Task.Delay(Timeout.Infinite);
    }

    public async Task AuthenticateAsync()
    {
        if (PlayerId != Guid.Empty)
        {
            _httpClient
                .DefaultRequestHeaders
                .Add("X-PlayerId", PlayerId.ToString());
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"{_webAppEndpoint}/tournament/{_tournamentId}/authenticate",
            new TournamentAuthRequest
            {
                TournamentId = _tournamentId,
                MachineName = Environment.MachineName,
                AgentName = Assembly.GetEntryAssembly()?.FullName
            });

        if (!(response?.IsSuccessStatusCode ?? false))
        {
            throw new Exception($"Authentication failed: {response?.StatusCode}");
        }

        var auth = await response.Content.ReadFromJsonAsync<TournamentAuthResponse>();
        if (auth == null)
        {
            throw new Exception($"Authentication failed: could not deserialize TournamentAuthResponse.");
        }

        PlayerId = auth.PlayerId;
        Authenticated = true;
        Token = auth.Token;

        OnAuthenticated(auth);
    }

    private async Task ConnectToSignalRAsync()
    {
        _signalRClient = _signalRBuilder.Build(
            _signalREndpoint,
            () => Task.FromResult(Token)
        );

        if (_signalRClient == null)
        {
            throw new ArgumentNullException(nameof(_signalRClient), "SignalR client cannot be null.");
        }

        _signalRClient.Reconnecting += error =>
        {
            Console.WriteLine("Reconnecting to SignalR...");
            return Task.CompletedTask;
        };

        _signalRClient.Reconnected += async connectionId =>
        {
            await RegisterToTournamentAsync();
            Console.WriteLine($"Reconnected to SignalR with connectionId: {connectionId}");
        };

        _signalRClient.Closed += error =>
        {
            Console.WriteLine("Connection closed.");
            return Task.CompletedTask;
        };

        await _signalRClient.SubscribeAsync<Guid>("OnRegistered", id =>
        {
            Console.WriteLine($"Registered as {id}");
        });

        await _signalRClient.SubscribeAsync<Guid>("OnPlayerRegistered", id =>
        {
            Console.WriteLine($"New player joined: {id}");
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentCreated", id =>
        {
            Console.WriteLine($"Tournament created: {id}");
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentUpdated", id =>
        {
            Console.WriteLine($"Tournament updated: {id}");
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentCancelled", id =>
        {
            Console.WriteLine($"Tournament cancelled: {id}");
        });

        await _signalRClient.SubscribeAsync<object>("OnTournamentStarted", data =>
        {
            Console.WriteLine("Tournament started.");
        });

        await _signalRClient.SubscribeAsync<Guid, Guid, Guid, string, bool>("OnMatchStarted", (matchId, playerId, opponentId, markStr, starts) =>
        {
            Mark = Enum.Parse<Mark>(markStr);
            Console.WriteLine($"Match started: {playerId} vs {opponentId}, mark: {Mark}, yourTurn: {starts}");
            OnMatchStarted(matchId, playerId, opponentId, Mark, starts);
        });

        await _signalRClient.SubscribeAsync<Guid, Guid, Mark[][]>("OnYourTurn", async (matchId, playerId, board) =>
        {
            try
            {
                CurrentBoard = board;
                Console.WriteLine($"It's your turn {playerId}!");
                var move = await MakeMove(matchId, board);
                Console.WriteLine($"Playing at ({move.row},{move.col})");
                await _signalRClient!.InvokeAsync("SubmitMove", _tournamentId, move.row, move.col);
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"You've lost you turn due to Timeout!");
            }
        });

        await _signalRClient.SubscribeAsync<Guid, int, int>("OnOpponentMoved", (matchId, row, col) =>
        {
            Console.WriteLine($"Opponent moved at ({row},{col})");
            OnOpponentMoved(matchId, row, col);
        });

        await _signalRClient.SubscribeAsync<GameResult>("OnMatchEnded", result =>
        {
            Console.WriteLine($"Match ended. Winner: {result.WinnerId?.ToString() ?? "Draw"}");
            OnMatchEnded(result);
        });

        await _signalRClient.SubscribeAsync<Guid, Mark[][]>("OnReceiveBoard", (matchId, board) =>
        {
            CurrentBoard = board;
            OnBoardUpdated(matchId, CurrentBoard);
        });

        await _signalRClient.SubscribeAsync<Dictionary<Guid, int>>("OnRefreshLeaderboard", leaderboard =>
        {
            Console.WriteLine("Leaderboard updated.");
            foreach (var entry in leaderboard.OrderByDescending(e => e.Value))
            {
                Console.WriteLine($" - {entry.Key}: {entry.Value} pts");
            }
        });

        Console.WriteLine("Connecting to SignalR...");
        await _signalRClient.StartAsync();
        Console.WriteLine("Connected to SignalR!");

        await SubscribeTournament(_tournamentId);
    }

    private async Task RegisterToTournamentAsync()
    {
        if (_signalRClient == null || _signalRClient.State != HubConnectionState.Connected)
        {
            Console.WriteLine("[Register] Cannot register player: not connected to SignalR.");
            return;
        }

        try
        {
            Console.WriteLine($"[Register] Registering {_botName} ({PlayerId}) to tournament {_tournamentId}...");
            await _signalRClient.InvokeAsync("RegisterPlayer", _botName, _tournamentId);
            Console.WriteLine($"[Register] Registered to tournament {_tournamentId} successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Register] Failed to register: {ex.Message}");
        }
    }

    private async Task SubscribeTournament(Guid tournamentId)
    {
        await _signalRClient!.InvokeAsync("SpectateTournament", _tournamentId);
    }

    private async Task RegisterAsync()
    {
        await _signalRClient!.InvokeAsync("RegisterPlayer", _botName, _tournamentId);
    }

    protected virtual void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
        Console.WriteLine($"Match started {playerId} (Player) vs {opponentId} | Player is {mark} | Starts = {starts}");
    }

    protected virtual void OnOpponentMoved(Guid matchId, int row, int col)
    {
        Console.WriteLine($"Opponent played at ({row}, {col})");
    }

    protected virtual void OnMatchEnded(GameResult result)
    {
        Console.WriteLine($"Match ended. Winner: {result.WinnerId?.ToString() ?? "Draw"}");
    }

    protected virtual void OnBoardUpdated(Guid matchId, Mark[][] board)
    {
        Console.WriteLine("Board updated.");
    }

    protected abstract Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board);

    protected virtual void OnAuthenticated(TournamentAuthResponse auth)
    {
        Console.WriteLine($"Authenticated as {PlayerId}");
    }

    Task<(int row, int col)> IBot.MakeMoveAsync(Guid matchId, Mark[][] board)
    {
        return MakeMove(matchId, board);
    }

    void IBot.OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
        OnMatchStarted(matchId, playerId, opponentId, mark, starts);
    }

    void IBot.OnOpponentMoved(Guid matchId, int row, int col)
    {
        OnOpponentMoved(matchId, row, col);
    }

    void IBot.OnMatchEnded(GameResult result)
    {
        OnMatchEnded(result);
    }

    void IBot.OnBoardUpdated(Guid matchId, Mark[][] board)
    {
        OnBoardUpdated(matchId, board);
    }
}