using Shouldly;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server.Tests;

public class BoardTests
{
    private Board MakeSut()
    {
        return new Board();
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyGrid()
    {
        var sut = MakeSut();
        var state = sut.GetState();

        state.Length.ShouldBe(3);
        foreach (var row in state)
        {
            row.Length.ShouldBe(3);
            foreach (var cell in row)
            {
                cell.ShouldBe(Mark.Empty);
            }
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(1, 1)]
    public void IsValidMove_WhenCellIsEmpty_ShouldReturnTrue(int row, int col)
    {
        var sut = MakeSut();

        sut.IsValidMove(row, col).ShouldBeTrue();
    }

    [Fact]
    public void IsValidMove_WhenCellIsTaken_ShouldReturnFalse()
    {
        var sut = MakeSut();
        sut.ApplyMove(1, 1, Mark.X);

        sut.IsValidMove(1, 1).ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 1)]
    [InlineData(0, -1)]
    [InlineData(2, 3)]
    public void IsValidMove_WhenOutOfBounds_ShouldReturnFalse(int row, int col)
    {
        var sut = MakeSut();

        sut.IsValidMove(row, col).ShouldBeFalse();
    }

    [Fact]
    public void ApplyMove_WhenValidMove_ShouldPlaceMark()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 0, Mark.X);

        sut.GetState()[0][0].ShouldBe(Mark.X);
    }

    [Fact]
    public void ApplyMove_WhenInvalidMove_ShouldThrow()
    {
        var sut = MakeSut();
        sut.ApplyMove(1, 1, Mark.O);

        Should.Throw<InvalidOperationException>(() => sut.ApplyMove(1, 1, Mark.X));
    }

    [Fact]
    public void GetWinner_WhenRowHasSameMark_ShouldReturnWinner()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 0, Mark.X);
        sut.ApplyMove(0, 1, Mark.X);
        sut.ApplyMove(0, 2, Mark.X);

        sut.GetWinner().ShouldBe(Mark.X);
    }

    [Fact]
    public void GetWinner_WhenColumnHasSameMark_ShouldReturnWinner()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 1, Mark.O);
        sut.ApplyMove(1, 1, Mark.O);
        sut.ApplyMove(2, 1, Mark.O);

        sut.GetWinner().ShouldBe(Mark.O);
    }

    [Fact]
    public void GetWinner_WhenDiagonalHasSameMark_ShouldReturnWinner()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 0, Mark.X);
        sut.ApplyMove(1, 1, Mark.X);
        sut.ApplyMove(2, 2, Mark.X);

        sut.GetWinner().ShouldBe(Mark.X);
    }

    [Fact]
    public void GetWinner_WhenAntiDiagonalHasSameMark_ShouldReturnWinner()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 2, Mark.O);
        sut.ApplyMove(1, 1, Mark.O);
        sut.ApplyMove(2, 0, Mark.O);

        sut.GetWinner().ShouldBe(Mark.O);
    }

    [Fact]
    public void GetWinner_WhenNoWinner_ShouldReturnNull()
    {
        var sut = MakeSut();

        sut.GetWinner().ShouldBeNull();
    }

    [Fact]
    public void IsGameOver_WhenBoardIsFull_ShouldReturnTrue()
    {
        var sut = MakeSut();
        var marks = new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.X, Mark.X, Mark.O },
            new[] { Mark.O, Mark.X, Mark.O }
        };

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                sut.ApplyMove(i, j, marks[i][j]);

        sut.IsGameOver().ShouldBeTrue();
    }

    [Fact]
    public void IsGameOver_WhenWinnerExists_ShouldReturnTrue()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 0, Mark.X);
        sut.ApplyMove(0, 1, Mark.X);
        sut.ApplyMove(0, 2, Mark.X);

        sut.IsGameOver().ShouldBeTrue();
    }

    [Fact]
    public void IsGameOver_WhenBoardNotFullAndNoWinner_ShouldReturnFalse()
    {
        var sut = MakeSut();
        sut.ApplyMove(0, 0, Mark.X);

        sut.IsGameOver().ShouldBeFalse();
    }

    [Fact]
    public void GetState_ShouldReturnCopyNotReference()
    {
        var sut = MakeSut();
        var state = sut.GetState();
        state[0][0] = Mark.X;

        var newState = sut.GetState();
        newState[0][0].ShouldBe(Mark.Empty);
    }
}