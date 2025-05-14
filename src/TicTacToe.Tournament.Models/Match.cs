namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class Match : BaseModel
    {
        private Guid _id = Guid.NewGuid();
        private Guid _playerA;
        private Guid _playerB;
        private MatchStatus _status = MatchStatus.Planned;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private Board _board = new Board();
        private Guid _currentTurn;
        private Mark? _winnerMark = Mark.Empty;

        public Match() : base()
        {
        }

        public Match(Guid playerA, Guid playerB) : this()
        {
            PlayerA = playerA;
            PlayerB = playerB;
        }

        public Guid Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnChanged();
                }
            }
        }

        public Guid PlayerA
        {
            get => _playerA;
            set
            {
                if (_playerA != value)
                {
                    _playerA = value;
                    OnChanged();
                }
            }
        }

        public Guid PlayerB
        {
            get => _playerB;
            set
            {
                if (_playerB != value)
                {
                    _playerB = value;
                    OnChanged();
                }
            }
        }

        public MatchStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnChanged();
                }
            }
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnChanged();
                }
            }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnChanged();
                }
            }
        }

        public TimeSpan? Duration =>
            StartTime.HasValue && EndTime.HasValue
                ? EndTime - StartTime
                : null;

        public Board Board
        {
            get => _board;
            set
            {
                _board = value;
                OnChanged();
            }
        }

        public Guid CurrentTurn
        {
            get => _currentTurn;
            set
            {
                if (_currentTurn != value)
                {
                    _currentTurn = value;
                    OnChanged();
                }
            }
        }

        public Mark? WinnerMark
        {
            get => _winnerMark;
            set
            {
                if (_winnerMark != value)
                {
                    _winnerMark = value;
                    OnChanged();
                }
            }
        }

        public void MakeMove(Guid playerId, int row, int col)
        {
            if (Status == MatchStatus.Finished)
            {
                throw new InvalidOperationException("Match already finished.");
            }

            if (playerId != PlayerA && playerId != PlayerB)
            {
                throw new AccessViolationException("Invalid player making move.");
            }

            if (CurrentTurn != Guid.Empty && CurrentTurn != playerId)
            {
                throw new InvalidOperationException("Not your turn.");
            }

            var mark = GetPlayerMark(playerId);
            Board.ApplyMove(row, col, mark);

            if (StartTime == null)
            {
                StartTime = DateTime.UtcNow;
            }

            if (Status != MatchStatus.Ongoing)
            {
                Status = MatchStatus.Ongoing;
            }

            if (HasWinner(mark))
            {
                WinnerMark = mark;
                Status = MatchStatus.Finished;
                EndTime = DateTime.UtcNow;
                return;
            }

            CurrentTurn = (playerId == PlayerA) ? PlayerB : PlayerA;
        }

        public Mark GetPlayerMark(Guid playerId)
        {
            if (playerId == PlayerA)
            {
                return Mark.X;
            }

            if (playerId == PlayerB)
            {
                return Mark.O;
            }

            throw new InvalidOperationException("Invalid player.");
        }

        private bool HasWinner(Mark mark)
        {
            var winnerMark = Board.GetWinner();

            return mark == winnerMark;
        }
    }
}
