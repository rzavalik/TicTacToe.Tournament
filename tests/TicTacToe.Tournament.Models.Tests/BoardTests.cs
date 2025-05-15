namespace TicTacToe.Tournament.Models.Tests
{
    using System;
    using Shouldly;
    using TicTacToe.Tournament.Models;
    using Xunit;

    public class BoardTests
    {
        private Board MakeSut() => new Board();

        [Fact]
        public void IsValidMove_ValidPosition_ShouldReturnTrue()
        {
            var sut = MakeSut();
            sut.IsValidMove(1, 1).ShouldBeTrue();
        }

        [Fact]
        public void IsValidMove_PositionAlreadyTaken_ShouldReturnFalse()
        {
            var sut = MakeSut();
            sut.ApplyMove(0, 0, Mark.X);
            sut.IsValidMove(0, 0).ShouldBeFalse();
        }

        [Fact]
        public void ApplyMove_ValidMove_ShouldUpdateStateAndMovements()
        {
            var sut = MakeSut();
            sut.ApplyMove(0, 1, Mark.O);

            sut.State[0][1].ShouldBe(Mark.O);
            sut.Movements.Count.ShouldBe(1);
            sut.Movements[0].Row.ShouldBe((byte)0);
            sut.Movements[0].Column.ShouldBe((byte)1);
            sut.Movements[0].Mark.ShouldBe(Mark.O);
        }

        [Fact]
        public void ApplyMove_InvalidMove_ShouldThrow()
        {
            var sut = MakeSut();
            sut.ApplyMove(0, 0, Mark.X);

            var ex = Should.Throw<InvalidOperationException>(() =>
            {
                sut.ApplyMove(0, 0, Mark.O);
            });

            ex.Message.ShouldBe("Invalid move");
        }

        [Fact]
        public void GetWinner_RowWin_ShouldReturnWinner()
        {
            var sut = MakeSut();
            sut.ApplyMove(1, 0, Mark.X);
            sut.ApplyMove(1, 1, Mark.X);
            sut.ApplyMove(1, 2, Mark.X);

            sut.GetWinner().ShouldBe(Mark.X);
        }

        [Fact]
        public void GetWinner_DiagonalWin_ShouldReturnWinner()
        {
            var sut = MakeSut();
            sut.ApplyMove(0, 0, Mark.O);
            sut.ApplyMove(1, 1, Mark.O);
            sut.ApplyMove(2, 2, Mark.O);

            sut.GetWinner().ShouldBe(Mark.O);
        }

        [Fact]
        public void IsGameOver_WinnerExists_ShouldReturnTrue()
        {
            var sut = MakeSut();
            sut.ApplyMove(0, 0, Mark.X);
            sut.ApplyMove(0, 1, Mark.X);
            sut.ApplyMove(0, 2, Mark.X);

            sut.IsGameOver().ShouldBeTrue();
        }

        [Fact]
        public void IsGameOver_BoardFullAndNoWinner_ShouldReturnTrue()
        {
            var sut = new Board(new[]
            {
                new[] { Mark.X, Mark.O, Mark.X },
                new[] { Mark.X, Mark.X, Mark.O },
                new[] { Mark.O, Mark.X, Mark.O }
            });

            sut.IsGameOver().ShouldBeTrue();
            sut.GetWinner().ShouldBeNull();
        }

        [Fact]
        public void GetState_ShouldReturnDeepCopy()
        {
            var sut = MakeSut();
            var state = sut.GetState();
            state[0][0] = Mark.X;

            sut.State[0][0].ShouldBe(Mark.Empty);
        }

        [Fact]
        public void Created_ShouldBeInitializedOnConstruction()
        {
            var before = DateTime.UtcNow;
            var sut = new Board();
            var after = DateTime.UtcNow;

            sut.Created.ShouldBeInRange(before, after);
        }

        [Fact]
        public void Modified_ShouldBeNullInitiallyAndUpdatedAfterChange()
        {
            var sut = new Board();

            var created = sut.Created;
            sut.Modified.ShouldBe(created);

            Thread.Sleep(10);
            sut.ApplyMove(1, 1, Mark.X);

            sut.Modified.Value.ShouldBeGreaterThan(created);
        }

        [Fact]
        public void ETag_ShouldReflectModifiedOrCreatedTicks()
        {
            var sut = new Board();
            var expectedTicks = sut.Created.ToUniversalTime().Ticks;
            sut.ETag.ShouldBe($"\"{expectedTicks}\"");

            Thread.Sleep(10);
            sut.ApplyMove(2, 2, Mark.O);
            var updatedTicks = sut.Modified.Value.ToUniversalTime().Ticks;
            sut.ETag.ShouldBe($"\"{updatedTicks}\"");
        }
    }
}
