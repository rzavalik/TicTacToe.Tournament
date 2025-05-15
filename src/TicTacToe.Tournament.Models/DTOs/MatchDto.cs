namespace TicTacToe.Tournament.Models.DTOs
{
    [Serializable]
    public class MatchDto
    {
        public MatchDto()
        {
            Id = Guid.NewGuid();
            Board = Models.Board.Empty;
            ETag = string.Empty;
        }

        public MatchDto(Tournament tournament, Match match)
        {
            Id = match.Id;
            PlayerAId = match.PlayerA;
            if (tournament.RegisteredPlayers.ContainsKey(match.PlayerA))
            {
                PlayerAName = tournament.RegisteredPlayers[match.PlayerA];
            }
            PlayerAMark = Mark.X;
            PlayerBId = match.PlayerB;
            if (tournament.RegisteredPlayers.ContainsKey(match.PlayerB))
            {
                PlayerBName = tournament.RegisteredPlayers[match.PlayerB];
            }
            PlayerBMark = Mark.O;
            Board = match.Board.State;
            Status = match.Status;
            EndTime = match.EndTime;
            StartTime = match.StartTime;
            Duration = match.Duration;
            Winner = match.WinnerMark;
            ETag = match.ETag;
        }

        public Guid Id { get; set; }
        public Guid PlayerAId { get; set; }
        public string PlayerAName { get; set; } = default!;
        public Mark PlayerAMark { get; set; }
        public Guid PlayerBId { get; set; }
        public string PlayerBName { get; set; } = default!;
        public Mark PlayerBMark { get; set; }
        public Mark[][] Board { get; set; } = Models.Board.Empty;
        public MatchStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public Mark? Winner { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}