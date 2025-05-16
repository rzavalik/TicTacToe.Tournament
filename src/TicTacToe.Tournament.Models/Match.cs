namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

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

        [JsonConstructor]
        public Match(
            Guid id,
            Guid playerA,
            Guid playerB,
            MatchStatus status,
            DateTime? startTime,
            DateTime? endTime,
            Board board,
            Guid currentTurn,
            Mark? winnerMark,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _id = id;
            _playerA = playerA;
            _playerB = playerB;
            _status = status;
            _startTime = startTime;
            _endTime = endTime;
            _board = board;
            _currentTurn = currentTurn;
            _winnerMark = winnerMark;
        }

        [JsonInclude]
        public Guid Id
        {
            get => _id;
            private set
            {
                if (_id != value)
                {
                    _id = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public Guid PlayerA
        {
            get => _playerA;
            private set
            {
                if (_playerA != value)
                {
                    _playerA = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public Guid PlayerB
        {
            get => _playerB;
            private set
            {
                if (_playerB != value)
                {
                    _playerB = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
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

        [JsonInclude]
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

        [JsonInclude]
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

        [JsonInclude]
        public TimeSpan? Duration =>
            StartTime.HasValue && EndTime.HasValue
                ? EndTime - StartTime
                : null;

        [JsonInclude]
        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnChanged();
            }
        }

        [JsonInclude]
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

        [JsonInclude]
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

        public void Start()
        {
            Status = MatchStatus.Ongoing;
            StartTime = DateTime.UtcNow;
        }

        public void MakeMove(Guid playerId, byte row, byte col)
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

        public void Walkover(Mark mark)
        {
            Finish(mark == Mark.X
                ? Mark.O
                : Mark.X);
        }

        public void Finish(Mark? mark)
        {
            WinnerMark = mark;
            Finish();
        }

        public void Draw()
        {
            WinnerMark = null;
            Finish();
        }

        public void Finish()
        {
            Status = MatchStatus.Finished;
            EndTime = DateTime.UtcNow;
        }
    }
}
