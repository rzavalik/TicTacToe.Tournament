using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;

namespace TicTacToe.Tournament.Server.Bots
{
    public class DummyPlayerBot : IPlayerBot
    {
        public Guid Id { get; }
        public string Name { get; set; }

        public DummyPlayerBot(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public void OnRegistered(Guid playerId)
        {
            // No-op
        }

        public void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
        {
            // No-op
        }

        public void OnOpponentMoved(int row, int column)
        {
            // No-op
        }

        public Task<(int row, int col)> MakeMoveAsync(Mark[][] board)
        {
            // Dummy implementation always returns an invalid move
            return Task.FromResult((-1, -1));
        }

        public void OnMatchEnded(GameResult result)
        {
            // No-op
        }
    }
}
