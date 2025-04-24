using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.BasePlayer.Interfaces;

public interface IBoardRenderer
{
    void Draw(Mark[][] board);
}
