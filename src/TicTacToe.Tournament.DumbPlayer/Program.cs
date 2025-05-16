namespace TicTacToe.Tournament.DumbPlayer
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
            Console.Title = "Dumb Player";

            await BotTestRunner.Run<DumbPlayerClient>(args);

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .Build();

            var webAppEndpoint = config["Server:WebEndpoint"]!;
            var signalrEndpoint = config["Server:SignalREndpoint"]!;

            Console.WriteLine("Welcome to DumbPlayer!");

            var name = BasePlayerClient.GetPlayerName(args);
            var tournamentId = BasePlayerClient.GetTournamentId(args);

            Console.Title = $"{name}: Dumb Player";

            var httpClient = (IHttpClient)new HttpClientWrapper();
            var signalrBuilder = new SignalRClientBuilder();

            var bot = new DumbPlayerClient(
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