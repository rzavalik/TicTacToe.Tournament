using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.DumbPlayer;

public class DumbPlayerStrategy : IPlayerStrategy
{
    private readonly Mark _playerMark;
    private readonly Mark _opponentMark;

    public DumbPlayerStrategy(
        Mark playerMark,
        Mark opponentMark)
    {
        _playerMark = playerMark;
        _opponentMark = opponentMark;
    }

    public (int row, int col) MakeMove(Mark[][] board)
    {
        var _rng = new Random();

        var moves = new List<(int row, int col)>();
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (board[r][c] == Mark.Empty)
                    moves.Add((r, c));

        return moves.OrderBy(r => _rng.NextDouble()).FirstOrDefault();
    }
}
