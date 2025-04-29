namespace TicTacToe.Tournament.Models.DTOs
{
    public class MatchPlayersDto
    {
        public Guid MatchId { get; set; }
        public Guid PlayerAId { get; set; }
        public string PlayerAName { get; set; } = default!;
        public Guid PlayerBId { get; set; }
        public string PlayerBName { get; set; } = default!;
    }
}