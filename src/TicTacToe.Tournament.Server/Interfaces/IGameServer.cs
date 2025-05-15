namespace TicTacToe.Tournament.Models.Interfaces
{
    using System.Collections.Concurrent;
    using TicTacToe.Tournament.Models;

    public interface IGameServer
    {
        Tournament Tournament { get; }

        IReadOnlyDictionary<Guid, IPlayerBot> RegisteredPlayers { get; }

        void RegisterPlayer(IPlayerBot player);

        void InitializeLeaderboard();

        ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>> GetPendingMoves();

        void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>> moves);

        Task StartTournamentAsync(Tournament tournament);

        void SubmitMove(Guid playerId, byte row, byte col);

        IPlayerBot? GetBotById(Guid playerId);

        void GenerateMatches();
    }
}