namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class Movement : BaseModel
    {
        private Mark _mark;
        private byte _row;
        private byte _col;

        public Movement() : base()
        {

        }

        public byte Column
        {
            get => _col;
            set
            {
                if (value >= 0 && value <= 2)
                {
                    _col = value;
                    OnChanged();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Column must be between 0 and 2.");
                }
            }
        }

        public byte Row
        {
            get => _row;
            set
            {
                if (value >= 0 && value <= 2)
                {
                    _row = value;
                    OnChanged();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Row must be between 0 and 2.");
                }
            }
        }

        public Mark Mark
        {
            get => _mark;
            set
            {
                if (value == Mark.X || value == Mark.O)
                {
                    _mark = value;
                    OnChanged();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Mark must be either X or O.");
                }
            }
        }
    }
}
