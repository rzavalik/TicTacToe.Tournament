namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchBoardDto
    {
        public Guid MatchId { get; set; }
        public Mark[][] Board { get; set; } = Models.Board.Empty;
        public Guid CurrentTurn { get; set; }
        public IList<Movement> Movements { get; set; } = new List<Movement>();
        public string ETag { get; set; } = string.Empty;
    }
}
