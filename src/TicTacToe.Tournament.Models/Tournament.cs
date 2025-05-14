namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class Tournament : BaseModel
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private List<Match> _matches = new();
        private TournamentStatus _status = TournamentStatus.Planned;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private uint _matchRepetition = 1;
        private IDictionary<Guid, string> _registeredPlayers = new Dictionary<Guid, string>();
        private IDictionary<Guid, int> _leaderboard = new Dictionary<Guid, int>();
        private Guid? _champion;

        public Tournament() : base()
        {

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

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnChanged();
                }
            }
        }

        public List<Match> Matches
        {
            get => _matches;
            set
            {
                _matches = value ?? new List<Match>();
                OnChanged();
            }
        }

        public TournamentStatus Status
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

        public uint MatchRepetition
        {
            get => _matchRepetition;
            set
            {
                if (_matchRepetition != value)
                {
                    _matchRepetition = value;
                    OnChanged();
                }
            }
        }

        public IDictionary<Guid, string> RegisteredPlayers
        {
            get => _registeredPlayers;
            set
            {
                _registeredPlayers = value ?? new Dictionary<Guid, string>();
                OnChanged();
            }
        }

        public IDictionary<Guid, int> Leaderboard
        {
            get => _leaderboard;
            set
            {
                _leaderboard = value ?? new Dictionary<Guid, int>();
                OnChanged();
            }
        }

        public Guid? Champion
        {
            get => _champion;
            set
            {
                if (_champion != value)
                {
                    _champion = value;
                    OnChanged();
                }
            }
        }

        public void InitializeLeaderboard()
        {
            Leaderboard = RegisteredPlayers.ToDictionary(
                entry => entry.Key,
                _ => 0
            );
        }

        public void AgreggateScoreToPlayer(Guid playerId, MatchScore score)
        {
            var current = Leaderboard.ContainsKey(playerId) ? Leaderboard[playerId] : 0;
            var updated = current + (int)score;

            if (!Leaderboard.ContainsKey(playerId) || updated != current)
            {
                Leaderboard[playerId] = updated;
                OnChanged();
            }
        }
    }
}
