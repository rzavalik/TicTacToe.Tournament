namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    using TicTacToe.Tournament.Models;

    public interface IPlayerStrategy
    {
        (byte row, byte col) MakeMove(Mark[][] board);
    }
}
