namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class Player : BaseModel
    {
        private Guid _id;
        private string _name = string.Empty;
        private Guid _tournamentId;

        public Player() : base()
        {
        }

        public Player(
            Guid id,
            string name,
            Guid tournamentId) : base()
        {
            _id = id;
            _name = name;
            _tournamentId = tournamentId;
        }

        [JsonConstructor]
        public Player(
            Guid id,
            string name,
            Guid tournamentId,
            DateTime created,
            DateTime? modified) : base(created, modified)
        {
            _id = id;
            _name = name;
            _tournamentId = tournamentId;
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
        public Guid TournamentId
        {
            get => _tournamentId;
            private set
            {
                if (_tournamentId != value)
                {
                    _tournamentId = value;
                    OnChanged();
                }
            }
        }
    }
}
