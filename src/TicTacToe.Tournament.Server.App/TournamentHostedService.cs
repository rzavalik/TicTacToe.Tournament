
using Microsoft.Extensions.Hosting;
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

        var tournaments = await _storageService.ListTournamentsAsync();

        foreach (var tournamentId in tournaments)
        {
            try
            {
                await _tournamentManager.InitializeTournamentAsync(tournamentId);
                Console.WriteLine($"[TournamentHostedService] Tournament {tournamentId} loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TournamentHostedService] Failed to load tournament {tournamentId}: {ex.Message}");
            }
        }

        Console.WriteLine("[TournamentHostedService] All tournaments loaded.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
