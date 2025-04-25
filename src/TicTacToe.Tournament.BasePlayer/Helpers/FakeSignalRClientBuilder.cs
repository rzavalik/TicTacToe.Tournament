using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class FakeSignalRClientBuilder : ISignalRClientBuilder
{
    ISignalRClient? ISignalRClientBuilder.Build(string endpoint, Func<Task<string?>> accessTokenProvider)
    {
        return default;
    }
}

