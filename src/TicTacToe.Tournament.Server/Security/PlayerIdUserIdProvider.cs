using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TicTacToe.Tournament.Server.Security;

public class PlayerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
    }
}