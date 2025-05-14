namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchBoardDto
    {
        public Guid MatchId { get; set; }
        public Mark[][] Board { get; set; } = new Mark[3][];
        public Guid CurrentTurn { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}
