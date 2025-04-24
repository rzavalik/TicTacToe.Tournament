using System.Collections.Concurrent;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server.Interfaces;

public interface IAzureStorageService
{
    Task<IEnumerable<Guid>> ListTournamentsAsync();

    Task SaveTournamentStateAsync(
        Guid tournamentId,
        Models.Tournament tournament,
        Dictionary<Guid, IPlayerBot> players,
        Dictionary<Guid, Guid> playerTournamentMap,
        ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> pendingMoves);

    Task<(Models.Tournament? Tournament, List<PlayerInfo>? PlayerInfos, Dictionary<Guid, Guid>? Map, ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>>? Moves)>
        LoadTournamentStateAsync(Guid tournamentId);
}
