using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer.Interfaces;

public interface IBot
{
    Task<(byte row, byte col)> MakeMoveAsync(Guid matchId, Mark[][] board);
    void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts);
    void OnOpponentMoved(Guid matchId, byte row, byte col);
    void OnMatchEnded(GameResult result);
    void OnBoardUpdated(Guid matchId, Mark[][] board);
}
