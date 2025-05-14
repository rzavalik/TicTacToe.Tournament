namespace TicTacToe.Tournament.Models.Tests
{
    using Shouldly;

    public class PlayerTests
    {
        private static Player MakeSut()
        {
            return new Player(Guid.NewGuid(), "TestPlayer", Guid.NewGuid());
        }

        [Fact]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            var id = Guid.NewGuid();
            var name = "Alice";
            var tournamentId = Guid.NewGuid();

            var player = new Player(id, name, tournamentId);

            player.Id.ShouldBe(id);
            player.Name.ShouldBe(name);
            player.TournamentId.ShouldBe(tournamentId);
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
        public void ETag_ShouldMatchCreatedTicksInitially()
        {
            var sut = MakeSut();
            sut.ETag.ShouldBe($"\"{sut.Created.ToUniversalTime().Ticks}\"");
        }

        [Fact]
        public void Name_SetNewValue_ShouldUpdateAndModify()
        {
            var sut = MakeSut();
            var oldModified = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            sut.Name = "Bob";

            sut.Name.ShouldBe("Bob");
            sut.Modified.Value.ShouldBeGreaterThan(oldModified);
        }

        [Fact]
        public void Name_SetSameValue_ShouldNotModify()
        {
            var sut = new Player(Guid.NewGuid(), "SameName", Guid.NewGuid());
            var before = sut.Modified ?? sut.Created;

            sut.Name = "SameName";
            sut.Modified.Value.ShouldBe(before);
        }

        [Fact]
        public void TournamentId_SetPrivate_ShouldUpdateUsingReflection()
        {
            var sut = MakeSut();
            var newTournamentId = Guid.NewGuid();
            var before = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            sut.GetType().GetProperty(nameof(Player.TournamentId))!
                .SetValue(sut, newTournamentId);

            sut.TournamentId.ShouldBe(newTournamentId);
            sut.Modified.Value.ShouldBeGreaterThan(before);
        }
    }
}