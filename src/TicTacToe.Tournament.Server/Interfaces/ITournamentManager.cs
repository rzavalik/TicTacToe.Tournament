namespace TicTacToe.Tournament.Server.Interfaces;

public interface ITournamentManager
{
    Task InitializeTournamentAsync(Guid tournamentId, string? name = null);

    Task RegisterPlayerAsync(Guid tournamentId, Models.Interfaces.IPlayerBot bot);

    Task StartTournamentAsync(Guid tournamentId);

    Task CancelTournament(Guid tournamentId);

    Task<Models.Tournament?> GetOrLoadTournamentAsync(Guid tournamentId);

    Task<GameServer?> GetOrLoadGameServerAsync(Guid tournamentId);

    bool TournamentExists(Guid tournamentId);

    Models.Tournament? GetTournament(Guid tournamentId);

    IReadOnlyCollection<Models.Tournament> GetAllTournaments();

    Dictionary<Guid, int> GetLeaderboard(Guid tournamentId);

    GameServer? GetGameServer(Guid tournamentId);

    GameServer? GetGameServerForPlayer(Guid playerId);
}
