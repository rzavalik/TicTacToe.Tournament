using Microsoft.AspNetCore.SignalR.Client;
using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class SignalRClientBuilder : ISignalRClientBuilder
{
    public ISignalRClient Build(string endpoint, Func<Task<string>> accessTokenProvider)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(endpoint, options =>
            {
                options.AccessTokenProvider = accessTokenProvider;
            })
            .WithAutomaticReconnect()
            .Build();

        return new SignalRClient(hub);
    }
}
