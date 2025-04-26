using TicTacToe.Tournament.Models.DTOs;

namespace TicTacToe.Tournament.Server.Interfaces
{
    public interface ITournamentOrchestratorService
    {
        Task<TournamentDto?> GetTournamentAsync(Guid tournamentId);
        Task<IEnumerable<TournamentSummaryDto>> GetAllTournamentsAsync();
        Task CreateTournamentAsync(Guid tournamentId);
        Task StartTournamentAsync(Guid tournamentId);
        Task CancelTournamentAsync(Guid tournamentId);
        Task<bool> TournamentExistsAsync(Guid tournamentId);
        Task SpectateTournamentAsync(Guid tournamentId);
        Task<IEnumerable<MatchDto>> GetMatchesAsync(Guid tournamentId);
        Task<MatchBoardDto?> GetCurrentMatchBoardAsync(Guid tournamentId);
        Task<MatchPlayersDto?> GetCurrentMatchPlayersAsync(Guid tournamentId);
        Task<MatchBoardDto?> GetMatchBoardAsync(Guid tournamentId, Guid matchId);
        Task<MatchPlayersDto?> GetMatchPlayersAsync(Guid tournamentId, Guid matchId);
        Task<PlayerDto?> GetPlayerAsync(Guid tournamentId, Guid playerId);
    }
}
