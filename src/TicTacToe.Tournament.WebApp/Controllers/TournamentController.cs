using Microsoft.AspNetCore.Mvc;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.DTOs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Security;
using TicTacToe.Tournament.WebApp.Models;

namespace TicTacToe.Tournament.WebApp.Controllers
{
    public class TournamentController : Controller
    {
        const string TournamentHubName = "tournamentHub";
        private readonly ITournamentOrchestratorService _orchestrator;
        private readonly string _signalrConnectionString;
        private readonly IConfiguration _configuration;

        public TournamentController(
            IConfiguration config,
            ITournamentOrchestratorService orchestrator)
        {
            _configuration = config
                ?? throw new ArgumentNullException(nameof(config), "Configuration service cannot be null.");
            _orchestrator = orchestrator
                ?? throw new ArgumentNullException(nameof(orchestrator), "Orchestrator service cannot be null.");

            _signalrConnectionString = config["Azure:SignalR:ConnectionString"]
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
            {
                return NotFound();
            }

            var eTag = tournament.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
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
                _signalrConnectionString,
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

        [HttpGet("tournament/create")]
        public IActionResult CreateTournamentView()
        {
            return View("Create");
        }

        [HttpPost("tournament/new")]
        public async Task<IActionResult> Create([FromBody] CreateTournamentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.MatchRepetition <= 0)
            {
                return BadRequest("Invalid Tournament data.");
            }

            var tournamentId = Guid.NewGuid();
            await _orchestrator.CreateTournamentAsync(tournamentId, request.Name, request.MatchRepetition);

            return Ok(new { id = tournamentId });
        }

        [HttpPost("tournament/{tournamentId}/rename")]
        public async Task<IActionResult> RenameTournament(Guid tournamentId, [FromBody] RenameTournamentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                return BadRequest("Invalid name.");
            }

            await _orchestrator.RenameTournamentAsync(tournamentId, request.NewName);
            return Ok();
        }

        [HttpPost("tournament/{tournamentId}/player/{playerId}/rename")]
        public async Task<IActionResult> RenamePlayer(Guid tournamentId, Guid playerId, [FromBody] RenamePlayerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                return BadRequest("Invalid name.");
            }

            await _orchestrator.RenamePlayerAsync(tournamentId, playerId, request.NewName);
            return Ok();
        }

        [HttpPost("tournament/{tournamentId}/cancel")]
        public async Task<IActionResult> Cancel(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status != TournamentStatus.Planned.ToString("G") &&
                tournament.Status != TournamentStatus.Ongoing.ToString("G"))
            {
                return BadRequest($"Cannot cancel because it is {tournament.Status}.");
            }

            await _orchestrator.CancelTournamentAsync(tournamentId);
            return Ok(new { message = $"Tournament {tournamentId} cancelled." });
        }

        [HttpPost("tournament/{tournamentId}/start")]
        public async Task<IActionResult> Start(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status != TournamentStatus.Planned.ToString("G"))
            {
                return BadRequest($"Cannot start because it is {tournament.Status}.");
            }

            if ((tournament?.RegisteredPlayers?.Count ?? 0) < 2)
            {
                return BadRequest("Cannot start with less than 2 players.");
            }

            await _orchestrator.StartTournamentAsync(tournamentId);
            return Ok(new { message = $"Tournament {tournamentId} started." });
        }

        [HttpDelete("tournament/{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status != TournamentStatus.Finished.ToString("G") &&
                tournament.Status != TournamentStatus.Cancelled.ToString("G"))
            {
                return BadRequest("Only Finished or Cancelled tournaments can be deleted.");
            }

            await _orchestrator.DeleteTournamentAsync(tournamentId);
            return Ok(new { message = $"Tournament {tournamentId} deleted." });
        }

        [HttpGet("tournament/{tournamentId}/matches")]
        public async Task<IActionResult> GetMatches(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
            {
                return NoContent();
            }

            var eTag = tournament
                .Matches
                .OrderByDescending(m => m.ETag)
                .Select(e => e.ETag)
                .First();

            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(tournament.Matches);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}")]
        public async Task<IActionResult> GetMatch(Guid tournamentId, Guid matchId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);

            var match = ResultMatchDto(tournament, matchId);
            if (match == null)
            {
                return NoContent();
            }

            var eTag = match.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(match);
        }

        [HttpGet("tournament/{tournamentId}/match/current/board")]
        public async Task<IActionResult> GetCurrentBoard(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
            {
                return NoContent();
            }

            var board = await _orchestrator.GetCurrentMatchBoardAsync(tournamentId);
            if (board == null)
            {
                return NotFound();
            }

            var eTag = board.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(board);
        }

        [HttpGet("tournament/{tournamentId}/match/current/players")]
        public async Task<IActionResult> GetCurrentPlayers(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
            {
                return NoContent();
            }

            var players = await _orchestrator.GetCurrentMatchPlayersAsync(tournamentId);
            if (players == null)
            {
                return NotFound();
            }

            var eTag = players.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/match/current")]
        public async Task<IActionResult> GetCurrentMatch(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            var match = ResultMatchDto(tournament);

            if (match == null)
            {
                return NoContent();
            }

            var eTag = match.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(match);
        }

        [HttpGet("tournament/{tournamentId}/players")]
        public async Task<IActionResult> GetTournamentPlayersAsync(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            var players = tournament.RegisteredPlayers
                .Select(p => new { id = p.Key, name = p.Value })
                .ToList();

            var eTag = tournament.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboardAsync(Guid tournamentId)
        {
            var tournament = await _orchestrator.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            var leaderboard = tournament
                .Leaderboard
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.Wins)
                .ThenBy(p => p.Draws)
                .ThenBy(p => p.Losses)
                .ThenBy(p => p.Walkovers)
                .ThenBy(p => p.PlayerName)
                .ToList();

            var eTag = tournament.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(leaderboard);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}/board")]
        public async Task<IActionResult> GetMatchBoard(Guid tournamentId, Guid matchId)
        {
            var board = await _orchestrator.GetMatchBoardAsync(tournamentId, matchId);
            if (board == null)
            {
                return NotFound();
            }

            var eTag = board.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(board);
        }

        [HttpGet("tournament/{tournamentId}/match/{matchId}/players")]
        public async Task<IActionResult> GetMatchPlayers(Guid tournamentId, Guid matchId)
        {
            var players = await _orchestrator.GetMatchPlayersAsync(tournamentId, matchId);
            if (players == null)
            {
                return NoContent();
            }

            var eTag = players.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(players);
        }

        [HttpGet("tournament/{tournamentId}/player/{playerId}")]
        public async Task<IActionResult> GetPlayer(Guid tournamentId, Guid playerId)
        {
            var player = await _orchestrator.GetPlayerAsync(tournamentId, playerId);
            if (player == null)
            {
                return NotFound();
            }

            var eTag = player.ETag;
            if (Request?.Headers?.IfNoneMatch.Any(h => h == eTag) ?? false)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (Response?.Headers != null)
            {
                Response.Headers.ETag = eTag;
            }
            return Ok(player);
        }

        private MatchDto? ResultMatchDto(TournamentDto? tournament, Guid? matchId = null)
        {
            if (tournament == null)
            {
                return null;
            }

            if (tournament.Status == TournamentStatus.Planned.ToString("G"))
            {
                return null;
            }

            var match = tournament.Matches
                .Where(m => matchId.HasValue
                    ? m.Id == matchId.Value
                    : m.Status == MatchStatus.Ongoing)
                .OrderBy(m => m.StartTime)
                .FirstOrDefault();

            if (match == null)
            {
                return null;
            }

            return match;
        }
    }
}