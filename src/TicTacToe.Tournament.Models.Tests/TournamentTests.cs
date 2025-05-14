using Shouldly;

namespace TicTacToe.Tournament.Models.Tests;

public class TournamentTests
{
    private Tournament MakeSut()
    {
        return new Tournament
        {
            Name = "Spring Invitational"
        };
    }

    [Fact]
    public void Constructor_DefaultValues_ShouldInitializeCorrectly()
    {
        var sut = MakeSut();

        sut.Id.ShouldNotBe(Guid.Empty);
        sut.Name.ShouldBe("Spring Invitational");
        sut.Status.ShouldBe(TournamentStatus.Planned);
        sut.Matches.ShouldBeEmpty();
        sut.RegisteredPlayers.ShouldBeEmpty();
        sut.Leaderboard.ShouldBeEmpty();
        sut.Champion.ShouldBeNull();
        sut.StartTime.ShouldBeNull();
        sut.EndTime.ShouldBeNull();
        sut.Duration.ShouldBeNull();
    }

    [Fact]
    public void InitializeLeaderboard_ShouldPopulateLeaderboardFromRegisteredPlayers()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        var playerName = "Player1";

        sut.RegisteredPlayers.Add(playerId, playerName);

        sut.InitializeLeaderboard();

        sut.Leaderboard.ShouldContain(p => p.PlayerId == playerId);

        var leaderboardEntry = sut.Leaderboard.First(p => p.PlayerId == playerId);
        leaderboardEntry.TotalPoints.ShouldBe(0);
    }

    [Fact]
    public void RegisterPlayer_ShouldAddPlayerToRegisteredPlayers()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        var playerName = "TestPlayer";

        sut.RegisteredPlayers.Add(playerId, playerName);

        sut.RegisteredPlayers.ContainsKey(playerId).ShouldBeTrue();
        sut.RegisteredPlayers[playerId].ShouldBe(playerName);
    }

    [Fact]
    public void Champion_Assignment_ShouldSetCorrectly()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();

        sut.Champion = playerId;

        sut.Champion.ShouldBe(playerId);
    }

    [Fact]
    public void Duration_WhenStartAndEndSet_ShouldReturnTimeSpan()
    {
        var sut = MakeSut();
        sut.StartTime = DateTime.UtcNow.AddHours(-1);
        sut.EndTime = DateTime.UtcNow;

        sut.Duration.ShouldNotBeNull();
        ((int)sut.Duration.Value.TotalMinutes).ShouldBe(60);
    }

    [Fact]
    public void Duration_WhenStartOrEndNotSet_ShouldReturnNull()
    {
        var sut = MakeSut();
        sut.StartTime = DateTime.UtcNow;

        sut.Duration.ShouldBeNull();

        sut.StartTime = null;
        sut.EndTime = DateTime.UtcNow;

        sut.Duration.ShouldBeNull();
    }

    [Fact]
    public void UpdateLeaderboard_WhenPlayerNotInLeaderboard_ShouldAddWithScore()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        var playerName = "Player1";

        sut.RegisteredPlayers.Add(playerId, playerName);

        sut.InitializeLeaderboard();

        sut.AgreggateScoreToPlayer(playerId, MatchScore.Win);

        sut.Leaderboard.ShouldContain(p => p.PlayerId == playerId);

        var leaderboardEntry = sut.Leaderboard.First(p => p.PlayerId == playerId);
        leaderboardEntry.TotalPoints.ShouldBe((int)MatchScore.Win);
    }

    [Fact]
    public void UpdateLeaderboard_WhenPlayerAlreadyInLeaderboard_ShouldAddScore()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        var playerName = "Player1";

        sut.RegisteredPlayers.Add(playerId, playerName);

        sut.InitializeLeaderboard();

        sut.AgreggateScoreToPlayer(playerId, MatchScore.Draw);

        sut.Leaderboard.ShouldContain(p => p.PlayerId == playerId);

        var leaderboardEntry = sut.Leaderboard.First(p => p.PlayerId == playerId);
        leaderboardEntry.TotalPoints.ShouldBe((int)MatchScore.Draw);
    }
}
