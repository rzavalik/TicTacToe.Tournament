namespace TicTacToe.Tournament.Auth;

public class TournamentAuthResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = default!;

    public string Token { get; set; } = default!;

    public Guid PlayerId { get; set; }

    public Guid TournamentId { get; set; }
}