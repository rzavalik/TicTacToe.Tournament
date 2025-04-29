namespace TicTacToe.Tournament.Player.Tests.Helpers
{
    using Shouldly;
    using TicTacToe.Tournament.BasePlayer.Helpers;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;
    using Xunit;
    using System.IO;

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

            var expected =
                " X |   | O \r\n" +
                "---+---+---\r\n" +
                " O | X |   \r\n" +
                "---+---+---\r\n" +
                "   |   | X \r\n";

            output.ToString().ShouldBe(expected);
        }

        [Theory]
        [InlineData(0, 0, Mark.X)]
        [InlineData(0, 1, Mark.O)]
        [InlineData(0, 2, Mark.X)]
        [InlineData(1, 0, Mark.O)]
        [InlineData(1, 1, Mark.X)]
        [InlineData(1, 2, Mark.O)]
        [InlineData(2, 0, Mark.X)]
        [InlineData(2, 1, Mark.O)]
        [InlineData(2, 2, Mark.X)]
        public void Draw_ShouldRenderCorrectSymbolAtPosition(int row, int col, Mark expectedMark)
        {
            var output = new StringWriter();
            var sut = MakeSut(output);

            var board = Board.Empty;

            board[row][col] = expectedMark;

            sut.Draw(board);

            var rendered = output.ToString();
            var lines = rendered.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var lineIndex = row * 2; 

            lineIndex.ShouldBeLessThan(lines.Length);
            var line = lines[lineIndex];

            var columnPosition = col * 4 + 1;

            columnPosition.ShouldBeLessThan(line.Length);

            var expectedSymbol = expectedMark == Mark.X ? "X" : "O";

            line[columnPosition].ToString().ShouldBe(expectedSymbol);
        }
    }
}
