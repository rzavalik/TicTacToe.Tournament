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


    private LeaderboardEntry MakeSut()
    {
        return new LeaderboardEntry();
    }
}
