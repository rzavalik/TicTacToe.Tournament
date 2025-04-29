namespace TicTacToe.Tournament.Models.DTOs
{
    public class MatchDto
    {
        public Guid Id { get; set; }
        public Guid PlayerAId { get; set; }
        public string PlayerAName { get; set; } = default!;
        public Guid PlayerBId { get; set; }
        public string PlayerBName { get; set; } = default!;
        public Mark[][] Board { get; set; } = new Mark[3][];
        public MatchStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Duration { get; set; }
        public string? Winner { get; set; }
    }
}