namespace TicTacToe.Tournament.Models.Tests
{
    using Shouldly;
    using TicTacToe.Tournament.Models;
    public class GameResultTests
    {
        private GameResult MakeSut()
        {
            return new GameResult();
        }

        [Fact]
        public void Constructor_ShouldInitializeMatchIdAndBoard()
        {
            var sut = MakeSut();

            sut.MatchId.ShouldNotBe(Guid.Empty);
            sut.Board.ShouldNotBeNull();
        }

        [Fact]
        public void Created_ShouldBeInitializedOnConstruction()
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
        public void ETag_ShouldReflectInitialCreatedTicks()
        {
            var sut = MakeSut();
            var expectedTicks = sut.Created.ToUniversalTime().Ticks;
            sut.ETag.ShouldBe($"\"{expectedTicks}\"");
        }

        [Fact]
        public void WinnerId_SetValue_ShouldUpdateAndModify()
        {
            var sut = MakeSut();
            var originalModified = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            var winnerId = Guid.NewGuid();
            sut.WinnerId = winnerId;

            sut.WinnerId.ShouldBe(winnerId);
            sut.Modified.Value.ShouldBeGreaterThan<DateTime>(originalModified);
        }

        [Fact]
        public void IsDraw_SetTrue_ShouldUpdateAndModify()
        {
            var sut = MakeSut();
            var originalModified = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            sut.IsDraw = true;

            sut.IsDraw.ShouldBeTrue();
            sut.Modified.Value.ShouldBeGreaterThan<DateTime>(originalModified);
        }

        [Fact]
        public void Board_SetNewBoard_ShouldUpdateAndModify()
        {
            var sut = MakeSut();
            var originalModified = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            var newBoard = new Board();
            sut.GetType().GetProperty("Board")!.SetValue(sut, newBoard);

            sut.Board.ShouldBe(newBoard);
            sut.Modified.Value.ShouldBeGreaterThan<DateTime>(originalModified);
        }

        [Fact]
        public void MatchId_SetNew_ShouldUpdateAndModify()
        {
            var sut = MakeSut();
            var originalModified = sut.Modified ?? sut.Created;

            Thread.Sleep(10);
            var newMatchId = Guid.NewGuid();
            sut.GetType().GetProperty("MatchId")!.SetValue(sut, newMatchId);

            sut.MatchId.ShouldBe(newMatchId);
            sut.Modified.Value.ShouldBeGreaterThan<DateTime>(originalModified);
        }
    }
}
