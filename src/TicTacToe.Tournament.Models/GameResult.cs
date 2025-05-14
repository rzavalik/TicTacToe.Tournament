namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class GameResult : BaseModel
    {
        private Guid _matchId;
        private Guid? _winnerId;
        private Board _board;
        private bool _isDraw;

        public GameResult() : base()
        {
            _matchId = Guid.NewGuid();
            _board = new Board();
        }

        public GameResult(Match match) : base()
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            _matchId = match.Id;
            _winnerId = match.WinnerMark == Mark.X
                ? match.PlayerA
                : match.PlayerB;
            _board = match.Board;
            _isDraw = false;
        }

        public GameResult(
            Guid matchId,
            Guid? winnerId,
            Board board,
            bool isDraw) : base()
        {
            _matchId = matchId;
            _winnerId = winnerId;
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _isDraw = isDraw;
        }

        [JsonConstructor]
        public GameResult(
            Guid matchId,
            Guid? winnerId,
            Board board,
            bool isDraw,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _matchId = matchId;
            _winnerId = winnerId;
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _isDraw = isDraw;
        }

        [JsonInclude]
        public Guid MatchId
        {
            get => _matchId;
            private set
            {
                if (value != _matchId)
                {
                    _matchId = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
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

        [JsonInclude]
        public Board Board
        {
            get => _board;
            private set
            {
                if (_board != value)
                {
                    _board = value ?? throw new ArgumentNullException(nameof(value));
                    OnChanged();
                }
            }
        }

        [JsonInclude]
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