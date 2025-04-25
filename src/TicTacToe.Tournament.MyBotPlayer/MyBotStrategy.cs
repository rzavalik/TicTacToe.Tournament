using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.MyBotPlayer
{
    public class MyBotStrategy : IPlayerStrategy
    {
        private readonly Mark _playerMark;
        private readonly Mark _opponentMark;

        public MyBotStrategy(
            Mark playerMark,
            Mark opponentMark)
        {
            _playerMark = playerMark;
            _opponentMark = opponentMark;
        }

        public (int row, int col) MakeMove(Mark[][] board)
        {
            // Add your strategy here
            throw new NotImplementedException();
        }
    }
}
