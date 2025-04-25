namespace TicTacToe.Tournament.Models
{
    public class Player
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public Guid TournamentId { get; set; }
    }
}
