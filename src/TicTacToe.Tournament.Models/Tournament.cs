namespace TicTacToe.Tournament.Models;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = default!;

    public List<Match> Matches { get; set; } = new();

    public TournamentStatus Status { get; set; } = TournamentStatus.Planned;

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public TimeSpan? Duration =>
        StartTime.HasValue && EndTime.HasValue
            ? EndTime - StartTime
            : null;

    public uint MatchRepetition { get; set; } = 1;

    public IDictionary<Guid, string> RegisteredPlayers { get; set; } = new Dictionary<Guid, string>();

    public IDictionary<Guid, int> Leaderboard { get; set; } = new Dictionary<Guid, int>();

    public Guid? Champion { get; set; }

    public void InitializeLeaderboard()
    {
        Leaderboard = RegisteredPlayers.ToDictionary
        (
            k => k.Key,
            v => 0
        );
    }

    public void AgreggateScoreToPlayer(Guid playerId, MatchScore score)
    {
        if (Leaderboard.ContainsKey(playerId))
        {
            Leaderboard[playerId] += (int)score;
        }
        else
        {
            Leaderboard[playerId] = (int)score;
        }
    }
}