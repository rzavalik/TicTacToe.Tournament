namespace TicTacToe.Tournament.MyBotPlayer
{
    using Microsoft.Extensions.Configuration;
    using TicTacToe.Tournament.BasePlayer;
    using TicTacToe.Tournament.BasePlayer.Helpers;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Player.Tests;

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

            var name = BasePlayerClient.GetPlayerName(args);
            var tournamentId = BasePlayerClient.GetTournamentId(args);

            Console.Title = $"{name}: MyBot Player";

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
}