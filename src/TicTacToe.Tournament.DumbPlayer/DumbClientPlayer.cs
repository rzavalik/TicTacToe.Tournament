using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.DumbPlayer;

public class DumbPlayerClient : BasePlayerClient
{
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

    protected override Task<(int row, int col)> MakeMoveAsync(Mark[][] board)
    {
        var _rng = new Random();

        var moves = new List<(int row, int col)>();
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (board[r][c] == Mark.Empty)
                    moves.Add((r, c));

        return Task.FromResult(moves.OrderBy(r => _rng.NextDouble()).FirstOrDefault());
    }
}