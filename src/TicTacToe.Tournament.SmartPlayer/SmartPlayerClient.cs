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
            opponentMark: mark == Mark.X ? Mark.O : Mark.X,
            (log) => base.ConsoleWrite(log),
            (message) => base.ConsoleRead<int>(message)
        );
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
            base.ConsoleWrite($"Error in MakeMoveAsync: {ex.Message}");
        }

        return Task.FromResult((-1, -1));
    }
}
