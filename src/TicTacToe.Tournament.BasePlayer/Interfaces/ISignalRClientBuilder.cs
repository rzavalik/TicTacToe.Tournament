namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    public interface ISignalRClientBuilder
    {
        ISignalRClient? Build(string endpoint, Func<Task<string?>> accessTokenProvider);
    }
}