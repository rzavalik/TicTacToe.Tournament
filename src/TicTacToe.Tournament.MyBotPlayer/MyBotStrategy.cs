using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.MyBotPlayer
{
    public class MyBotStrategy : IPlayerStrategy
    {
        private readonly Mark _playerMark;
        private readonly Mark _opponentMark;
        private readonly Action<string> _consoleWrite;
        private readonly Func<string, int> _consoleRead;

        public MyBotStrategy(
            Mark playerMark,
            Mark opponentMark,
            Action<string> consoleWrite,
            Func<string, int> consoleRead)
        {
            _playerMark = playerMark;
            _opponentMark = opponentMark;
            _consoleWrite = consoleWrite;
            _consoleRead = consoleRead;
        }

        public (byte row, byte col) MakeMove(Mark[][] board)
        {
            // Add your strategy here
            throw new NotImplementedException();
        }
    }
}
