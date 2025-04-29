using Microsoft.AspNetCore.SignalR;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server.Bots;

public class RemotePlayerBot : IPlayerBot
{
    private readonly IClientProxy _client;

    private TaskCompletionSource<(int row, int col)>? _moveSource;

    public Guid Id { get; }
    public string Name { get; set; }
    public Guid TournamentId { get; set; }

    public RemotePlayerBot(
        Guid id, 
        string name, 
        IClientProxy client,
        Guid tournamentId)
    {
        Id = id;
        Name = name;
        _client = client;
        TournamentId = tournamentId;
    }

    public void OnRegistered(Guid playerId)
    {
        _client.SendAsync("OnRegistered", playerId);
    }

    public void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
        _client.SendAsync("OnMatchStarted", matchId, playerId, opponentId, mark.ToString("G"), starts);
    }

    public void OnOpponentMoved(Guid matchId, int row, int col)
    {
        _client.SendAsync("OnOpponentMoved", matchId, row, col);
    }

    public void OnMatchEnded(GameResult result)
    {
        _client.SendAsync("OnMatchEnded", result);
    }

    public async Task<(int row, int col)> MakeMoveAsync(Guid matchId, Mark[][] board)
    {
        _moveSource = new TaskCompletionSource<(int row, int col)>();

        await _client.SendAsync("OnBoardUpdated", matchId, board);

        var completed = await Task.WhenAny(_moveSource.Task, Task.Delay(TimeSpan.FromSeconds(60)));

        if (completed != _moveSource.Task)
            throw new TimeoutException("Bot did not respond in time.");

        return await _moveSource.Task;
    }

    public void SubmitMove(int row, int col)
    {
        if (_moveSource is { Task.IsCompleted: false })
        {
            _moveSource.TrySetResult((row, col));
        }
    }
}