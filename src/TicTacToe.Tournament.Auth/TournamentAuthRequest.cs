namespace TicTacToe.Tournament.Auth
{
    public class TournamentAuthRequest
    {
        public Guid TournamentId { get; set; } = Guid.NewGuid()!;

        public string? PlayerName { get; set; }

        public string? AgentName { get; set; }

        public string? MachineName { get; set; }
    }
}