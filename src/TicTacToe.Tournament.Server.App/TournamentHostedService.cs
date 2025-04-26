
using Microsoft.Extensions.Hosting;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Server.Interfaces;

namespace TicTacToe.Tournament.Server.App;
public class TournamentHostedService : BackgroundService
{
    private readonly ITournamentManager _tournamentManager;
    private readonly IAzureStorageService _storageService;

    public TournamentHostedService(
        ITournamentManager tournamentManager,
        IAzureStorageService storageService)
    {
        _tournamentManager = tournamentManager;
        _storageService = storageService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[TournamentHostedService] Starting preload of tournaments...");

        await _tournamentManager.LoadFromDataSourceAsync();
        
        Console.WriteLine("[TournamentHostedService] All tournaments loaded.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
