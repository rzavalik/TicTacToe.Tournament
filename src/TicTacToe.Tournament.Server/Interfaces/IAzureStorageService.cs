namespace TicTacToe.Tournament.Server.Interfaces
{
    using System.Collections.Concurrent;
    using TicTacToe.Tournament.Models;

    public interface IAzureStorageService
    {
        Task<IEnumerable<Guid>> ListTournamentsAsync();

        Task SaveTournamentStateAsync(TournamentContext tContext);

        Task<(
            Models.Tournament? Tournament,
            List<PlayerInfo>? PlayerInfos,
            Dictionary<Guid, Guid>? Map,
            ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>>? Moves)>
            LoadTournamentStateAsync(Guid tournamentId);

        Task DeleteTournamentAsync(Guid tournamentId);

        Task<bool> TournamentExistsAsync(Guid tournamentId);
    }
}