using Shouldly;

namespace TicTacToe.Tournament.Models.Tests;

public class LeaderboardEntryTests
{
    [Fact]
    public void RegisterResult_WhenWin_UpdatesWinsAndPoints()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Win);

        sut.Wins.ShouldBe((uint)1);
        sut.TotalPoints.ShouldBe(3);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenDraw_UpdatesDrawsAndPoints()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Draw);

        sut.Draws.ShouldBe((uint)1);
        sut.TotalPoints.ShouldBe(1);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenLose_UpdatesLossesOnly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Lose);

        sut.Losses.ShouldBe((uint)1);
        sut.TotalPoints.ShouldBe(0);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenWalkover_UpdatesWalkoversOnly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Walkover);

        sut.Walkovers.ShouldBe((uint)1);
        sut.TotalPoints.ShouldBe(-1);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_MultipleWins_AccumulatesCorrectly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Win);
        sut.RegisterResult(MatchScore.Win);
        sut.RegisterResult(MatchScore.Win);

        sut.Wins.ShouldBe((uint)3);
        sut.TotalPoints.ShouldBe(9);
        sut.GamesPlayed.ShouldBe(3);
    }

    [Fact]
    public void RegisterResult_MixedResults_AccumulatesCorrectly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Win);         // 3 points
        sut.RegisterResult(MatchScore.Draw);        // 1 point
        sut.RegisterResult(MatchScore.Lose);        // 0 points
        sut.RegisterResult(MatchScore.Walkover);    // -1 points

        sut.Wins.ShouldBe((uint)1);
        sut.Draws.ShouldBe((uint)1);
        sut.Losses.ShouldBe((uint)1);
        sut.Walkovers.ShouldBe((uint)1);
        sut.TotalPoints.ShouldBe(3);
        sut.GamesPlayed.ShouldBe(4);
    }

    [Fact]
    public void RegisterResult_InvalidMatchScore_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = MakeSut();

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            sut.RegisterResult((MatchScore)999);
        });
    }

    [Fact]
    public void Constructor_ShouldSetPlayerNameAndId()
    {
        var playerId = Guid.NewGuid();
        var sut = new LeaderboardEntry("John", playerId);

        sut.PlayerName.ShouldBe("John");
        sut.PlayerId.ShouldBe(playerId);
    }

    [Fact]
    public void Created_ShouldBeSetOnInstantiation()
    {
        var before = DateTime.UtcNow;
        var sut = MakeSut();
        var after = DateTime.UtcNow;

        sut.Created.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Modified_ShouldInitiallyMatchCreated()
    {
        var sut = MakeSut();
        sut.Modified.ShouldBe(sut.Created);
    }

    [Fact]
    public void ETag_ShouldMatchCreatedTicksInitially()
    {
        var sut = MakeSut();
        sut.ETag.ShouldBe($"\"{sut.Created.ToUniversalTime().Ticks}\"");
    }

    [Theory]
    [InlineData(MatchScore.Win, 1, 0, 0, 0, 3)]
    [InlineData(MatchScore.Draw, 0, 1, 0, 0, 1)]
    [InlineData(MatchScore.Lose, 0, 0, 1, 0, 0)]
    [InlineData(MatchScore.Walkover, 0, 0, 0, 1, -1)]
    public void RegisterResult_ShouldUpdateCorrectly(MatchScore score, uint wins, uint draws, uint losses, uint walkovers, int points)
    {
        var sut = MakeSut();
        var originalModified = sut.Modified ?? sut.Created;

        Thread.Sleep(10);
        sut.RegisterResult(score);

        sut.Wins.ShouldBe(wins);
        sut.Draws.ShouldBe(draws);
        sut.Losses.ShouldBe(losses);
        sut.Walkovers.ShouldBe(walkovers);
        sut.TotalPoints.ShouldBe(points);
        sut.Modified.Value.ShouldBeGreaterThan(originalModified);
    }

    [Fact]
    public void GamesPlayed_ShouldSumAllResults()
    {
        var sut = MakeSut();
        sut.RegisterResult(MatchScore.Win);
        sut.RegisterResult(MatchScore.Draw);
        sut.RegisterResult(MatchScore.Lose);
        sut.RegisterResult(MatchScore.Walkover);

        sut.GamesPlayed.ShouldBe(4);
    }

    [Fact]
    public void PlayerName_Set_ShouldUpdateValueAndModify()
    {
        var sut = MakeSut();
        var before = sut.Modified ?? sut.Created;

        Thread.Sleep(10);
        sut.PlayerName = "Another";

        sut.PlayerName.ShouldBe("Another");
        sut.Modified.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public void SettingSamePlayerName_ShouldNotTriggerModification()
    {
        var sut = new LeaderboardEntry("Static", Guid.NewGuid());
        var before = sut.Modified;

        sut.PlayerName = "Static";
        sut.Modified.ShouldBe(before);
    }


    private LeaderboardEntry MakeSut()
    {
        return new LeaderboardEntry();
    }
}
