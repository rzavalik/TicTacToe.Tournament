using Microsoft.AspNetCore.SignalR.Client;
using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class SignalRClient : ISignalRClient
{
    private readonly HubConnection _conn;

    public SignalRClient(HubConnection conn) => _conn = conn;

    public HubConnectionState State => _conn.State;

    public Task StartAsync() => _conn.StartAsync();

    public Task InvokeAsync(string method, params object[] args) =>
        _conn.InvokeCoreAsync(method, typeof(object), args);

    public event Func<Exception?, Task> Reconnecting
    {
        add => _conn.Reconnecting += value;
        remove => _conn.Reconnecting -= value;
    }

    public event Func<string?, Task> Reconnected
    {
        add => _conn.Reconnected += value;
        remove => _conn.Reconnected -= value;
    }

    public event Func<Exception?, Task> Closed
    {
        add => _conn.Closed += value;
        remove => _conn.Closed -= value;
    }

    public Task SubscribeAsync<T1>(string method, Action<T1> handler)
    {
        _conn.On(method, handler);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T1, T2>(string method, Action<T1, T2> handler)
    {
        _conn.On(method, handler);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T1, T2, T3>(string method, Action<T1, T2, T3> handler)
    {
        _conn.On(method, handler);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T1, T2, T3, T4>(string method, Action<T1, T2, T3, T4> handler)
    {
        _conn.On(method, handler);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T1, T2, T3, T4, T5>(string method, Action<T1, T2, T3, T4, T5> handler)
    {
        _conn.On(method, handler);
        return Task.CompletedTask;
    }
}
