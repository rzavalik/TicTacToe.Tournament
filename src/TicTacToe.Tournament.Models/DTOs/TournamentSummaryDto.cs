namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class TournamentSummaryDto
    {
        public TournamentSummaryDto()
        {

        }

        public TournamentSummaryDto(Tournament tournament)
        {
            Id = tournament.Id;
            Name = tournament.Name;
            Status = tournament.Status.ToString("G");
            RegisteredPlayersCount = tournament.RegisteredPlayers.Keys.Count();
            MatchCount = tournament.Matches.Count();
            ETag = tournament.ETag;
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Status { get; set; } = default!;
        public int RegisteredPlayersCount { get; set; }
        public int MatchCount { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}