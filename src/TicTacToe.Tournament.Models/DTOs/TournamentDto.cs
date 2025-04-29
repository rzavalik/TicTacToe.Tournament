namespace TicTacToe.Tournament.Models.DTOs
{
    public class TournamentDto
    {
        public TournamentDto()
        {
            RegisteredPlayers = new Dictionary<Guid, string>();
            Leaderboard = new Dictionary<Guid, int>();
            Matches = [];
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Status { get; set; } = default!;
        public IDictionary<Guid, string> RegisteredPlayers { get; set; }
        public IDictionary<Guid, int> Leaderboard { get; set; }
        public IList<MatchDto> Matches { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}