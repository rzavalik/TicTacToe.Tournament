namespace TicTacToe.Tournament.Server.Interfaces
{
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.Interfaces;
    public interface ITournamentManager
    {
        Task LoadFromDataSourceAsync();
        Task SaveTournamentAsync(Tournament tournament);
        Task InitializeTournamentAsync(Guid tournamentId, string? name, uint? matchRepetition);
        Task RegisterPlayerAsync(Guid tournamentId, IPlayerBot bot);
        Task SubmitMove(Guid tournamentId, Guid playerId, int row, int col);
        Task StartTournamentAsync(Guid tournamentId);
        Task RenamePlayerAsync(Guid tournamentId, Guid playerId, string newName);
        Task CancelTournamentAsync(Guid tournamentId);
        Task DeleteTournamentAsync(Guid tournamentId);
        Task<TournamentContext?> GetTournamentContextAsync(Guid tournamentId);
        Task<bool> TournamentExistsAsync(Guid tournamentId);
        Tournament? GetTournament(Guid tournamentId);
        IEnumerable<Tournament> GetAllTournaments();
        Dictionary<Guid, int> GetLeaderboard(Guid tournamentId);
        Task RenameTournamentAsync(Guid tournamentId, string newName);
    }
}
