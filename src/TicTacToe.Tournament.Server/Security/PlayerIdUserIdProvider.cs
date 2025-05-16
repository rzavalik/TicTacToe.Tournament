namespace TicTacToe.Tournament.Server.Security
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.SignalR;

    public class PlayerIdUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        }
    }
}