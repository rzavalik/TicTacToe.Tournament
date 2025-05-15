namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchBoardDto
    {
        public MatchBoardDto()
        {
            MatchId = Guid.NewGuid();
            Board = Models.Board.Empty;
            CurrentTurn = Guid.Empty;
            Movements = new List<Movement>();
            ETag = string.Empty;
        }

        public MatchBoardDto(Match match)
        {
            MatchId = match.Id;
            Board = match.Board.State;
            CurrentTurn = match.CurrentTurn;
            Movements = match.Board.Movements;
            ETag = match.Board.ETag;
        }

        public Guid MatchId { get; set; }
        public Mark[][] Board { get; set; } = Models.Board.Empty;
        public Guid CurrentTurn { get; set; }
        public IList<Movement> Movements { get; set; } = new List<Movement>();
        public string ETag { get; set; } = string.Empty;
    }
}
