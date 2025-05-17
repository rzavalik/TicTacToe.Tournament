namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class TournamentDto
    {
        public TournamentDto()
        {
            RegisteredPlayers = new Dictionary<Guid, string>();
            Leaderboard = new List<LeaderboardDto>();
            Matches = [];
        }

        public TournamentDto(Tournament tournament)
        {
            Id = tournament.Id;
            Name = tournament.Name;
            Status = tournament.Status.ToString("G");
            RegisteredPlayers = tournament
                .RegisteredPlayers
                .OrderBy(pair => pair.Value)
                .ToDictionary();
            Leaderboard = tournament
                .Leaderboard
                .OrderByDescending(l => l.TotalPoints)
                .ThenByDescending(l => l.Wins)
                .ThenByDescending(l => l.Draws)
                .ThenByDescending(l => l.Losses)
                .ThenByDescending(l => l.Walkovers)
                .ThenByDescending(l => l.PlayerName)
                .Select(l => new LeaderboardDto(l))
                .ToList();
            Matches = tournament
                .Matches
                .Select(m => new MatchDto(tournament, m))
                .ToList();
            StartTime = tournament.StartTime;
            EndTime = tournament.EndTime;
            Duration = tournament.Duration;
            ETag = tournament.ETag;
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Status { get; set; } = default!;
        public IDictionary<Guid, string> RegisteredPlayers { get; set; }
        public IList<LeaderboardDto> Leaderboard { get; set; }
        public IList<MatchDto> Matches { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}