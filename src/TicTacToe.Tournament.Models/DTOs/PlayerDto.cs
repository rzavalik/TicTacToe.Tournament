namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class PlayerDto
    {
        public PlayerDto()
        {

        }

        public PlayerDto(Tournament tournament, Match match, Player player)
        {
            Id = player.Id;
            Name = player.Name;
            Score = tournament.Leaderboard.FirstOrDefault(l => l.PlayerId == player.Id)?.TotalPoints ?? 0;
            ETag = player.ETag;

            if (match.PlayerA == player.Id || match.PlayerB == player.Id)
            {
                Mark = match.PlayerA == player.Id ? Mark.X : Mark.O;
            }
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public int Score { get; set; }
        public Mark Mark { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}