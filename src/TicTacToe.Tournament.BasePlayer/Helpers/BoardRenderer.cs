using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class BoardRenderer : IBoardRenderer
{
    private readonly TextWriter _output;

    public BoardRenderer(TextWriter output)
    {
        _output = output;
    }

    public void Draw(Mark[][] board)
    {
        for (int row = 0; row < 3; row++)
        {
            _output?.WriteLine($"{RenderMark(board[row][0])} | {RenderMark(board[row][1])} | {RenderMark(board[row][2])}");
        }
    }

    private string RenderMark(Mark mark)
    {
        return mark switch
        {
            Mark.X => "X",
            Mark.O => "O",
            _ => " "
        };
    }
}
