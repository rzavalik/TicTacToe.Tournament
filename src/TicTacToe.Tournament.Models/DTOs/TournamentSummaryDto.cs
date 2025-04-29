namespace TicTacToe.Tournament.Models.DTOs
{
    public class TournamentSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Status { get; set; } = default!;
        public int RegisteredPlayersCount { get; set; }
        public int MatchCount { get; set; }
    }
}