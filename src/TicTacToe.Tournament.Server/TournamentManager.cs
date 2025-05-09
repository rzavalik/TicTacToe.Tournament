﻿namespace TicTacToe.Tournament.Server
{
    using Microsoft.AspNetCore.SignalR;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
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


                var tournament = new Models.Tournament
                {
                    Id = tournamentId,
                    Name = name ?? $"Tournament {DateTime.Now.ToShortDateString()}",
                    Status = TournamentStatus.Planned,
                    RegisteredPlayers = new Dictionary<Guid, string>(),
                    Matches = new List<Match>(),
                    MatchRepetition = matchRepetition.Value,
                    Leaderboard = new Dictionary<Guid, int>(),
                };

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

        public Task SubmitMove(Guid tournamentId, Guid player, int row, int col)
        {
            if (!_tournamentContext.TryGetValue(tournamentId, out var tContext))
            {
                Console.WriteLine($"[TournamentManager] Tournament {tournamentId} not found.");
                return Task.CompletedTask;
            }

            tContext.GameServer.SubmitMove(player, row, col);

            return Task.CompletedTask;
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

            tContext.Tournament.Status = TournamentStatus.Ongoing;
            tContext.Tournament.StartTime = DateTime.UtcNow;
            tContext.Tournament.InitializeLeaderboard();

            await SaveStateAsync(tContext);

            Console.WriteLine($"[TournamentManager] Starting tournament {tournamentId} with {tContext.Tournament.RegisteredPlayers.Count} players.");

            await Task.WhenAll(
                tContext.GameServer.StartTournamentAsync(tContext.Tournament),
                _hubContext.Clients.Group(tournamentId.ToString()).SendAsync("OnRefreshLeaderboard", tContext.Tournament.Leaderboard),
                OnTournamentStarted(tContext.Tournament)
            );
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

            tContext.Tournament.Status = TournamentStatus.Cancelled;
            tContext.Tournament.EndTime = DateTime.UtcNow;

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
                            tContext = new TournamentContext(_hubContext, storageRequest.Result.Tournament);
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
            => _tournamentContext.TryGetValue(tournamentId, out var tContext)
            ? tContext.Tournament
            : null;

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
                ?? new ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>();

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
            _hubContext.Clients.Group(tournament.Id.ToString()).SendAsync("OnTournamentStarted", new TournamentDto
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
            });
    }
}