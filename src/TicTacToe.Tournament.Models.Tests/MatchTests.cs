using Shouldly;

namespace TicTacToe.Tournament.Models.Tests
{
    public class MatchTests
    {
        [Fact]
        public void Board_ShouldBe3By3Matrix()
        {
            var sut = MakeSut();
            sut.Board.Length.ShouldBe(3);
            foreach (var row in sut.Board)
            {
                row.Length.ShouldBe(3);
            }
        }

        [Fact]
        public void Duration_WhenStartAndEndDefined_ReturnsCorrectTimespan()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            sut.EndTime = sut.StartTime.Value.AddMinutes(5);

            sut.Duration.ShouldBe(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Duration_WhenStartOrEndMissing_ReturnsNull()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            sut.EndTime = null;
            sut.Duration.ShouldBeNull();

            sut.StartTime = null;
            sut.EndTime = DateTime.UtcNow;
            sut.Duration.ShouldBeNull();
        }

        private Match MakeSut()
        {
            return new Match
            {
                Board = new[]
                {
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty },
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty },
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty }
                }
            };
        }
    }
}
