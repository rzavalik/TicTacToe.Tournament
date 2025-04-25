using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer.Interfaces;

public interface IBot
{
    Task<(int row, int col)> MakeMoveAsync(Guid matchId, Mark[][] board);
    void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts);
    void OnOpponentMoved(Guid matchId, int row, int col);
    void OnMatchEnded(GameResult result);
    void OnBoardUpdated(Guid matchId, Mark[][] board);
}
