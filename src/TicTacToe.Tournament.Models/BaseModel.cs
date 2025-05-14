namespace TicTacToe.Tournament.Models
{
    using System.Text.Json.Serialization;

    [Serializable]
    public class BaseModel
    {
        private DateTime _created;
        private DateTime? _modified;

        public BaseModel()
        {
            OnCreated();
        }

        public BaseModel(DateTime created, DateTime? modified)
        {
            _created = created;
            _modified = modified;
        }

        [JsonInclude]
        public DateTime Created { get => _created; }

        [JsonInclude]
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
