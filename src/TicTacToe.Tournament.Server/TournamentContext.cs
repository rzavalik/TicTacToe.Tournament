using Microsoft.AspNetCore.SignalR;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Hubs;

namespace TicTacToe.Tournament.Server;

public class TournamentContext
{
    private readonly IHubContext<TournamentHub> _hubContext;

    public Models.Tournament Tournament { get; set; } = default!;

    public IGameServer GameServer { get; set; } = default!;

    public Dictionary<Guid, Guid> PlayerTournamentMap { get; set; } = new();

    public SemaphoreSlim Lock { get; set; } = new(1, 1);

    public Action<TournamentContext> SaveState { get; set; }

    public TournamentContext(
        IHubContext<TournamentHub> hubContext,
        Models.Tournament tournament)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

        Tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));

        GameServer = new GameServer(
            tournament,
            _hubContext,
            async (playerId, matchScore) =>
            {
                tournament.AgreggateScoreToPlayer(
                    playerId,
                    matchScore);

                await _hubContext
                    .Clients
                    .Group(tournament.Id.ToString())
                    .SendAsync("OnRefreshLeaderboard", tournament.Leaderboard);
            },
            () =>
            {
                SaveState?.Invoke(this);
            },
            Server.GameServer.DefaultTimeout);

        if (Tournament.Leaderboard == null ||
            Tournament.Leaderboard.Count == 0)
        {
            GameServer.InitializeLeaderboard();
        }

        if (Tournament.Status == Models.TournamentStatus.Planned)
        {
            GameServer.GenerateMatches();
        }
    }

    ~TournamentContext()
    {
        try { SaveState?.Invoke(this); }
        catch { }

        try
        {
            Lock?.Release();
            Lock?.Dispose();
        }
        catch
        { }
    }
}