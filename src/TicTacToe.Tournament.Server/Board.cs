using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server;

public class Board
{
    private readonly Mark[][] _grid = new Mark[3][];

    public Board()
    {
        _grid = new Mark[3][];
        for (int i = 0; i < 3; i++)
        {
            _grid[i] = new Mark[3];

            for (int j = 0; j < 3; j++)
            {
                _grid[i][j] = Mark.Empty;
            }
        }
    }

    public bool IsValidMove(int row, int col)
    {
        return row >= 0 
            && row < 3 
            && col >= 0 
            && col < 3 
            && _grid[row][col] == Mark.Empty;
    }

    public void ApplyMove(int row, int col, Mark mark)
    {
        if (!IsValidMove(row, col))
            throw new InvalidOperationException("Invalid move");

        _grid[row][col] = mark;
    }

    public bool IsGameOver()
    {
        return GetWinner().HasValue || !_grid.Any(row => row.Any(cell => cell == Mark.Empty));
    }

    public Mark? GetWinner()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_grid[i][0] != Mark.Empty &&
                _grid[i][0] == _grid[i][1] && _grid[i][1] == _grid[i][2])
                return _grid[i][0];

            if (_grid[0][i] != Mark.Empty &&
                _grid[0][i] == _grid[1][i] && _grid[1][i] == _grid[2][i])
                return _grid[0][i];
        }

        if (_grid[0][0] != Mark.Empty && _grid[0][0] == _grid[1][1] && _grid[1][1] == _grid[2][2])
            return _grid[0][0];

        if (_grid[0][2] != Mark.Empty && _grid[0][2] == _grid[1][1] && _grid[1][1] == _grid[2][0])
            return _grid[0][2];

        return null;
    }

    public Mark[][] GetState()
    {
        var copy = new Mark[3][];
        for (int i = 0; i < 3; i++)
        {
            copy[i] = new Mark[3];
            for (int j = 0; j < 3; j++)
            {
                copy[i][j] = _grid[i][j];
            }
        }
        return copy;
    }
}
