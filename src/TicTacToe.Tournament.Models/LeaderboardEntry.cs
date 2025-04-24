namespace TicTacToe.Tournament.Models;

[Serializable]
public class LeaderboardEntry
{
    public string PlayerName { get; set; } = string.Empty;
    
    public Guid PlayerId { get; set; } = Guid.Empty;

    public int TotalPoints { get; set; }

    public int GamesPlayed => Wins + Draws + Losses + Walkovers;

    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int Walkovers { get; set; }

    public void RegisterResult(MatchScore result)
    {
        switch (result)
        {
            case MatchScore.Win:
                Wins++;
                TotalPoints += 3;
                break;
            case MatchScore.Draw:
                Draws++;
                TotalPoints += 1;
                break;
            case MatchScore.Lose:
                Losses++;
                break;
            case MatchScore.Walkover:
                Walkovers++;
                break;
        }
    }
}
