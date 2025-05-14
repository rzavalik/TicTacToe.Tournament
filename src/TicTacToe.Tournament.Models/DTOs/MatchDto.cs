namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchDto
    {
        public Guid Id { get; set; }
        public Guid PlayerAId { get; set; }
        public string PlayerAName { get; set; } = default!;
        public Mark PlayerAMark { get; set; }
        public Guid PlayerBId { get; set; }
        public string PlayerBName { get; set; } = default!;
        public Mark PlayerBMark { get; set; }
        public Mark[][] Board { get; set; } = Models.Board.Empty;
        public MatchStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Duration { get; set; }
        public string? Winner { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}