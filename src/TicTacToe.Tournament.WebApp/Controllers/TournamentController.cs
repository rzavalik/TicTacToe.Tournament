using Microsoft.AspNetCore.Mvc;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.DTOs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Security;

namespace TicTacToe.Tournament.WebApp.Controllers
{
    public class TournamentController : Controller
    {
        const string TournamentHubName = "tournamentHub";
        private readonly string _signalREndpoint;
        private readonly string _signalRAccessKey;
        private readonly ITournamentOrchestratorService _orchestrator;
        private readonly IConfiguration _configuration;

        public TournamentController(
            IConfiguration config,
            ITournamentOrchestratorService orchestrator)
        {
            _configuration = config
                ?? throw new ArgumentNullException(nameof(config), "Configuration service cannot be null.");
            _orchestrator = orchestrator
                ?? throw new ArgumentNullException(nameof(orchestrator), "Orchestrator service cannot be null.");
            _signalREndpoint = config["Azure:SignalR:Endpoint"]
                ?? throw new ArgumentNullException(nameof(config), "SignalR Endpoint must be present in the ConnectionString.");
            _signalRAccessKey = config["Azure:SignalR:AccessKey"]
                ?? throw new ArgumentNullException(nameof(config), "SignalR AccessKey must be present in the ConnectionString.");
        }

        [HttpGet("tournament")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("tournament/list")]
        public async Task<IActionResult> List()
        {
            var tournaments = await _orchestrator.GetAllTournamentsAsync();
            return Ok(tournaments);
        }

        [HttpGet("tournament/view/{tournamentId}")]
        public IActionResult Details(Guid tournamentId)
        {
            ViewData["TournamentId"] = tournamentId;
            _orchestrator.SpectateTournamentAsync(tournamentId);

            return View();
        }

        [HttpGet("tournament/{tournamentId}")]
        public async Task<IActionResult> GetTournament(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            return Ok(tournament);
        }

        [HttpPost("tournament/{tournamentId}/authenticate")]
        public async Task<IActionResult> AuthenticateAsync(Guid tournamentId, [FromBody] TournamentAuthRequest request)
        {
            request.TournamentId = tournamentId;

            var header = Request?.Headers["X-PlayerId"].FirstOrDefault();
            var playerId = Guid.TryParse(header, out var parsedId) ? parsedId : Guid.NewGuid();

            var tournamentExists = await _orchestrator.TournamentExistsAsync(tournamentId);
            if (!tournamentExists)
            {
                return NotFound($"Tournament {request?.TournamentId} does not exist.");
            }

            var token = SignalRAccessHelper.GenerateSignalRAccessToken(
                _signalREndpoint,
                _signalRAccessKey,
                TournamentHubName,
                playerId.ToString(),
                tournamentId);

            return Ok(new TournamentAuthResponse
            {
                Success = true,
                PlayerId = playerId,
                TournamentId = tournamentId,
                Token = token,
                Message = $"Connected to tournament {tournamentId} as {request.PlayerName} ({playerId} - {request.MachineName}) | {request.AgentName}"
            });
        }

        [HttpPost("tournament/new")]
        public async Task<IActionResult> Create()
        {
            var tournamentId = Guid.NewGuid();
            await _orchestrator.CreateTournamentAsync(tournamentId);

            return Ok(new { id = tournamentId });
        }

        [HttpPost("tournament/{tournamentId}/cancel")]
        public async Task<IActionResult> Cancel(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            if (tournament.Status != TournamentStatus.Planned.ToString("G") &&
                tournament.Status != TournamentStatus.Ongoing.ToString("G"))
                return BadRequest($"Cannot cancel because it is {tournament.Status}.");

            await _orchestrator.CancelTournamentAsync(tournamentId);
            return Ok(new { message = $"Tournament {tournamentId} cancelled." });
        }

        [HttpPost("tournament/{tournamentId}/start")]
        public async Task<IActionResult> Start(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            if (tournament.Status != TournamentStatus.Planned.ToString("G"))
                return BadRequest($"Cannot start because it is {tournament.Status}.");

            if ((tournament?.RegisteredPlayers?.Count ?? 0) < 2)
                return BadRequest("Cannot start with less than 2 players.");

            await _orchestrator.StartTournamentAsync(tournamentId);
            return Ok(new { message = $"Tournament {tournamentId} started." });
        }

        [HttpGet("tournament/{tournamentId}/matches")]
        public async Task<IActionResult> GetMatches(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
                return NoContent();

            var matches = await _orchestrator.GetMatchesAsync(tournamentId);
            return Ok(matches);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}")]
        public async Task<IActionResult> GetMatch(Guid tournamentId, Guid matchId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);

            return ResultMatchDto(tournament, matchId);
        }

        [HttpGet("tournament/{tournamentId}/match/current/board")]
        public async Task<IActionResult> GetCurrentBoard(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
                return NoContent();

            var board = await _orchestrator.GetCurrentMatchBoardAsync(tournamentId);
            return board == null
                ? NotFound()
                : Ok(board);
        }

        [HttpGet("tournament/{tournamentId}/match/current/players")]
        public async Task<IActionResult> GetCurrentPlayers(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
                return NoContent();

            var players = await _orchestrator.GetCurrentMatchPlayersAsync(tournamentId);
            return players == null
                ? NotFound()
                : Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/match/current")]
        public async Task<IActionResult> GetCurrentMatch(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            return ResultMatchDto(tournament);
        }

        [HttpGet("tournament/{tournamentId}/players")]
        public async Task<IActionResult> GetTournamentPlayersAsync(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            var players = tournament.RegisteredPlayers
                .Select(p => new { id = p.Key, name = p.Value })
                .ToList();

            return Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboardAsync(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
                return NotFound();

            var leaderboard = tournament
                .Leaderboard
                .Select(g => new
                {
                    id = g.Key,
                    name = tournament.RegisteredPlayers[g.Key] ?? "Unknown",
                    score = g.Value
                })
                .OrderByDescending(p => p.score)
                .ToList();

            return Ok(leaderboard);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}/board")]
        public async Task<IActionResult> GetMatchBoard(Guid tournamentId, Guid matchId)
        {
            var board = await _orchestrator.GetMatchBoardAsync(tournamentId, matchId);
            return board == null
                ? NotFound()
                : Ok(board);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}/players")]
        public async Task<IActionResult> GetMatchPlayers(Guid tournamentId, Guid matchId)
        {
            var players = await _orchestrator.GetMatchPlayersAsync(tournamentId, matchId);
            return players == null
                ? NotFound()
                : Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/player/{playerId}")]
        public async Task<IActionResult> GetPlayer(Guid tournamentId, Guid playerId)
        {
            var player = await _orchestrator.GetPlayerAsync(tournamentId, playerId);
            return player == null
                ? NotFound()
                : Ok(player);
        }

        private IActionResult ResultMatchDto(TournamentDto? tournament, Guid? matchId = null)
        {
            if (tournament == null)
                return NotFound();

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
                return NoContent();

            var match = tournament.Matches
                .Where(m => matchId.HasValue
                    ? m.Id == matchId.Value
                    : m.Status == MatchStatus.Ongoing)
                .OrderBy(m => m.StartTime)
                .FirstOrDefault();

            if (match == null)
                return NoContent();

            var playerAName = tournament.RegisteredPlayers.TryGetValue(match.PlayerAId, out var nameA) ? nameA : "Unknown";
            var playerBName = tournament.RegisteredPlayers.TryGetValue(match.PlayerBId, out var nameB) ? nameB : "Unknown";

            return Ok(new
            {
                id = match.Id,
                playerAId = match.PlayerAId,
                playerAName = playerAName,
                playerBId = match.PlayerBId,
                playerBName = playerBName,
                board = match.Board,
                status = match.Status,
                startTime = match.StartTime,
                endTime = match.EndTime,
                duration = match.Duration
            });
        }
    }
}