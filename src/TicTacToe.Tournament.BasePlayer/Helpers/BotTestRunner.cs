using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Player.Tests;

public class BotTestRunner
{
    private readonly IBot _bot;

    public BotTestRunner(IBot bot)
    {
        _bot = bot;
        Console.Clear();
    }

    public async Task<bool> RunBasicMoveValidationAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("");
        Console.WriteLine("[TestMode] Simulating basic move...");

        var boardRenderer = new BoardRenderer(Console.Out);
        var hasFailed = false;
        var cases = BoardTestCases;

        for (var i = 0; i < cases.Count; i++)
        {
            var board = cases[i];

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("");
            Console.WriteLine($"[TestMode][Case #{i}]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            boardRenderer.Draw(board);
            Console.WriteLine("");
            Console.WriteLine("");

            var (row, col) = await _bot.MakeMoveAsync(board);

            if (row < 0 || row > 2 || col < 0 || col > 2)
            {
                hasFailed = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERR] Bot made an illegal move: outside board bounds ({row}, {col}).");
                continue;
            }

            if (board[row][col] != Mark.Empty)
            {
                hasFailed = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERR] Bot tried to move on a non-empty cell ({row}, {col} = {board[row][col]}).");
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Bot made a valid move: ({row}, {col})");
        }

        if (hasFailed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[TestMode][Fatal] Basic move validation failed.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[TestMode][Success] Basic move validation passed.");
        }
        Console.WriteLine("");

        Console.ForegroundColor = ConsoleColor.White;

        return !hasFailed;
    }

    public async Task<bool> RunSimulatedMatchAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("");
        Console.WriteLine("[TestMode] Simulating simple match...");

        Console.ForegroundColor = ConsoleColor.White;
        var boardRenderer = new BoardRenderer(Console.Out);
        var hasFailed = false;

        _bot.OnMatchStarted(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Mark.X, true);

        var board = EmptyBoard;

        Console.WriteLine("");
        boardRenderer.Draw(board);
        Console.WriteLine("");
        Console.WriteLine("");

        for (int turn = 0; turn < 9; turn++)
        {
            var (row, col) = await _bot.MakeMoveAsync(board);

            if (board[row][col] != Mark.Empty)
            {
                hasFailed = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERR] Invalid move at ({row},{col}). Cell already occupied. You've lost your turn.");
                continue; 
            }

            board[row][col] = turn % 2 == 0 ? Mark.X : Mark.O;

            _bot.OnOpponentMoved(row, col);
            _bot.OnBoardUpdated(board);

            Console.WriteLine("");
            boardRenderer.Draw(board);
            Console.WriteLine("");
            Console.WriteLine("");
        }

        Console.ForegroundColor = ConsoleColor.Green;

        _bot.OnMatchEnded(new GameResult { WinnerId = null });

        if (hasFailed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[TestMode][Fatal] Simulated match validation failed.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[TestMode][Success] Simulated match passed.");
        }
        Console.ForegroundColor = ConsoleColor.White;

        return !hasFailed;
    }

    private Mark[][] EmptyBoard =>
    [
        new[] { Mark.Empty, Mark.Empty, Mark.Empty },
        new[] { Mark.Empty, Mark.Empty, Mark.Empty },
        new[] { Mark.Empty, Mark.Empty, Mark.Empty }
    ];

    private List<Mark[][]> BoardTestCases =>
    [
        // 0 - Empty board
        EmptyBoard,

        // 1 - First move by X
        new[]
        {
            new[] { Mark.X, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 2 - X followed by O
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 3 - 3 moves
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 4
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 5
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 6
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.O },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 7
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.O },
            new[] { Mark.X, Mark.Empty, Mark.Empty }
        },

        // 8
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.O },
            new[] { Mark.X, Mark.X, Mark.Empty }
        },

        // 9 - Near win for X
        new[]
        {
            new[] { Mark.X, Mark.X, Mark.Empty },
            new[] { Mark.O, Mark.O, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 10 - Near win for O
        new[]
        {
            new[] { Mark.O, Mark.O, Mark.Empty },
            new[] { Mark.X, Mark.X, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 11
        new[]
        {
            new[] { Mark.X, Mark.Empty, Mark.O },
            new[] { Mark.Empty, Mark.X, Mark.Empty },
            new[] { Mark.O, Mark.Empty, Mark.X }
        },

        // 12 - Start in center
        new[]
        {
            new[] { Mark.Empty, Mark.Empty, Mark.Empty },
            new[] { Mark.Empty, Mark.X, Mark.Empty },
            new[] { Mark.Empty, Mark.Empty, Mark.Empty }
        },

        // 13
        new[]
        {
            new[] { Mark.Empty, Mark.O, Mark.Empty },
            new[] { Mark.X, Mark.X, Mark.Empty },
            new[] { Mark.O, Mark.Empty, Mark.Empty }
        },

        // 14
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.X, Mark.O, Mark.Empty },
            new[] { Mark.O, Mark.X, Mark.Empty }
        },

        // 15 - Opponent fork
        new[]
        {
            new[] { Mark.O, Mark.Empty, Mark.O },
            new[] { Mark.Empty, Mark.X, Mark.Empty },
            new[] { Mark.X, Mark.Empty, Mark.Empty }
        },

        // 16 - Block fork
        new[]
        {
            new[] { Mark.X, Mark.Empty, Mark.Empty },
            new[] { Mark.O, Mark.X, Mark.Empty },
            new[] { Mark.O, Mark.Empty, Mark.Empty }
        },

        // 17 - Mid game
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.X },
            new[] { Mark.O, Mark.Empty, Mark.Empty }
        },

        // 18 - Unpredictable layout
        new[]
        {
            new[] { Mark.X, Mark.Empty, Mark.O },
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.Empty }
        },

        // 19 - Almost full, still playable
        new[]
        {
            new[] { Mark.X, Mark.O, Mark.X },
            new[] { Mark.O, Mark.X, Mark.O },
            new[] { Mark.X, Mark.X, Mark.Empty }
        }
    ];

    public static async Task Run<T>(string[] args) where T : IBot, new()
    {
        if (args?.Any(arg => arg.Equals("--test", StringComparison.OrdinalIgnoreCase)) == true)
        {
            var myBot = new T();
            var runner = new BotTestRunner(myBot);

            await runner.RunBasicMoveValidationAsync();

            await runner.RunSimulatedMatchAsync();

            Environment.Exit(0);
        }
    }
}