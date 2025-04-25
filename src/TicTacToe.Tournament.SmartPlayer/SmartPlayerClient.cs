using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.SmartPlayer;

public class SmartPlayerClient : BasePlayerClient
{
    private IPlayerStrategy? _strategy;

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

        _strategy = new SmartClientStrategy(
            playerMark: mark,
            opponentMark: mark == Mark.X ? Mark.O : Mark.X
        );
    }

    protected override void OnOpponentMoved(Guid matchId, int row, int col)
    {
        base.OnOpponentMoved(matchId, row, col);
    }

    protected override void OnMatchEnded(GameResult result)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Match ended!" + (result.IsDraw ? "It's a draw!" : $"Winner: {result.WinnerId}"));
    }

    protected override Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board)
    {
        try
        {
            if (_strategy != null)
            {
                return Task.FromResult(_strategy!.MakeMove(board));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MakeMoveAsync: {ex.Message}");
        }

        return Task.FromResult((-1, -1));
    }

    protected override void OnBoardUpdated(Guid matchId, Mark[][] board)
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
