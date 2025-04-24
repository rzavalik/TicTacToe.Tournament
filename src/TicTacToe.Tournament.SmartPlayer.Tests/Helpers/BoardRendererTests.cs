using Shouldly;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Player.Tests.Helpers;

public class BoardRendererTests
{
    private IBoardRenderer MakeSut(TextWriter writer)
    {
        return (IBoardRenderer)new BoardRenderer(writer);
    }

    [Fact]
    public void Draw_ShouldRenderBoardCorrectly()
    {
        var output = new StringWriter();
        var sut = MakeSut(output);

        var board = new[]
        {
            new[] { Mark.X, Mark.Empty, Mark.O },
            new[] { Mark.O, Mark.X, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.X }
        };

        sut.Draw(board);

        var expected = "X |   | O\r\nO | X |  \r\n  |   | X\r\n";

        output.ToString().ShouldBe(expected);
    }
}