namespace TicTacToe.Tournament.Models.Tests
{
    using Shouldly;
    public class MovementTests
    {
        [Fact]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            var movement = new Movement(1, 2, Mark.X);

            movement.Row.ShouldBe((byte)1);
            movement.Column.ShouldBe((byte)2);
            movement.Mark.ShouldBe(Mark.X);
        }

        [Fact]
        public void Created_ShouldBeSetOnInstantiation()
        {
            var before = DateTime.UtcNow;
            var sut = new Movement(1, 2, Mark.O);
            var after = DateTime.UtcNow;

            sut.Created.ShouldBeInRange(before, after);
        }

        [Fact]
        public void ETag_ShouldMatch()
        {
            var sut = new Movement(2, 0, Mark.X);
            sut.ETag.ShouldBe($"\"{(sut.Modified ?? sut.Created).ToUniversalTime().Ticks}\"");
        }

        [Theory]
        [InlineData(3)]
        [InlineData(255)]
        [InlineData(99)]
        public void Row_SetOutOfRange_ShouldThrow(byte invalidRow)
        {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
                new Movement(invalidRow, 1, Mark.X));

            ex.ParamName.ShouldBe("value");
            ex.Message.ShouldContain("Row must be between 0 and 2");
        }

        [Theory]
        [InlineData(3)]
        [InlineData(255)]
        [InlineData(42)]
        public void Column_SetOutOfRange_ShouldThrow(byte invalidCol)
        {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
                new Movement(1, invalidCol, Mark.X));

            ex.ParamName.ShouldBe("value");
            ex.Message.ShouldContain("Column must be between 0 and 2");
        }

        [Fact]
        public void Mark_SetInvalidValue_ShouldThrow()
        {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
                new Movement(1, 1, Mark.Empty));

            ex.ParamName.ShouldBe("value");
            ex.Message.ShouldContain("Mark must be either X or O");
        }
    }
}
