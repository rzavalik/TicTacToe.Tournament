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

        ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> GetPendingMoves();

        void LoadPendingMoves(ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> moves);

        Task StartTournamentAsync(Tournament tournament);

        void SubmitMove(Guid playerId, int row, int col);

        IPlayerBot? GetBotById(Guid playerId);

        void GenerateMatches();
    }
}