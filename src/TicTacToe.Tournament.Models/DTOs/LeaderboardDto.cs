namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class LeaderboardDto
    {
        public LeaderboardDto()
        {
            PlayerName = string.Empty;
            PlayerId = Guid.Empty;
            TotalPoints = 0;
            Wins = 0;
            Draws = 0;
            Losses = 0;
            Walkovers = 0;
            ETag = string.Empty;
        }

        public LeaderboardDto(LeaderboardEntry entry)
        {
            PlayerName = entry.PlayerName;
            PlayerId = entry.PlayerId;
            TotalPoints = entry.TotalPoints;
            Wins = entry.Wins;
            Draws = entry.Draws;
            Losses = entry.Losses;
            Walkovers = entry.Walkovers;
            ETag = entry.ETag;
        }

        public string PlayerName { get; set; }
        public Guid PlayerId { get; set; }
        public int TotalPoints { get; set; }
        public uint Wins { get; set; }
        public uint Draws { get; set; }
        public uint Losses { get; set; }
        public uint Walkovers { get; set; }
        public string ETag { get; set; }
    }
}
