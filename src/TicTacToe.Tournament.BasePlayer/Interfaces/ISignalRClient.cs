namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    using Microsoft.AspNetCore.SignalR.Client;
    using TicTacToe.Tournament.Models.DTOs;
    public interface ISignalRClient
    {
        Task StartAsync();
        Task SubscribeAsync<T1>(string method, Action<T1> handler);
        Task SubscribeAsync<T1, T2>(string method, Action<T1, T2> handler);
        Task SubscribeAsync<T1, T2, T3>(string method, Action<T1, T2, T3> handler);
        Task SubscribeAsync<T1, T2, T3, T4>(string method, Action<T1, T2, T3, T4> handler);
        Task SubscribeAsync<T1, T2, T3, T4, T5>(string method, Action<T1, T2, T3, T4, T5> handler);
        Task<TournamentDto?> SpectateTournamentAsync(Guid tournamentId);
        Task SubmitMoveAsync(Guid tournamentId, byte row, byte col);
        Task<TournamentDto?> GetTournamentAsync(Guid tournamentId);
        Task RegisterPlayerAsync(string botName, Guid playerId);

        HubConnectionState State { get; }

        event Func<Exception?, Task> Reconnecting;
        event Func<string?, Task> Reconnected;
        event Func<Exception?, Task> Closed;
    }
}