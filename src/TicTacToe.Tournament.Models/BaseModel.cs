namespace TicTacToe.Tournament.Models
{
    [Serializable]
    public class BaseModel
    {
        private DateTime _created;
        private DateTime? _modified;

        public BaseModel()
        {
            OnCreated();
        }

        public DateTime Created { get => _created; }

        public DateTime? Modified { get => _modified ?? _created; }

        public string ETag => $"\"{(Modified ?? Created).ToUniversalTime().Ticks}\"";

        protected void OnChanged()
        {
            _modified = DateTime.UtcNow;
        }

        private void OnCreated()
        {
            _created = DateTime.UtcNow;
        }
    }
}
