using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Reflection;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.DTOs;

namespace TicTacToe.Tournament.BasePlayer;

public abstract class BasePlayerClient : IBot
{
    private readonly GameConsoleUI _consoleUI = new();
    private readonly IHttpClient _httpClient;
    private readonly Guid _tournamentId;
    private readonly string _botName;
    private readonly string _webAppEndpoint;
    private readonly string _signalREndpoint;
    private TournamentDto _tournament;
    private ISignalRClient? _signalRClient;
    private readonly ISignalRClientBuilder _signalRBuilder;

    public Mark[][]? CurrentBoard { get; protected set; }

    public bool Authenticated { get; protected set; } = false;

    public Guid PlayerId { get; protected set; } = default;

    public Guid UserId { get; protected set; } = default;

    public Guid OpponentId { get; protected set; } = default;

    public string? Token { get; protected set; } = null;

    public string BotName => _botName;

    public Mark Mark { get; protected set; } = Mark.Empty;

    public TournamentDto Tournament
    {
        get { return _tournament; }
        protected set
        {
            _tournament = value;
            if (value != null)
            {
                _consoleUI.TournamentName = value.Name;
                _consoleUI.PlayerA = GetPlayerName(PlayerId);
                _consoleUI.PlayerB = GetPlayerName(OpponentId);
                _consoleUI.TotalPlayers = value?.RegisteredPlayers?.Keys.Count() ?? 0;
                _consoleUI.TotalMatches = value?.Matches?.Count();
                _consoleUI.MatchesFinished = value?.Matches?.Count(m => m.Status == MatchStatus.Finished);
                _consoleUI.MatchesPlanned = value?.Matches?.Count(m => m.Status == MatchStatus.Planned);
                _consoleUI.MatchesOngoing = value?.Matches?.Count(m => m.Status == MatchStatus.Ongoing);
                _consoleUI.MatchesCancelled = value?.Matches?.Count(m => m.Status == MatchStatus.Cancelled);
                _consoleUI.Leaderboard = value
                    ?.Leaderboard
                    ?.Select(k =>
                    new LeaderboardEntry
                    {
                        PlayerId = k.Key,
                        PlayerName = GetPlayerName(k.Key),
                        TotalPoints = k.Value
                    }).ToList();
                _consoleUI.TournamentStatus = value.Status;
            }
        }
    }

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
        _consoleUI.Start();

        await ConnectToSignalRAsync();
        await RegisterAsync();

        _consoleUI.Log($"{_botName} is ready. PlayerId = {UserId}");

        await Task.Delay(Timeout.Infinite);
    }

    public async Task AuthenticateAsync()
    {
        if (UserId != Guid.Empty)
        {
            _httpClient
                .DefaultRequestHeaders
                .Add("X-PlayerId", UserId.ToString());
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
            throw new AccessViolationException($"Authentication failed: {response?.StatusCode}");
        }

        var auth = await response.Content.ReadFromJsonAsync<TournamentAuthResponse>();
        if (auth == null)
        {
            throw new AccessViolationException($"Authentication failed: could not deserialize TournamentAuthResponse.");
        }

        UserId = auth.PlayerId;
        Authenticated = true;
        Token = auth.Token;

        _consoleUI.Log($"Authenticated as {UserId}");

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
            _consoleUI.Log("Reconnecting to SignalR...");
            return Task.CompletedTask;
        };

        _signalRClient.Reconnected += async connectionId =>
        {
            await RegisterToTournamentAsync();
            _consoleUI.Log($"Reconnected to SignalR with connectionId: {connectionId}");
        };

        _signalRClient.Closed += error =>
        {
            _consoleUI.IsPlaying = false;
            _consoleUI.Board = CurrentBoard = null;
            Tournament = null;

            _consoleUI.Log("Connection closed.");
            return Task.CompletedTask;
        };

        await _signalRClient.SubscribeAsync<Guid>("OnRegistered", id =>
        {
            try
            {
                _consoleUI.IsPlaying = false;
                var name = GetPlayerName(id) ?? id.ToString();
                _consoleUI.Log($"{name} has been registered to this tournament");
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid>("OnPlayerRegistered", id =>
        {
            try
            {
                _consoleUI.IsPlaying = false;
                var playerName = GetPlayerName(id) ?? id.ToString();
                _consoleUI.Log($"New player joined: {playerName}");
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentCreated", async id =>
        {
            try
            {
                _consoleUI.IsPlaying = false;
                Tournament = await GetTournamentAsync();
                var name = Tournament?.Name ?? id.ToString();
                _consoleUI.Log($"Tournament created: {name}");
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentUpdated", async id =>
        {
            try
            {
                Tournament = await GetTournamentAsync();
                var name = Tournament?.Name ?? id.ToString();
                _consoleUI.Log($"Tournament updated: {name}");

                if (Tournament?.Status == "Finished" ||
                    Tournament?.Status == "Cancelled")
                {
                    _consoleUI.PlayerA = null;
                    _consoleUI.PlayerB = null;
                    _consoleUI.IsPlaying = false;
                    _consoleUI.Board = CurrentBoard = null;
                    _consoleUI.MatchEndTime = DateTime.Now;
                    _consoleUI.Log($"Tournament {Tournament.Status.ToLower()}!");
                }
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid>("OnTournamentCancelled", async id =>
        {
            try
            {
                Tournament = await GetTournamentAsync();
                _consoleUI.Board = CurrentBoard = null;
                _consoleUI.IsPlaying = false;
                _consoleUI.MatchEndTime = DateTime.Now;
                _consoleUI.PlayerA = null;
                _consoleUI.PlayerB = null;

                var name = Tournament?.Name ?? id.ToString();
                _consoleUI.Log($"Tournament cancelled: {name}");
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<TournamentDto>("OnTournamentStarted", async data =>
        {
            try
            {
                _consoleUI.IsPlaying = false;
                _consoleUI.Board = CurrentBoard = null;
                Tournament = await GetTournamentAsync();
                var name = Tournament?.Name ?? data.Id.ToString();
                _consoleUI.Log("{name} started.");
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid, Guid, Guid, string, bool>("OnMatchStarted", (matchId, playerId, opponentId, markStr, starts) =>
        {
            try
            {
                PlayerId = playerId;
                OpponentId = opponentId;

                _consoleUI.IsPlaying = IsUserPlaying(matchId);

                _consoleUI.PlayerMark = Mark = Enum.Parse<Mark>(markStr);
                _consoleUI.PlayerA = GetPlayerName(playerId) ?? playerId.ToString();
                _consoleUI.PlayerB = GetPlayerName(opponentId) ?? opponentId.ToString();
                _consoleUI.MatchStartTime = DateTime.Now;
                _consoleUI.MatchEndTime = null;

                _consoleUI.Log($"Match started: {playerId} vs {opponentId}, mark: {Mark}, yourTurn: {starts}");
                OnMatchStarted(matchId, playerId, opponentId, Mark, starts);
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid, Guid, Mark[][]>("OnYourTurn", async (matchId, playerId, board) =>
        {
            try
            {
                _consoleUI.CurrentTurn = Mark;
                _consoleUI.Board = CurrentBoard = board;
                var name = GetPlayerName(playerId) ?? playerId.ToString();
                _consoleUI.Log($"{name}: it's your turn!");

                if (UserId == playerId)
                {
                    var move = await MakeMove(matchId, board);
                    _consoleUI.Log($"{name} is playing at ({move.row},{move.col})");

                    if (_consoleUI.IsPlaying)
                    {
                        _consoleUI.CurrentTurn = Mark == Mark.X
                            ? Mark.O
                            : Mark.X;
                    }
                    else
                    {
                        _consoleUI.CurrentTurn = Mark.Empty;
                    }
                    CurrentBoard[move.row][move.col] = Mark;
                    _consoleUI.Board = CurrentBoard = board;
                    await SubmitMove(move.row, move.col);
                    _consoleUI.CurrentTurn = Mark.Empty;
                }
            }
            catch (TimeoutException)
            {
                _consoleUI.Log($"You've lost you turn due to Timeout!");
            }
        });

        await _signalRClient.SubscribeAsync<Guid, int, int>("OnOpponentMoved", (matchId, row, col) =>
        {
            try
            {
                _consoleUI.Log($"Opponent moved at ({row},{col})");
                OnOpponentMoved(matchId, row, col);
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<GameResult>("OnMatchEnded", result =>
        {
            try
            {
                _consoleUI.IsPlaying = false;
                _consoleUI.Board = CurrentBoard = null;
                _consoleUI.MatchEndTime = DateTime.Now;

                if (result.WinnerId.HasValue)
                {
                    var name = GetPlayerName(result.WinnerId.Value) ?? result.WinnerId.ToString();
                    _consoleUI.Log($"Match ended with {name} as Winner");
                }
                else
                {
                    _consoleUI.Log($"Match ended.");
                }

                OnMatchEnded(result);
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Guid, Mark[][]>("OnReceiveBoard", (matchId, board) =>
        {
            try
            {
                _consoleUI.Board = CurrentBoard = board;
                OnBoardUpdated(matchId, CurrentBoard);
            }
            catch (Exception e)
            {
                _consoleUI.Log(e.Message);
            }
        });

        await _signalRClient.SubscribeAsync<Dictionary<Guid, int>>("OnRefreshLeaderboard", leaderboard =>
        {
            _consoleUI.Log("Leaderboard updated.");
            foreach (var entry in leaderboard.OrderByDescending(e => e.Value))
            {
                _consoleUI.Log($" - {entry.Key}: {entry.Value} pts");
            }
        });

        _consoleUI.Log("Connecting to SignalR...");
        await _signalRClient.StartAsync();
        _consoleUI.Log("Connected to SignalR!");

        Tournament = await GetTournamentAsync();

        await SubscribeTournament(_tournamentId);
    }

    private async Task SubmitMove(int row, int col)
    {
        await _signalRClient!.InvokeAsync("SubmitMoveAsync", _tournamentId, row, col);
    }

    private async Task<TournamentDto?> GetTournamentAsync()
    {
        return await GetTournamentAsync(_tournamentId);
    }

    private async Task<TournamentDto?> GetTournamentAsync(Guid tournamentId)
    {
        try
        {
            if (_signalRClient == null)
                return null;

            return await _signalRClient.InvokeAsync<TournamentDto>(
                "GetTournamentAsync",
                tournamentId
            );
        }
        catch (Exception ex)
        {
            _consoleUI.Log(ex.Message);
            return null;
        }
    }

    private async Task RegisterToTournamentAsync()
    {
        if (_signalRClient == null || _signalRClient.State != HubConnectionState.Connected)
        {
            _consoleUI.Log("Cannot register player: not connected to SignalR.");
            return;
        }

        try
        {
            _consoleUI.Log($"Registering {_botName} ({PlayerId}) to tournament {_tournamentId}...");
            await _signalRClient.InvokeAsync("RegisterPlayerAsync", _botName, _tournamentId);
            _consoleUI.Log($"Registered to tournament {_tournamentId} successfully.");
        }
        catch (Exception ex)
        {
            _consoleUI.Log($"Failed to register: {ex.Message}");
        }
    }

    private async Task SubscribeTournament(Guid tournamentId)
    {
        await _signalRClient!.InvokeAsync("SpectateTournamentAsync", _tournamentId);
    }

    private async Task RegisterAsync()
    {
        await _signalRClient!.InvokeAsync("RegisterPlayerAsync", _botName, _tournamentId);
    }

    protected virtual void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
    }

    protected virtual void OnOpponentMoved(Guid matchId, int row, int col)
    {
    }

    protected virtual void OnMatchEnded(GameResult result)
    {
    }

    protected virtual void OnBoardUpdated(Guid matchId, Mark[][] board)
    {
    }

    protected abstract Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board);

    protected virtual void OnAuthenticated(TournamentAuthResponse auth)
    {
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

    protected void ConsoleWrite(string message)
    {
        _consoleUI.Log(message);
    }

    protected T ConsoleRead<T>(string message)
    {
        return _consoleUI.Read<T>(message);
    }

    protected string GetPlayerName(Guid playerId)
    {
        if (_tournament?.RegisteredPlayers.ContainsKey(playerId) ?? false)
        {
            return _tournament?
                .RegisteredPlayers[playerId]?
                .ToString() ?? playerId.ToString();

        }

        return "";
    }

    protected bool IsUserPlaying(Guid matchId)
    {
        if (!(Tournament?.Matches.Any() ?? false))
        {
            return (UserId == PlayerId || UserId == OpponentId);
        }

        var match = Tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
        {
            return (UserId == PlayerId || UserId == OpponentId);
        }

        return UserId == match.PlayerBId || UserId == match.PlayerAId;
    }
}