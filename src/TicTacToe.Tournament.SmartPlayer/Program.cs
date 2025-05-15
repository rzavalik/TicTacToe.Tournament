using Microsoft.Extensions.Configuration;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Player.Tests;

namespace TicTacToe.Tournament.SmartPlayer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.Title = $"Smart Player";

        await BotTestRunner.Run<SmartPlayerClient>(args);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .Build();

        var webAppEndpoint = config["Server:WebEndpoint"]!;
        var signalrEndpoint = config["Server:SignalREndpoint"]!;

        Console.WriteLine("Welcome to SmartPlayer!");

        Console.Write("Enter your Player Name: ");
        var name = Console.ReadLine();

        Console.Title = $"OpenAI Player: {name}";

        Console.Write("Enter the Tournament ID: ");
        var tournamentIdInput = Console.ReadLine();

        var tournamentId = Guid.TryParse(tournamentIdInput, out var parsedId)
            ? parsedId
            : throw new FormatException("Invalid TournamentId");

        var httpClient = (IHttpClient)new HttpClientWrapper();
        var signalrBuilder = new SignalRClientBuilder();

        Console.Clear();

        var bot = new SmartPlayerClient(
            name!,
            tournamentId,
            webAppEndpoint,
            signalrEndpoint,
            httpClient,
            signalrBuilder);

        await bot.AuthenticateAsync();
        await bot.StartAsync();
    }
}
