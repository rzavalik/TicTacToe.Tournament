using System.Collections.Concurrent;

namespace TicTacToe.Tournament.Models.Interfaces;

public interface IGameServer
{
    IReadOnlyDictionary<Guid, IPlayerBot> RegisteredPlayers { get; }

    void RegisterPlayer(IPlayerBot player);

    void SubmitMove(Guid playerId, int row, int col);

    IPlayerBot? GetBotById(Guid playerId);

    ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> GetPendingMoves();

    void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> moves);

    Task StartTournamentAsync(Tournament tournament);

    void GenerateMatches();

    void InitializeLeaderboard();
}
