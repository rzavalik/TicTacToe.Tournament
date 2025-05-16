namespace TicTacToe.Tournament.Server
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;
    using TicTacToe.Tournament.Models.Interfaces;
    using TicTacToe.Tournament.Server.Hubs;
    using TicTacToe.Tournament.Server.Interfaces;

    public class TournamentManager : ITournamentManager
    {
        private readonly IAzureStorageService _storageService;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly ConcurrentDictionary<Guid, TournamentContext> _tournamentContext;

        public TournamentManager(
            IHubContext<TournamentHub> hubContext,
            IAzureStorageService storageService)
        {
            _hubContext = hubContext;
            _storageService = storageService;
            _tournamentContext = new ConcurrentDictionary<Guid, TournamentContext>();
        }

        ~TournamentManager()
        {
            SaveStateAsync().Wait();
        }

        public async Task LoadFromDataSourceAsync()
        {
            var tournaments = await _storageService.ListTournamentsAsync();
            var tasks = tournaments.Select(tId => GetTournamentContextAsync(tId));
            await Task.WhenAll(tasks);
        }

        public async Task SaveTournamentAsync(Models.Tournament tournament)
        {
            await InitializeContextAsync(tournament);
        }


        public async Task InitializeTournamentAsync(Guid tournamentId, string? name, uint? matchRepetition)
        {
            var tContext = await GetTournamentContextAsync(tournamentId);
            if (tContext == null)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name), "Tournament name cannot be null when creating a new tournament.");
                }

                if (matchRepetition == null)
                {
                    throw new ArgumentNullException(nameof(matchRepetition), "Match repetition cannot be null when creating a new tournament.");
                }


                var tournament = new Models.Tournament(
                    tournamentId,
                    name ?? $"Tournament {DateTime.Now.ToShortDateString()}",
                    matchRepetition.Value);

                tContext = await InitializeContextAsync(tournament);
            }

            if (tContext == null)
            {
                throw new ArgumentNullException(nameof(tContext), "Tournament could not be initialized.");
            }

            await SaveStateAsync(tContext);

            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} initialized.");

            //notify everybody that a new tournament was created
            await _hubContext.Clients.All.SendAsync("OnTournamentCreated", tContext.Tournament.Id);
        }

        private async Task<TournamentContext> InitializeContextAsync(Models.Tournament tournament)
        {
            var tContext = new TournamentContext(_hubContext, tournament);

            await SaveStateAsync(tContext);

            return tContext;
        }

        public async Task RegisterPlayerAsync(Guid tournamentId, IPlayerBot bot)
        {
            var tContext = await GetTournamentContextAsync(tournamentId);
            if (tContext == null)
            {
                throw new ArgumentNullException(nameof(tournamentId), $"Tournament {tournamentId} not found.");
            }

            if (tContext.Tournament.Status != TournamentStatus.Planned)
            {
                Console.WriteLine($"[TournamentManager] Cannot register player after tournament started.");
                return;
            }

            tContext.GameServer.RegisterPlayer(bot);

            await SaveStateAsync(tContext);

            //lets notify the bot itself he's in
            bot.OnRegistered(bot.Id);

            //lets notify the tournament
            await _hubContext.Clients.Group(tournamentId.ToString()).SendAsync("OnRegistered", bot.Id);
            //leaderboard updated
            await _hubContext.Clients.Group(tournamentId.ToString()).SendAsync("OnRefreshLeaderboard", tContext.Tournament.Leaderboard);
        }

        public async Task SubmitMoveAsync(Guid tournamentId, Guid player, byte row, byte col)
        {
            if (!_tournamentContext.TryGetValue(tournamentId, out var tContext))
            {
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
                return;
            }

            tContext.GameServer.SubmitMove(player, row, col);

            await SaveStateAsync(tContext);
        }

        public async Task StartTournamentAsync(Guid tournamentId)
        {
            var tContext = await GetTournamentContextAsync(tournamentId);
            if (tContext == null)
            {
                throw new ArgumentNullException(nameof(tournamentId), $"Tournament {tournamentId} not found.");
            }

            if (tContext.Tournament.Status != TournamentStatus.Planned)
            {
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} already started or finished.");
                return;
            }

            tContext.Tournament.Start();
            tContext.Tournament.InitializeLeaderboard();

            await SaveStateAsync(tContext);

            Console.WriteLine($"[TournamentManager] Starting tournament {tournamentId} with {tContext.Tournament.RegisteredPlayers.Count} players.");

            await Task.WhenAll(
                _hubContext.Clients.Group(tournamentId.ToString()).SendAsync("OnRefreshLeaderboard", tContext.Tournament.Leaderboard),
                OnTournamentStarted(tContext.Tournament),
                SaveStateAsync(tContext)
            );

            //this will hold the game process
            await tContext.GameServer.StartTournamentAsync(tContext.Tournament);
        }

        public async Task RenamePlayerAsync(Guid tournamentId, Guid playerId, string newName)
        {
            if (!_tournamentContext.TryGetValue(tournamentId, out var tContext))
            {
                throw new InvalidOperationException($"Tournament {tournamentId} not found.");
            }

            if (!tContext.Tournament.RegisteredPlayers.ContainsKey(playerId))
            {
                throw new InvalidOperationException($"Player {playerId} not found in tournament {tournamentId}.");
            }

            tContext.Tournament.RegisteredPlayers[playerId] = newName;

            await SaveStateAsync(tContext);
        }

        public async Task CancelTournamentAsync(Guid tournamentId)
        {
            var tContext = await GetTournamentContextAsync(tournamentId);
            if (tContext == null)
            {
                throw new ArgumentNullException(nameof(tournamentId), $"Tournament {tournamentId} not found.");
            }

            if (tContext.Tournament.Status == TournamentStatus.Finished ||
                tContext.Tournament.Status == TournamentStatus.Cancelled)
            {
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} already cancelled or finished.");
                return;
            }

            tContext.Tournament.Cancel();

            await SaveStateAsync(tContext);

            Console.WriteLine($"[TournamentManager] Tournament {tournamentId} cancelled.");

            await OnTournamentCancelled(tournamentId);
        }

        public async Task DeleteTournamentAsync(Guid tournamentId)
        {
            lock (_tournamentContext)
            {
                _tournamentContext.TryRemove(tournamentId, out _);

                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} deleted from memory.");
            }

            try
            {
                await _storageService.DeleteTournamentAsync(tournamentId);
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} deleted from Azure Blob Storage.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TournamentManager] Failed to delete tournament {tournamentId} from storage: {ex.Message}");
            }
        }

        public async Task<TournamentContext?> GetTournamentContextAsync(Guid tournamentId)
        {
            lock (_tournamentContext)
            {
                TournamentContext? tContext;

                if (!_tournamentContext.TryGetValue(tournamentId, out tContext))
                {
                    try
                    {
                        var storageRequest = _storageService.LoadTournamentStateAsync(tournamentId);
                        storageRequest.Wait();

                        if (storageRequest.Result.Tournament != null)
                        {
                            tContext = new TournamentContext(
                                _hubContext,
                                storageRequest.Result.Tournament);

                            tContext.SaveState =
                                async (context) => await SaveStateAsync(context);

                            _tournamentContext.AddOrUpdate(
                                tournamentId,
                                tContext,
                                (key, oldValue) => oldValue = tContext);
                        }
                    }
                    catch
                    {
                        tContext = null;
                    }
                }

                if (tContext == null)
                {
                    return null;
                }

                _tournamentContext.AddOrUpdate(
                    tournamentId,
                    tContext,
                    (key, oldValue) => oldValue = tContext);

                return tContext;
            }
        }

        public Task<bool> TournamentExistsAsync(Guid tournamentId)
        {
            return _storageService.TournamentExistsAsync(tournamentId);
        }

        public Models.Tournament? GetTournament(Guid tournamentId)
        {
            var tournament = _tournamentContext.TryGetValue(tournamentId, out var tContext)
                ? tContext.Tournament
                : null;

            if (tournament == null)
            {
                return tournament;
            }

            if (tournament.Status == TournamentStatus.Cancelled)
            {
                if (tournament.Matches.Any(m => m.Status == MatchStatus.Ongoing || m.Status == MatchStatus.Planned))
                {
                    foreach (var match in tournament.Matches.Where(m => m.Status == MatchStatus.Ongoing || m.Status == MatchStatus.Planned))
                    {
                        match.Status = MatchStatus.Cancelled;
                    }
                }
            }
            else if (tournament.Status != TournamentStatus.Finished &&
                     tournament.Matches.All(m => m.Status == MatchStatus.Finished))
            {
                tournament.Status = TournamentStatus.Finished;
            }

            if (tContext != null)
            {
                SaveStateAsync(tContext).Wait();
            }

            return tournament;
        }

        public IEnumerable<Models.Tournament> GetAllTournaments() => _tournamentContext.Values.Select(v => v.Tournament);

        public Dictionary<Guid, int> GetLeaderboard(Guid tournamentId)
        {
            if (!_tournamentContext.TryGetValue(tournamentId, out var tContext))
            {
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
                return new();
            }

            var leaderboard = new Dictionary<Guid, int>();

            foreach (var match in tContext.Tournament.Matches.Where(m => m.Status == MatchStatus.Finished))
            {
                var winner = match.WinnerMark;

                var scoreA = MatchScore.Draw;
                var scoreB = MatchScore.Draw;

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

        private async Task SaveStateAsync(TournamentContext tContext)
        {
            await tContext.Lock.WaitAsync();

            try
            {
                await SaveStateUnsafeAsync(tContext);

                _tournamentContext.AddOrUpdate(
                    tContext.Tournament.Id,
                    tContext,
                    (key, oldValue) => tContext
                );
            }
            finally
            {
                tContext.Lock.Release();
            }
        }

        private async Task SaveStateUnsafeAsync(TournamentContext tContext)
        {
            var pendingMoves = tContext?.GameServer?.GetPendingMoves()
                ?? new ConcurrentDictionary<Guid, ConcurrentQueue<(byte, byte)>>();

            await _storageService.SaveTournamentStateAsync(tContext);
        }

        public async Task RenameTournamentAsync(Guid tournamentId, string newName)
        {
            if (_tournamentContext.TryGetValue(tournamentId, out var tContext))
            {
                tContext.Tournament.Name = newName;
                await SaveStateAsync(tContext);
                return;
            }

            throw new InvalidOperationException($"Tournament {tournamentId} not found.");
        }

        private Task OnTournamentCancelled(Guid tournamentId) =>
            _hubContext.Clients.All.SendAsync("OnTournamentCancelled", tournamentId);

        private Task OnTournamentStarted(Models.Tournament tournament) =>
            _hubContext.Clients.Group(tournament.Id.ToString()).SendAsync("OnTournamentStarted", new TournamentDto(tournament));

        public async Task SaveStateAsync()
        {
            try
            {
                foreach (var tContext in _tournamentContext.Values)
                {
                    try { await SaveStateAsync(tContext); }
                    catch { }
                }
            }
            catch { }
        }
    }
}
