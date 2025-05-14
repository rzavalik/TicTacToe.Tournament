namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class Movement : BaseModel
    {
        private Mark _mark;
        private byte _row;
        private byte _col;

        public Movement() : base()
        {

        }

        [JsonConstructor]
        public Movement(byte row, byte col, Mark mark) : this()
        {
            Row = row;
            Column = col;
            Mark = mark;
        }

        [JsonInclude]
        public byte Column
        {
            get => _col;
            private set
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

        [JsonInclude]
        public byte Row
        {
            get => _row;
            private set
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

        [JsonInclude]
        public Mark Mark
        {
            get => _mark;
            private set
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
