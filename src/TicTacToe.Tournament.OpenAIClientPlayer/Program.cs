using Microsoft.Extensions.Configuration;
using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Player.Tests;

namespace TicTacToe.Tournament.OpenAIClientPlayer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.Title = $"OpenAI Player";

        await BotTestRunner.Run<OpenAIClientPlayer>(args);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .Build();

        var webAppEndpoint = config["Server:WebEndpoint"]!;
        var signalrEndpoint = config["Server:SignalREndpoint"]!;
        var apiKey = config["Bot:OpenAIAPIKey"]!;

        Console.WriteLine("Welcome to OpenAI Player!");

        var name = BasePlayerClient.GetPlayerName(args);
        var tournamentId = BasePlayerClient.GetTournamentId(args);

        Console.Title = $"{name}: OpenAI Player";

        var httpClient = (IHttpClient)new HttpClientWrapper();
        var signalrBuilder = new SignalRClientBuilder();

        var bot = new OpenAIClientPlayer(
            name!,
            tournamentId,
            webAppEndpoint,
            signalrEndpoint,
            httpClient,
            signalrBuilder,
            apiKey);

        await bot.AuthenticateAsync();
        await bot.StartAsync();
    }
}