namespace TicTacToe.Tournament.Server.Interfaces;

public interface ITournamentManager
{
    Task InitializeTournamentAsync(Guid tournamentId, string? name, uint? matchRepetition);

    Task RegisterPlayerAsync(Guid tournamentId, Models.Interfaces.IPlayerBot bot);

    Task StartTournamentAsync(Guid tournamentId);

    Task CancelTournamentAsync(Guid tournamentId);

    Task DeleteTournamentAsync(Guid tournamentId);

    Task<TournamentContext> GetTournamentContextAsync(Guid tournamentId);

    Models.Tournament? GetTournament(Guid tournamentId);

    IEnumerable<Models.Tournament> GetAllTournaments();

    Dictionary<Guid, int> GetLeaderboard(Guid tournamentId);

    Task RenameTournamentAsync(Guid tournamentId, string newName);

    Task RenamePlayerAsync(Guid tournamentId, Guid playerId, string newName);

    Task SubmitMove(Guid tournamentId, Guid player, int row, int col);

    Task<bool> TournamentExistsAsync(Guid tournamentId);

    Task LoadFromDataSourceAsync();

    Task SaveTournamentAsync(Models.Tournament tContext);
}
