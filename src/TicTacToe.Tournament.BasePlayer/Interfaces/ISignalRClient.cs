using Microsoft.AspNetCore.SignalR.Client;

namespace TicTacToe.Tournament.BasePlayer.Interfaces;

public interface ISignalRClient
{
    Task StartAsync();
    Task InvokeAsync(string method, params object[] args);
    Task SubscribeAsync<T1>(string method, Action<T1> handler);
    Task SubscribeAsync<T1, T2>(string method, Action<T1, T2> handler);
    Task SubscribeAsync<T1, T2, T3>(string method, Action<T1, T2, T3> handler);
    Task SubscribeAsync<T1, T2, T3, T4>(string method, Action<T1, T2, T3, T4> handler);
    Task SubscribeAsync<T1, T2, T3, T4, T5>(string method, Action<T1, T2, T3, T4, T5> handler);

    HubConnectionState State { get; }

    event Func<Exception?, Task> Reconnecting;
    event Func<string?, Task> Reconnected;
    event Func<Exception?, Task> Closed;
}