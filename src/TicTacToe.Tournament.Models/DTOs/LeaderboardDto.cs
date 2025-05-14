namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class LeaderboardDto
    {
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
