using Microsoft.Extensions.Configuration;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Player.Tests;

namespace TicTacToe.Tournament.MyBotPlayer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.Title = $"MyBot Player";

        await BotTestRunner.Run<MyBotPlayer>(args);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .Build();

        var webAppEndpoint = config["Server:WebEndpoint"]!;
        var signalrEndpoint = config["Server:SignalREndpoint"]!;

        Console.WriteLine("Welcome to MyBot!");

        Console.Write("Enter your Player Name: ");
        var name = Console.ReadLine();

        Console.Title = $"MyBot Player: {name}";

        Console.Write("Enter the Tournament ID: ");
        var tournamentIdInput = Console.ReadLine();

        var tournamentId = Guid.TryParse(tournamentIdInput, out var parsedId)
            ? parsedId
            : throw new FormatException("Invalid TournamentId");

        var httpClient = (IHttpClient)new HttpClientWrapper();
        var signalrBuilder = new SignalRClientBuilder();

        var bot = new MyBotPlayer(
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