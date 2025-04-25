using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.DumbPlayer;

public class DumbPlayerClient : BasePlayerClient
{
    private IPlayerStrategy? _strategy;

    public DumbPlayerClient()
    : base(
        botName: "DumbBot",
        tournamentId: Guid.NewGuid(),
        webAppEndpoint: "http://localhost",
        signalrEndpoint: "http://localhost",
        httpClient: new FakeHttpClient(),
        signalRBuilder: new FakeSignalRClientBuilder())
    { }

    public DumbPlayerClient(
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

        _strategy = new DumbPlayerStrategy(
            playerMark: mark,
            opponentMark: mark == Mark.X ? Mark.O : Mark.X
        );
    }

    protected override Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board)
    {
        try
        {
            if (_strategy != null)
            {
                return Task.FromResult(_strategy.MakeMove(board));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MakeMoveAsync: {ex.Message}");
        }

        return Task.FromResult((-1, -1));
    }
}