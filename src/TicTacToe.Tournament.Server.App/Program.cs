using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Tournament.Server.Hubs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Security;
using TicTacToe.Tournament.Server.Services;

namespace TicTacToe.Tournament.Server.App;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["Azure:Storage:ConnectionString"];
            return new AzureStorageService(connectionString!);
        });
        builder.Services.AddSingleton<ITournamentManager, TournamentManager>();
        builder.Services.AddHostedService<TournamentHostedService>();
        builder.Services.AddSingleton<IUserIdProvider, PlayerIdUserIdProvider>();
        builder.Services.AddSignalR().AddAzureSignalR(options =>
        {
            options.ConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
        });
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        try
                        {
                            var host = new Uri(origin).Host;
                            return host == "localhost" ||
                                   host == "127.0.0.1" ||
                                   host.EndsWith(".azurecontainerapps.io", StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();
        app.UseCors();
        app.MapHub<TournamentHub>("/tournamentHub");

        app.Run();
    }
}