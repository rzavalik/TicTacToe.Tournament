namespace TicTacToe.Tournament.Models.DTOs;

public class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int Score { get; set; }
}