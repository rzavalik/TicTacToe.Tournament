namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class PlayerInfo : BaseModel
    {
        private Guid _playerId;
        private string _name = string.Empty;

        public PlayerInfo() : base()
        {
        }

        public PlayerInfo(Guid playerId, string name)
        {
            _playerId = playerId;
            _name = name;
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
    }
}
