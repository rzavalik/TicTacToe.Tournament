namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class Board : BaseModel
    {
        private Mark[][] _grid = Empty;
        private List<Movement> _movements = new List<Movement>();

        public static Mark[][] Empty =>
        [
            [Mark.Empty, Mark.Empty, Mark.Empty],
            [Mark.Empty, Mark.Empty, Mark.Empty],
            [Mark.Empty, Mark.Empty, Mark.Empty]
        ];

        public Board() : base()
        {
            _grid = Empty;
        }

        public Board(Mark[][] board) : base()
        {
            _grid = board;
        }

        public Board(
            Mark[][] board,
            List<Movement> movements) : base()
        {
            _grid = board;
            _movements = movements;
        }

        public Board(
            Mark[][] state,
            List<Movement> movements,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _grid = state;
            _movements = movements;
        }

        public bool IsValidMove(byte row, byte col)
        {
            return row >= 0
                && row < 3
                && col >= 0
                && col < 3
                && _grid[row][col] == Mark.Empty;
        }

        public void ApplyMove(byte row, byte col, Mark mark)
        {
            if (!IsValidMove(row, col))
            {
                throw new InvalidOperationException("Invalid move");
            }

            _grid[row][col] = mark;
            _movements.Add(
                new Movement(row, col, mark)
            );

            OnChanged();
        }

        public bool IsGameOver()
        {
            return GetWinner().HasValue || !_grid.Any(row => row.Any(cell => cell == Mark.Empty));
        }

        public Mark? GetWinner()
        {
            for (var i = 0; i < 3; i++)
            {
                if (_grid[i][0] != Mark.Empty &&
                    _grid[i][0] == _grid[i][1] && _grid[i][1] == _grid[i][2])
                {
                    return _grid[i][0];
                }

                if (_grid[0][i] != Mark.Empty &&
                    _grid[0][i] == _grid[1][i] && _grid[1][i] == _grid[2][i])
                {
                    return _grid[0][i];
                }
            }

            if (_grid[0][0] != Mark.Empty && _grid[0][0] == _grid[1][1] && _grid[1][1] == _grid[2][2])
            {
                return _grid[0][0];
            }

            if (_grid[0][2] != Mark.Empty && _grid[0][2] == _grid[1][1] && _grid[1][1] == _grid[2][0])
            {
                return _grid[0][2];
            }

            return null;
        }

        [JsonInclude]
        public Mark[][] State
        {
            get => _grid;
            private set
            {
                if (value != _grid)
                {
                    _grid = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public List<Movement> Movements
        {
            get => _movements;
            private set
            {
                if (value != _movements)
                {
                    _movements = value;
                    OnChanged();
                }
            }
        }

        public Mark[][] GetState()
        {
            var copy = new Mark[3][];
            for (var i = 0; i < 3; i++)
            {
                copy[i] = new Mark[3];
                for (var j = 0; j < 3; j++)
                {
                    copy[i][j] = _grid[i][j];
                }
            }
            return copy;
        }
    }
}