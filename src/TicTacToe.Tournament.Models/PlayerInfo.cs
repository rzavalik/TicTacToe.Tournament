namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class PlayerInfo : BaseModel
    {
        private Guid _playerId;
        private string _name = string.Empty;

        public PlayerInfo() : base()
        {
        }

        public PlayerInfo(
            Guid playerId,
            string name) : base()
        {
            _playerId = playerId;
            _name = name;
        }

        [JsonConstructor]
        public PlayerInfo(
            Guid playerId,
            string name,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _playerId = playerId;
            _name = name;
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
