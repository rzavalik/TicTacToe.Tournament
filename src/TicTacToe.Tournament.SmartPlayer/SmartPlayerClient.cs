using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.SmartPlayer;

public class SmartPlayerClient : BasePlayerClient
{
    public SmartPlayerClient()
    : base(
        botName: "SmartBot",
        tournamentId: Guid.NewGuid(),
        webAppEndpoint: "http://localhost",
        signalrEndpoint: "http://localhost",
        httpClient: new FakeHttpClient(),
        signalRBuilder: new FakeSignalRClientBuilder())
    { }

    public SmartPlayerClient(
        string botName,
        Guid tournamentId,
        string webAppEndpoint,
        string signalrEndpoint,
        IHttpClient httpClient,
        ISignalRClientBuilder signalRBuilder)
        : base(
            botName,
            tournamentId,
            webAppEndpoint,
            signalrEndpoint,
            httpClient,
            signalRBuilder)
    { }

    protected override void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
        base.OnMatchStarted(matchId, playerId, opponentId, mark, starts);
    }

    protected override void OnOpponentMoved(int row, int col)
    {
        base.OnOpponentMoved(row, col);
    }

    protected override void OnMatchEnded(GameResult result)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Match ended!" + (result.IsDraw ? "It's a draw!" : $"Winner: {result.WinnerId}"));
    }

    protected override async Task<(int row, int col)> MakeMoveAsync(Mark[][] board)
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
            var rowInput = await ReadLineWithTimeoutAsync(timeLeft);
            if (rowInput == null) break;

            timeLeft = timeout - (DateTime.UtcNow - startTime);
            Console.Write($"Enter column (0-2), {timeLeft.Seconds}s left: ");
            var colInput = await ReadLineWithTimeoutAsync(timeLeft);
            if (colInput == null) break;

            if (int.TryParse(rowInput, out row) &&
                int.TryParse(colInput, out col) &&
                row is >= 0 and <= 2 &&
                col is >= 0 and <= 2 &&
                board[row][col] == Mark.Empty)
            {
                CurrentBoard[row][col] = Mark;
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

    protected override void OnBoardUpdated(Mark[][] board)
    {
        DrawBoard();
    }

    private void DrawBoard()
    {
        Console.WriteLine();
        Console.WriteLine("Current board:");

        var boardRenderer = new BoardRenderer(Console.Out);
        boardRenderer.Draw(CurrentBoard);

        Console.WriteLine();
        Console.WriteLine();
    }
}
