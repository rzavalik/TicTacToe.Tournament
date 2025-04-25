namespace TicTacToe.Tournament.Models;

public class GameResult
{
    public Guid MatchId { get; set; }
    public Guid? WinnerId { get; set; }
    public Mark[][] Board { get; set; } = new Mark[3][];
    public bool IsDraw { get; set; }
}