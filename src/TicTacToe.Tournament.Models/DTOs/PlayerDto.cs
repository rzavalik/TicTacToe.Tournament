namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class PlayerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public int Score { get; set; }
        public Mark Mark { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}