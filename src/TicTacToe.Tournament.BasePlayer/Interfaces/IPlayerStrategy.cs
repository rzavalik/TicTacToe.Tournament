using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    public interface IPlayerStrategy
    {
        (int row, int col) MakeMove(Mark[][] board);
    }
}
