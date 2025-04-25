using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.SmartPlayer;

internal class SmartClientStrategy : IPlayerStrategy
{
    private readonly Mark _playerMark;
    private readonly Mark _opponentMark;

    public SmartClientStrategy(
        Mark playerMark,
        Mark opponentMark)
    {
        _playerMark = playerMark;
        _opponentMark = opponentMark;
    }

    public (int row, int col) MakeMove(Mark[][] board)
    {
        Console.WriteLine("");
        Console.WriteLine("It's your time to make a move!");
        Console.WriteLine("");
        Console.WriteLine("");

        int row = -1, col = -1;
        var timeout = TimeSpan.FromSeconds(50);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var timeLeft = timeout - (DateTime.UtcNow - startTime);

            Console.Write($"\nEnter row (0-2), {timeLeft.Seconds}s left: ");

            var rowInputRequest = ReadLineWithTimeoutAsync(timeLeft);
            rowInputRequest.Wait();
            var rowInput = rowInputRequest.Result;
            if (rowInput == null) break;

            timeLeft = timeout - (DateTime.UtcNow - startTime);
            Console.Write($"Enter column (0-2), {timeLeft.Seconds}s left: ");
            var colInputRequest = ReadLineWithTimeoutAsync(timeLeft);
            colInputRequest.Wait();
            var colInput = colInputRequest.Result;
            if (colInput == null) break;

            if (int.TryParse(rowInput, out row) &&
                int.TryParse(colInput, out col) &&
                row is >= 0 and <= 2 &&
                col is >= 0 and <= 2 &&
                board[row][col] == Mark.Empty)
            {
                board[row][col] = _playerMark;
                return ((row, col));
            }

            Console.WriteLine("Invalid move. Try again.");
        }

        Console.WriteLine("Timeout or invalid input. You've lost by WO.");
        throw new TimeoutException();
    }

    private static async Task<string?> ReadLineWithTimeoutAsync(TimeSpan timeout)
    {
        var inputTask = Task.Run(() => Console.ReadLine());
        var timeoutTask = Task.Delay(timeout);

        var completedTask = await Task.WhenAny(inputTask, timeoutTask);
        return completedTask == inputTask ? await inputTask : null;
    }
}
