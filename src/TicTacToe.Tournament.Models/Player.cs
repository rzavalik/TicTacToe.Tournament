namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class Player : BaseModel
    {
        private Guid _id;
        private string _name = string.Empty;
        private Guid _tournamentId;

        public Player() : base()
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

        public Guid TournamentId
        {
            get => _tournamentId;
            set
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
