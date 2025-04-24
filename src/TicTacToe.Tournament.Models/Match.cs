namespace TicTacToe.Tournament.Models;

public class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlayerA { get; set; } = default!;
    public Guid PlayerB { get; set; } = default!;

    public MatchStatus Status { get; set; } = MatchStatus.Planned;

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public TimeSpan? Duration =>
        StartTime.HasValue && EndTime.HasValue
            ? EndTime - StartTime
            : null;

    public Mark[][] Board { get; set; } = new Mark[3][];

    public Guid CurrentTurn { get; set; } = default!;

    public Mark? WinnerMark { get; set; } = Mark.Empty;
}
