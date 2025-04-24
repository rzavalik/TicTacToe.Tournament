using Shouldly;

namespace TicTacToe.Tournament.Models.Tests;

public class LeaderboardEntryTests
{
    [Fact]
    public void RegisterResult_WhenWin_UpdatesWinsAndPoints()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Win);

        sut.Wins.ShouldBe(1);
        sut.TotalPoints.ShouldBe(3);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenDraw_UpdatesDrawsAndPoints()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Draw);

        sut.Draws.ShouldBe(1);
        sut.TotalPoints.ShouldBe(1);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenLose_UpdatesLossesOnly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Lose);

        sut.Losses.ShouldBe(1);
        sut.TotalPoints.ShouldBe(0);
        sut.GamesPlayed.ShouldBe(1);
    }

    [Fact]
    public void RegisterResult_WhenWalkover_UpdatesWalkoversOnly()
    {
        var sut = MakeSut();

        sut.RegisterResult(MatchScore.Walkover);

        sut.Walkovers.ShouldBe(1);
        sut.TotalPoints.ShouldBe(0);
        sut.GamesPlayed.ShouldBe(1);
    }

    private LeaderboardEntry MakeSut()
    {
        return new LeaderboardEntry();
    }
}
