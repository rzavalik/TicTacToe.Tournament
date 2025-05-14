namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

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
        private List<LeaderboardEntry> _leaderboard = new List<LeaderboardEntry>();
        private Guid? _champion;

        public Tournament() : base()
        {

        }

        public Tournament(
            Guid id,
            string name,
            uint matchRepetition) : this()
        {
            _id = id;
            _name = name;
            _matchRepetition = Math.Min(matchRepetition, 9);
        }

        [JsonConstructor]
        public Tournament(
            Guid id,
            string name,
            List<Match> matches,
            TournamentStatus status,
            DateTime? startTime,
            DateTime? endTime,
            uint matchRepetition,
            IDictionary<Guid, string> registeredPlayers,
            IList<LeaderboardEntry> leaderboard,
            Guid? champion,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _id = id;
            _name = name;
            _matches = matches ?? new List<Match>();
            _status = status;
            _startTime = startTime;
            _endTime = endTime;
            _matchRepetition = matchRepetition;
            _registeredPlayers = registeredPlayers ?? new Dictionary<Guid, string>();
            _leaderboard = leaderboard?.ToList() ?? new List<LeaderboardEntry>();
            _champion = champion;
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

        [JsonInclude]
        public List<Match> Matches
        {
            get => _matches;
            private set
            {
                _matches = value ?? new List<Match>();
                OnChanged();
            }
        }

        [JsonInclude]
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
        public uint MatchRepetition
        {
            get => _matchRepetition;
            private set
            {
                if (_matchRepetition != value)
                {
                    _matchRepetition = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public IDictionary<Guid, string> RegisteredPlayers
        {
            get => _registeredPlayers;
            private set
            {
                _registeredPlayers = value ?? new Dictionary<Guid, string>();
                OnChanged();
            }
        }

        [JsonInclude]
        public IList<LeaderboardEntry> Leaderboard
        {
            get => _leaderboard;
            private set
            {
                _leaderboard = value?.ToList() ?? new List<LeaderboardEntry>();
                OnChanged();
            }
        }

        [JsonInclude]
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
            Leaderboard.Clear();
            foreach (var player in RegisteredPlayers)
            {
                Leaderboard.Add(new LeaderboardEntry(player.Value, player.Key));
            }
            OnChanged();
        }

        public void AgreggateScoreToPlayer(Guid playerId, MatchScore score)
        {
            var current = Leaderboard.First(p => p.PlayerId == playerId);
            current.RegisterResult(score);
            OnChanged();
        }

        public void RegisterPlayer(Guid id, string name)
        {
            if (RegisteredPlayers.ContainsKey(id) && RegisteredPlayers[id] == name)
            {
                return;
            }

            RegisteredPlayers[id] = name;
            OnChanged();
        }
    }
}
