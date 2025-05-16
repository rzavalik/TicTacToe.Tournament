namespace TicTacToe.Tournament.SmartPlayer
{
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;
    internal class SmartClientStrategy : IPlayerStrategy
    {
        private readonly Mark _playerMark;
        private readonly Mark _opponentMark;
        private readonly Action<string> _consoleWrite;
        private readonly Func<string, byte> _consoleRead;

        public SmartClientStrategy(
            Mark playerMark,
            Mark opponentMark,
            Action<string> consoleWrite,
            Func<string, byte> consoleRead)
        {
            _playerMark = playerMark;
            _opponentMark = opponentMark;
            _consoleWrite = consoleWrite;
            _consoleRead = consoleRead;
        }

        public (byte row, byte col) MakeMove(Mark[][] board)
        {
            _consoleWrite("It's your time to make a move!");

            byte row = 255, col = 255;
            var timeout = TimeSpan.FromSeconds(59);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                var timeLeft = timeout - (DateTime.UtcNow - startTime);

                row = _consoleRead("Enter a Row number (0-2):");
                col = _consoleRead("Enter a Column number (0-2):");

                if (row is >= 0 and <= 2 &&
                    col is >= 0 and <= 2 &&
                    board[row][col] == Mark.Empty)
                {
                    board[row][col] = _playerMark;
                    return ((row, col));
                }

                _consoleWrite("Invalid move. Try again.");
            }

            _consoleWrite("Timeout or invalid input. You've lost by WO.");
            throw new TimeoutException();
        }
    }
}