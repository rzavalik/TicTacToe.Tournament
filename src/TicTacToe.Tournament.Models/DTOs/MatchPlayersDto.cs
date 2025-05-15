namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchPlayersDto
    {
        public MatchPlayersDto()
        {

        }

        public MatchPlayersDto(Tournament tournament, Match match)
        {
            MatchId = match.Id;
            PlayerAId = match.PlayerA;
            PlayerAName = tournament.RegisteredPlayers[match.PlayerA];
            PlayerAMark = Mark.X;
            PlayerBId = match.PlayerB;
            PlayerBName = tournament.RegisteredPlayers[match.PlayerB];
            PlayerBMark = Mark.O;
            ETag = match.ETag;
        }

        public Guid MatchId { get; set; }
        public Guid PlayerAId { get; set; }
        public string PlayerAName { get; set; } = default!;
        public Mark PlayerAMark { get; set; }
        public Guid PlayerBId { get; set; }
        public string PlayerBName { get; set; } = default!;
        public Mark PlayerBMark { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}