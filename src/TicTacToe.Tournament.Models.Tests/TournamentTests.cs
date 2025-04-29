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

        sut.Leaderboard.ShouldContainKey(playerId);
        sut.Leaderboard[playerId].ShouldBe(0);
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
        sut.StartTime = new DateTime(2025, 4, 24, 10, 0, 0);
        sut.EndTime = new DateTime(2025, 4, 24, 11, 0, 0);

        sut.Duration.ShouldNotBeNull();
        sut.Duration.Value.TotalMinutes.ShouldBe(60);
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

        sut.AgreggateScoreToPlayer(playerId, MatchScore.Win);

        sut.Leaderboard.ContainsKey(playerId).ShouldBeTrue();
        sut.Leaderboard[playerId].ShouldBe((int)MatchScore.Win);
    }

    [Fact]
    public void UpdateLeaderboard_WhenPlayerAlreadyInLeaderboard_ShouldAddScore()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        sut.Leaderboard[playerId] = 5;

        sut.AgreggateScoreToPlayer(playerId, MatchScore.Draw);

        sut.Leaderboard[playerId].ShouldBe(5 + (int)MatchScore.Draw);
    }
}
