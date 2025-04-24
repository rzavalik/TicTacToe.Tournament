using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server.DTOs;

public class MatchBoardDto
{
    public Guid MatchId { get; set; }
    public Mark[][] Board { get; set; } = new Mark[3][];
    public Guid CurrentTurn { get; set; }
}
