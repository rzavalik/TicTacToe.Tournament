namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class LeaderboardEntry : BaseModel
    {
        private string _playerName = string.Empty;
        private Guid _playerId = Guid.Empty;
        private int _totalPoints;
        private uint _wins;
        private uint _draws;
        private uint _losses;
        private uint _walkovers;

        public LeaderboardEntry() : base()
        {
            _playerId = Guid.NewGuid();
        }

        public LeaderboardEntry(string playerName, Guid playerId) : base()
        {
            _playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
            _playerId = playerId;
        }

        [JsonConstructor]
        public LeaderboardEntry(
            string playerName,
            Guid playerId,
            int totalPoints,
            uint wins,
            uint draws,
            uint losses,
            uint walkovers,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
            _playerId = playerId;
            _totalPoints = totalPoints;
            _wins = wins;
            _draws = draws;
            _losses = losses;
            _walkovers = walkovers;
        }

        [JsonInclude]
        public string PlayerName
        {
            get => _playerName;
            set
            {
                if (_playerName != value)
                {
                    _playerName = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public Guid PlayerId
        {
            get => _playerId;
            private set
            {
                if (_playerId != value)
                {
                    _playerId = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public int TotalPoints
        {
            get => _totalPoints;
            private set
            {
                if (_totalPoints != value)
                {
                    _totalPoints = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public uint Wins
        {
            get => _wins;
            private set
            {
                if (_wins != value)
                {
                    _wins = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public uint Draws
        {
            get => _draws;
            private set
            {
                if (_draws != value)
                {
                    _draws = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public uint Losses
        {
            get => _losses;
            private set
            {
                if (_losses != value)
                {
                    _losses = value;
                    OnChanged();
                }
            }
        }

        [JsonInclude]
        public uint Walkovers
        {
            get => _walkovers;
            private set
            {
                if (_walkovers != value)
                {
                    _walkovers = value;
                    OnChanged();
                }
            }
        }

        public int GamesPlayed => (int)Wins + (int)Draws + (int)Losses + (int)Walkovers;

        public void RegisterResult(MatchScore score)
        {
            switch (score)
            {
                case MatchScore.Win:
                    Wins++;
                    TotalPoints += 3;
                    break;
                case MatchScore.Draw:
                    Draws++;
                    TotalPoints += 1;
                    break;
                case MatchScore.Lose:
                    Losses++;
                    break;
                case MatchScore.Walkover:
                    Walkovers++;
                    TotalPoints -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(score), $"Invalid match result: {score}");
            }

            OnChanged();
        }
    }
}
