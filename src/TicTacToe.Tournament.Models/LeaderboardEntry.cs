namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class LeaderboardEntry : BaseModel
    {
        private string _playerName = string.Empty;
        private Guid _playerId = Guid.Empty;
        private int _totalPoints;
        private int _wins;
        private int _draws;
        private int _losses;
        private int _walkovers;

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

        public Guid PlayerId
        {
            get => _playerId;
            set
            {
                if (_playerId != value)
                {
                    _playerId = value;
                    OnChanged();
                }
            }
        }

        public int TotalPoints
        {
            get => _totalPoints;
            set
            {
                if (_totalPoints != value)
                {
                    _totalPoints = value;
                    OnChanged();
                }
            }
        }

        public int Wins
        {
            get => _wins;
            set
            {
                if (_wins != value)
                {
                    _wins = value;
                    OnChanged();
                }
            }
        }

        public int Draws
        {
            get => _draws;
            set
            {
                if (_draws != value)
                {
                    _draws = value;
                    OnChanged();
                }
            }
        }

        public int Losses
        {
            get => _losses;
            set
            {
                if (_losses != value)
                {
                    _losses = value;
                    OnChanged();
                }
            }
        }

        public int Walkovers
        {
            get => _walkovers;
            set
            {
                if (_walkovers != value)
                {
                    _walkovers = value;
                    OnChanged();
                }
            }
        }

        public int GamesPlayed => Wins + Draws + Losses + Walkovers;

        public void RegisterResult(MatchScore result)
        {
            switch (result)
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), $"Invalid match result: {result}");
            }

            OnChanged();
        }
    }
}
