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

    public (byte row, byte col) MakeMove(Mark[][] board)
    {
        var _rng = new Random();

        var moves = new List<(byte row, byte col)>();
        for (byte r = 0; r < 3; r++)
        {
            for (byte c = 0; c < 3; c++)
            {
                if (board[r][c] == Mark.Empty)
                {
                    moves.Add((r, c));
                }
            }
        }

        return moves.OrderBy(r => _rng.NextDouble()).FirstOrDefault();
    }
}
