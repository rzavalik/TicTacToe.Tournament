using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Services;

internal class Program
{
    public static string? HubUrl { get; set; }

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddControllersWithViews();
        builder.Services.AddSingleton<ITournamentOrchestratorService, TournamentOrchestratorService>();
        builder.Configuration
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var app = builder.Build();

        HubUrl = builder.Configuration["Azure:SignalR:HubUrl"] ?? "";

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseDefaultFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}