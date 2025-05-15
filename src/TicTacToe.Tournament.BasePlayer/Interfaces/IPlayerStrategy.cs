using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    public interface IPlayerStrategy
    {
        (byte row, byte col) MakeMove(Mark[][] board);
    }
}
