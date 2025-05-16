namespace TicTacToe.Tournament.BasePlayer
{
    using System.Net.Http.Json;
    using System.Reflection;
    using Microsoft.AspNetCore.SignalR.Client;
    using TicTacToe.Tournament.Auth;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;
    public abstract class BasePlayerClient : IBot
    {
        protected IGameConsoleUI _consoleUI = new GameConsoleUI();
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

        public TournamentDto? Tournament
        {
            get { return _tournament; }
            protected set
            {
                if (value != null)
                {
                    _tournament = value;
                    _consoleUI?.LoadTournament(_tournament);
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
            _botName = botName?.Trim();
            if (string.IsNullOrEmpty(_botName))
            {
                _botName = Environment.MachineName;
            }

            _tournamentId = tournamentId;

            _webAppEndpoint = webAppEndpoint.TrimEnd('/');
            if (string.IsNullOrEmpty(_webAppEndpoint))
            {
                throw new ArgumentNullException(nameof(webAppEndpoint), "Web app endpoint cannot be null or empty.");
            }
            else
            {
                var uri = new Uri(_webAppEndpoint);
                _webAppEndpoint = uri.GetLeftPart(UriPartial.Authority) + "/client/?hub=tournamentHub";
            }

            _signalREndpoint = signalrEndpoint;
            if (string.IsNullOrEmpty(_signalREndpoint))
            {
                throw new ArgumentNullException(nameof(webAppEndpoint), "SignalR endpoint cannot be null or empty.");
            }
            else
            {
                var uri = new Uri(_signalREndpoint);
                _signalREndpoint = uri.GetLeftPart(UriPartial.Authority) + "/client/?hub=tournamentHub";
            }

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _signalRBuilder = signalRBuilder ?? throw new ArgumentNullException(nameof(signalRBuilder));
        }

        public async Task StartAsync()
        {
            _consoleUI?.Start();

            await ConnectToSignalRAsync();
            await RegisterAsync();

            _consoleUI?.Log($"{_botName} is ready. PlayerId = {UserId}");

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

            _consoleUI?.Log($"Authenticated as {UserId}");

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
                _consoleUI?.Log("Reconnecting to SignalR...");
                return Task.CompletedTask;
            };

            _signalRClient.Reconnected += async connectionId =>
            {
                await RegisterToTournamentAsync();
                _consoleUI?.Log($"Reconnected to SignalR with connectionId: {connectionId}");
            };

            _signalRClient.Closed += error =>
            {
                _consoleUI?.Log("Connection closed.");
                return Task.CompletedTask;
            };

            await _signalRClient.SubscribeAsync<Guid>("OnRegistered", async id =>
            {
                try
                {
                    _consoleUI?.SetIsPlaying(false);
                    Tournament = await GetTournamentAsync();
                    var name = GetPlayerName(id) ?? id.ToString();
                    _consoleUI?.Log($"{name} has been registered to this tournament");
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid>("OnPlayerRegistered", async id =>
            {
                try
                {
                    _consoleUI?.SetIsPlaying(false);
                    Tournament = await GetTournamentAsync();
                    var playerName = GetPlayerName(id) ?? id.ToString();
                    _consoleUI?.Log($"New player joined: {playerName}");
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid>("OnTournamentCreated", async id =>
            {
                try
                {
                    _consoleUI?.SetIsPlaying(false);
                    Tournament = await GetTournamentAsync();
                    var name = Tournament?.Name ?? id.ToString();
                    _consoleUI?.Log($"Tournament created: {name}");
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid>("OnTournamentUpdated", async id =>
            {
                try
                {
                    Tournament = await GetTournamentAsync();
                    var name = Tournament?.Name ?? id.ToString();
                    _consoleUI?.Log($"Tournament updated: {name}");

                    if (Tournament?.Status == "Finished" ||
                        Tournament?.Status == "Cancelled")
                    {
                        CurrentBoard = null;
                        _consoleUI?.SetPlayerA(null);
                        _consoleUI?.SetPlayerB(null);
                        _consoleUI?.SetIsPlaying(false);
                        _consoleUI?.SetBoard(CurrentBoard);
                        _consoleUI?.SetMatchEndTime(DateTime.Now);
                        _consoleUI?.Log($"Tournament {Tournament.Status.ToLower()}!");
                    }
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid>("OnTournamentCancelled", async id =>
            {
                try
                {
                    Tournament = await GetTournamentAsync();
                    _consoleUI?.SetBoard(CurrentBoard = null);
                    _consoleUI?.SetIsPlaying(false);
                    _consoleUI?.SetMatchEndTime(DateTime.Now);
                    _consoleUI?.SetPlayerA(null);
                    _consoleUI?.SetPlayerB(null);

                    var name = Tournament?.Name ?? id.ToString();
                    _consoleUI?.Log($"Tournament cancelled: {name}");
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<TournamentDto>("OnTournamentStarted", async data =>
            {
                try
                {
                    _consoleUI?.SetIsPlaying(false);
                    _consoleUI?.SetBoard(CurrentBoard = null);
                    Tournament = await GetTournamentAsync();
                    var name = Tournament?.Name ?? data.Id.ToString();
                    _consoleUI?.Log($"{name} started.");
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid, Guid, Guid, string, bool>("OnMatchStarted", (matchId, playerId, opponentId, markStr, starts) =>
            {
                try
                {
                    PlayerId = playerId;
                    OpponentId = opponentId;

                    _consoleUI?.SetIsPlaying(IsUserPlaying(matchId));
                    _consoleUI?.SetPlayerMark(Mark = Enum.Parse<Mark>(markStr));
                    _consoleUI?.SetPlayerA(GetPlayerName(playerId) ?? playerId.ToString());
                    _consoleUI?.SetPlayerB(GetPlayerName(opponentId) ?? opponentId.ToString());
                    _consoleUI?.SetMatchStartTime(DateTime.Now);
                    _consoleUI?.SetMatchEndTime(null);
                    _consoleUI?.Log($"Match started: {playerId} vs {opponentId}, mark: {Mark}, yourTurn: {starts}");

                    OnMatchStarted(matchId, playerId, opponentId, Mark, starts);
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid, Guid, Mark[][]>("OnYourTurn", async (matchId, playerId, board) =>
            {
                try
                {
                    _consoleUI?.SetCurrentTurn(Mark);
                    _consoleUI?.SetBoard(CurrentBoard = board);

                    var name = GetPlayerName(playerId) ?? playerId.ToString();
                    _consoleUI?.Log($"{name}: it's your turn!");

                    if (UserId == playerId)
                    {
                        var move = await MakeMove(matchId, board);
                        _consoleUI?.Log($"{name} is playing at ({move.row},{move.col})");

                        if (_consoleUI != null)
                        {
                            if (_consoleUI.IsPlaying)
                            {
                                _consoleUI?.SetCurrentTurn(Mark == Mark.X
                                    ? Mark.O
                                    : Mark.X);
                            }
                            else
                            {
                                _consoleUI?.SetCurrentTurn(Mark.Empty);
                            }
                        }

                        CurrentBoard[move.row][move.col] = Mark;

                        _consoleUI?.SetBoard(CurrentBoard = board);
                        await SubmitMove(move.row, move.col);
                        _consoleUI?.SetCurrentTurn(Mark.Empty);
                    }
                }
                catch (TimeoutException)
                {
                    _consoleUI?.Log($"You've lost you turn due to Timeout!");
                }
            });

            await _signalRClient.SubscribeAsync<Guid, byte, byte>("OnOpponentMoved", (matchId, row, col) =>
            {
                try
                {
                    _consoleUI?.Log($"Opponent moved at ({row},{col})");
                    OnOpponentMoved(matchId, row, col);
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<GameResult>("OnMatchEnded", result =>
            {
                try
                {
                    _consoleUI?.SetIsPlaying(false);
                    _consoleUI?.SetBoard(CurrentBoard = null);
                    _consoleUI?.SetMatchEndTime(DateTime.Now);

                    if (result.WinnerId.HasValue)
                    {
                        var name = GetPlayerName(result.WinnerId.Value) ?? result.WinnerId.ToString();
                        _consoleUI?.Log($"Match ended with {name} as Winner");
                    }
                    else
                    {
                        _consoleUI?.Log($"Match ended.");
                    }

                    OnMatchEnded(result);
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Guid, Mark[][]>("OnReceiveBoard", (matchId, board) =>
            {
                try
                {
                    CurrentBoard = board;
                    _consoleUI?.SetBoard(CurrentBoard);

                    OnBoardUpdated(matchId, CurrentBoard);
                }
                catch (Exception e)
                {
                    _consoleUI?.Log(e.Message);
                }
            });

            await _signalRClient.SubscribeAsync<Dictionary<Guid, int>>("OnRefreshLeaderboard", leaderboard =>
            {
                _consoleUI?.Log("Leaderboard updated.");
                foreach (var entry in leaderboard.OrderByDescending(e => e.Value))
                {
                    _consoleUI?.Log($" - {entry.Key}: {entry.Value} pts");
                }
            });

            _consoleUI?.Log("Connecting to SignalR...");
            await _signalRClient.StartAsync();
            _consoleUI?.Log("Connected to SignalR!");

            Tournament = await GetTournamentAsync();

            await SubscribeTournament(_tournamentId);
        }

        private async Task SubmitMove(byte row, byte col)
        {
            await _signalRClient!.SubmitMoveAsync(_tournamentId, row, col);
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
                {
                    return null;
                }

                Tournament = await _signalRClient.GetTournamentAsync(tournamentId);

                return Tournament;
            }
            catch (Exception ex)
            {
                _consoleUI?.Log(ex.Message);
                return null;
            }
        }

        private async Task RegisterToTournamentAsync()
        {
            if (_signalRClient == null || _signalRClient.State != HubConnectionState.Connected)
            {
                _consoleUI?.Log("Cannot register player: not connected to SignalR.");
                return;
            }

            try
            {
                _consoleUI?.Log($"Registering {_botName} ({PlayerId}) to tournament {_tournamentId}...");
                await _signalRClient.RegisterPlayerAsync(_botName, _tournamentId);
                _consoleUI?.Log($"Registered to tournament {_tournamentId} successfully.");
            }
            catch (Exception ex)
            {
                _consoleUI?.Log($"Failed to register: {ex.Message}");
            }
        }

        private async Task SubscribeTournament(Guid tournamentId)
        {
            var tournament = await _signalRClient!.SpectateTournamentAsync(tournamentId);
            if (tournament != null)
            {
                Tournament = tournament;
            }
        }

        private async Task RegisterAsync()
        {
            await _signalRClient!.RegisterPlayerAsync(_botName, _tournamentId);
        }

        protected virtual void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
        {
        }

        protected virtual void OnOpponentMoved(Guid matchId, byte row, byte col)
        {
        }

        protected virtual void OnMatchEnded(GameResult result)
        {
        }

        protected virtual void OnBoardUpdated(Guid matchId, Mark[][] board)
        {
        }

        protected abstract Task<(byte row, byte col)> MakeMove(Guid matchId, Mark[][] board);

        protected virtual void OnAuthenticated(TournamentAuthResponse auth)
        {
        }

        Task<(byte row, byte col)> IBot.MakeMoveAsync(Guid matchId, Mark[][] board)
        {
            return MakeMove(matchId, board);
        }

        void IBot.OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
        {
            OnMatchStarted(matchId, playerId, opponentId, mark, starts);
        }

        void IBot.OnOpponentMoved(Guid matchId, byte row, byte col)
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
            _consoleUI?.Log(message);
        }

        protected T ConsoleRead<T>(string message)
        {
            if (_consoleUI == null)
            {
                return default;
            }

            return _consoleUI.Read<T>(message);
        }

        protected string? GetPlayerName(Guid playerId)
        {
            if (_tournament?.RegisteredPlayers.ContainsKey(playerId) ?? false)
            {
                return _tournament?
                    .RegisteredPlayers[playerId]?
                    .ToString() ?? (playerId == PlayerId ? _botName : playerId.ToString());

            }

            if (playerId == PlayerId)
            {
                return _botName;
            }

            return null;
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


        public static string GetPlayerName(string[] args)
        {
            var name = "Bot";

            if (args.Contains("--name"))
            {
                var nameInput = args[Array.IndexOf(args, "--name") + 1]?.Trim();
                if (!string.IsNullOrEmpty(nameInput))
                {
                    name = nameInput;
                }
            }
            else
            {
                Console.Write("Enter your Player Name: ");
                name = Console.ReadLine();
            }

            return name ?? Environment.MachineName;
        }

        public static Guid GetTournamentId(string[] args)
        {
            Guid tournamentId;

            if (args.Contains("--tournament-id"))
            {
                var tournamentIdInput = args[Array.IndexOf(args, "--tournament-id") + 1]?.Trim();
                tournamentId = Guid.TryParse(tournamentIdInput, out var parsedId)
                    ? parsedId
                    : throw new FormatException("Invalid TournamentId");
            }
            else
            {
                Console.Write("Enter the Tournament ID: ");
                var tournamentIdInput = Console.ReadLine();
                tournamentId = Guid.TryParse(tournamentIdInput, out var parsedId)
                    ? parsedId
                    : throw new FormatException("Invalid TournamentId");
            }

            return tournamentId;
        }
    }
}