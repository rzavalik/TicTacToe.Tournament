namespace TicTacToe.Tournament.Models.Tests
{
    using Shouldly;

    public class PlayerInfoTests
    {
        private static PlayerInfo MakeSut()
        {
            return new PlayerInfo(Guid.NewGuid(), "Original");
        }

        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            var id = Guid.NewGuid();
            var name = "Player Test";

            var sut = new PlayerInfo(id, name);

            sut.PlayerId.ShouldBe(id);
            sut.Name.ShouldBe(name);
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
            sut.Name = "Updated";

            sut.Name.ShouldBe("Updated");
            sut.Modified.Value.ShouldBeGreaterThan(oldModified);
        }

        [Fact]
        public void Name_SetSameValue_ShouldNotChangeModified()
        {
            var sut = new PlayerInfo(Guid.NewGuid(), "Fixed");
            var before = sut.Modified ?? sut.Created;

            sut.Name = "Fixed";
            sut.Modified.ShouldBe(before);
        }

        [Fact]
        public void PlayerId_SetPrivate_ShouldUpdateUsingReflection()
        {
            var sut = MakeSut();
            var newId = Guid.NewGuid();
            var before = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            typeof(PlayerInfo).GetProperty(nameof(PlayerInfo.PlayerId))!
                .SetValue(sut, newId);

            sut.PlayerId.ShouldBe(newId);
            sut.Modified.Value.ShouldBeGreaterThan(before);
        }
    }
}
