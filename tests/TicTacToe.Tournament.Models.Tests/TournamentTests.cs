namespace TicTacToe.Tournament.Models.Tests
{
    using Shouldly;

    public class TournamentTests
    {
        private static Guid _player1 = Guid.NewGuid();
        private static Guid _player2 = Guid.NewGuid();

        private static Tournament MakeSut(string name = "Tournament Test", uint repetition = 1)
        {
            return new Tournament(Guid.NewGuid(), name, repetition);
        }

        [Fact]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            var id = Guid.NewGuid();
            var name = "Championship";
            uint repetition = 5;

            var tournament = new Tournament(id, name, repetition);

            tournament.Id.ShouldBe(id);
            tournament.Name.ShouldBe(name);
            tournament.MatchRepetition.ShouldBe(repetition);
        }

        [Fact]
        public void MatchRepetition_ShouldBeLimitedToNine()
        {
            var sut = new Tournament(Guid.NewGuid(), "T", 20);
            sut.MatchRepetition.ShouldBe(9u);
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
        public void Modified_ShouldInitiallyEqualCreated()
        {
            var sut = MakeSut();
            sut.Modified.ShouldBe(sut.Created);
        }

        [Fact]
        public void ETag_ShouldReflectCreatedTicks()
        {
            var sut = MakeSut();
            sut.ETag.ShouldBe($"\"{sut.Created.ToUniversalTime().Ticks}\"");
        }

        [Fact]
        public void RegisterPlayer_NewPlayer_ShouldAddToRegisteredPlayersAndModify()
        {
            var sut = MakeSut();
            var before = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            sut.RegisterPlayer(_player1, "Player One");

            sut.RegisteredPlayers.ShouldContainKey(_player1);
            sut.Modified.Value.ShouldBeGreaterThan(before);
        }

        [Fact]
        public void RegisterPlayer_SamePlayerAndName_ShouldNotModify()
        {
            var sut = MakeSut();
            sut.RegisterPlayer(_player1, "Player One");

            var before = sut.Modified;
            sut.RegisterPlayer(_player1, "Player One");

            sut.Modified.ShouldBe(before); // no change
        }

        [Fact]
        public void InitializeLeaderboard_ShouldCreateEntriesForRegisteredPlayers()
        {
            var sut = MakeSut();
            sut.RegisterPlayer(_player1, "Alice");
            sut.RegisterPlayer(_player2, "Bob");

            sut.InitializeLeaderboard();

            sut.Leaderboard.Count.ShouldBe(2);
            sut.Leaderboard.Any(p => p.PlayerId == _player1).ShouldBeTrue();
            sut.Leaderboard.Any(p => p.PlayerId == _player2).ShouldBeTrue();
        }

        [Fact]
        public void AgreggateScoreToPlayer_ShouldUpdateLeaderboardEntry()
        {
            var sut = MakeSut();
            sut.RegisterPlayer(_player1, "Alice");
            sut.InitializeLeaderboard();

            var entry = sut.Leaderboard.First(p => p.PlayerId == _player1);
            var before = entry.TotalPoints;

            sut.AgreggateScoreToPlayer(_player1, MatchScore.Win);

            var updated = sut.Leaderboard.First(p => p.PlayerId == _player1);
            updated.TotalPoints.ShouldBe(before + 3);
        }

        [Fact]
        public void Duration_ShouldBeCalculatedFromStartAndEndTime()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            Thread.Sleep(10);
            sut.EndTime = DateTime.UtcNow;

            sut.Duration.ShouldNotBeNull();
            sut.Duration.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void Constructor_DefaultValues_ShouldInitializeCorrectly()
        {
            var sut = MakeSut();

            sut.Id.ShouldNotBe(Guid.Empty);
            sut.Name.ShouldBe("Tournament Test");
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
}