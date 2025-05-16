namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    using TicTacToe.Tournament.Models;

    public interface IBoardRenderer
    {
        void Draw(Mark[][] board);
    }
}