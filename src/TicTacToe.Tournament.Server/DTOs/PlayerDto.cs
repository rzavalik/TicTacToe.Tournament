namespace TicTacToe.Tournament.Server.DTOs;

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int Score { get; set; }
}