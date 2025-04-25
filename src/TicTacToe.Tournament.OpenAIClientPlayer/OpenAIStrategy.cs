using System.Text;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.OpenAIClientPlayer
{
    public class OpenAIStrategy : IPlayerStrategy
    {
        private readonly string _apiKey;
        private readonly Mark _playerMark;
        private readonly Mark _opponentMark;
        private Mark[][]? _currentBoard;

        public OpenAIStrategy(
            Mark playerMark,
            Mark opponentMark,
            string apiKey)
        {
            _playerMark = playerMark;
            _opponentMark = opponentMark;
            _apiKey = apiKey;
        }

        public (int row, int col) MakeMove(Mark[][] board)
        {
            _currentBoard = board;

            var playerPositions = new List<(int row, int col)>();
            var opponentPositions = new List<(int row, int col)>();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i][j] == _playerMark)
                    {
                        playerPositions.Add((i, j));
                    }
                    else if (board[i][j] != Mark.Empty)
                    {
                        opponentPositions.Add((i, j));
                    }
                }
            }

            int row = -1, col = -1;

            var prompt = GetPrompt(playerPositions, opponentPositions);
            var hasResponse = false;
            var retries = 0;

            do
            {
                try
                {
                    if (retries > 2)
                    {
                        var tupple = GetDumbMove();
                        row = tupple.row;
                        col = tupple.col;
                    }
                    else
                    {
                        var oRequest = OpenAiService.GetChatGptReplyAsync(_apiKey, prompt);
                        oRequest.Wait();
                        var output = oRequest.Result;
                        row = ExtractCoordinate(output, "ROW");
                        col = ExtractCoordinate(output, "COL");
                    }

                    hasResponse = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    hasResponse = false;
                    retries++;
                }
            }
            while (!hasResponse);

            return (row, col);
        }

        private (int row, int col) GetDumbMove()
        {
            if (_currentBoard == null)
            {
                throw new InvalidOperationException("Current board is not set.");
            }

            var _rng = new Random();
            var moves = new List<(int row, int col)>();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (_currentBoard[r][c] == Mark.Empty)
                        moves.Add((r, c));

            return (moves.OrderBy(r => _rng.NextDouble()).FirstOrDefault());
        }

        private string GetPrompt(
            List<(int row, int col)> playerPositions,
            List<(int row, int col)> opponentPositions)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"- You are playing using {_playerMark:G} and your ooponent is playing using {(_playerMark == Mark.O ? Mark.X : Mark.O):G}");
            if (playerPositions?.Any() ?? false)
            {
                stringBuilder.AppendLine(
                    $"- You have {_playerMark:G} on positions " +
                    string.Join(", ", playerPositions.Select(p => $"[{p.row},{p.col}]")));
            }
            if (opponentPositions?.Any() ?? false)
            {
                stringBuilder.AppendLine(
                    $"- Your opponent has {_opponentMark:G} on positions " +
                    string.Join(", ", opponentPositions.Select(p => $"[{p.row},{p.col}]")));
            }
            if (_currentBoard != null)
            {
                stringBuilder.AppendLine("- This is the current board: \r\n" +
                    ConvertBoardToString(_currentBoard));
            }
            return stringBuilder.ToString();
        }

        private static string ConvertBoardToString(Mark[][] board)
        {
            var textWriter = new StringWriter();
            var boardRenderer = new BoardRenderer(textWriter);
            boardRenderer.Draw(board);
            return textWriter.ToString();
        }

        private static int ExtractCoordinate(string input, string label)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, @$"{label}\s*=\s*(\d)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }
}
