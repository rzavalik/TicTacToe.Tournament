namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class GameResult : BaseModel
    {
        private Guid _matchId;
        private Guid? _winnerId;
        private Board _board;
        private bool _isDraw;

        public GameResult() : base()
        {
            _board = new Board();
        }

        public Guid MatchId
        {
            get => _matchId;
            set
            {
                if (value != _matchId)
                {
                    _matchId = value;
                    OnChanged();
                }
            }
        }
        public Guid? WinnerId
        {
            get => _winnerId;
            set
            {
                if (value != _winnerId)
                {
                    _winnerId = value;
                    OnChanged();
                }
            }
        }
        public Board Board
        {
            get => _board;
            set
            {
                if (_board != value)
                {
                    _board = value ?? throw new ArgumentNullException(nameof(value));
                    OnChanged();
                }
            }
        }

        public bool IsDraw
        {
            get => _isDraw;
            set
            {
                if (_isDraw != value)
                {
                    _isDraw = value;
                    OnChanged();
                }
            }
        }
    }
}