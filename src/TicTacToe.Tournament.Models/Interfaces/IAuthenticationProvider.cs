namespace TicTacToe.Tournament.Models.Interfaces
{
    public interface IAuthenticationProvider
    {
        Guid? PlayerId { get; set; }
        Guid? TournamentId { get; set; }
        string? LastMessage { get; set; }
        Task<string> GetTokenAsync(Guid tournamentId);
    }
}
