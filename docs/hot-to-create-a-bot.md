# How to Create a Bot

To participate in the TicTacToe Tournament, you must implement a custom bot by extending the `BasePlayerClient` class.  
This guide explains how to get started with the latest architecture and best practices.

---

## 🧱 Step-by-Step Instructions

### 1. Create a New Project

Create a new .NET 8 console project:

```bash
dotnet new console -n MyBot
```

### 2. Add a Reference to the Tournament BasePlayer

Reference the base project which contains the `BasePlayerClient` and `IPlayerStrategy` interfaces:

```bash
dotnet add reference ../TicTacToe.Tournament.BasePlayer/TicTacToe.Tournament.BasePlayer.csproj
```

---

### 3. Implement Your Strategy

Create a class that implements `IPlayerStrategy`. This is your bot’s decision engine:

```csharp
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

public class MyBotStrategy : IPlayerStrategy
{
    private readonly Mark _playerMark;
    private readonly Mark _opponentMark;
    private readonly Action<string> _consoleWrite;
    private readonly Func<string, int> _consoleRead;

    public MyBotStrategy(
        Mark playerMark,
        Mark opponentMark,
        Action<string> consoleWrite,
        Func<string, int> consoleRead)
    {
        _playerMark = playerMark;
        _opponentMark = opponentMark;
        _consoleWrite = consoleWrite;
        _consoleRead = consoleRead;
    }

    public (int row, int col) MakeMove(Mark[][] board)
    {
        // Add your custom strategy logic here
        return (0, 0);
    }
}
```

---

### 4. Implement Your Bot Client

Create a class that inherits from `BasePlayerClient`, wires up your strategy, and optionally overrides lifecycle events:

```csharp
using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Helpers;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

public class MyBotPlayer : BasePlayerClient
{
    private IPlayerStrategy? _strategy;

    public MyBotPlayer(string botName, Guid tournamentId, string webAppEndpoint, string signalrEndpoint, IHttpClient httpClient, ISignalRClientBuilder signalRBuilder)
        : base(botName, tournamentId, webAppEndpoint, signalrEndpoint, httpClient, signalRBuilder)
    {
    }

    public MyBotPlayer()
        : base(
            botName: "MyBot",
            tournamentId: Guid.NewGuid(),
            webAppEndpoint: "http://localhost",
            signalrEndpoint: "http://localhost",
            httpClient: new FakeHttpClient(),
            signalRBuilder: new FakeSignalRClientBuilder())
    { }

    protected override void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
    {
        base.OnMatchStarted(matchId, playerId, opponentId, mark, starts);

        _strategy = new MyBotStrategy(
            mark,
            mark == Mark.X ? Mark.O : Mark.X,
            ConsoleWrite,
            ConsoleRead<int>
        );
    }

    protected override Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board)
    {
        try
        {
            return Task.FromResult(_strategy?.MakeMove(board) ?? (-1, -1));
        }
        catch (Exception ex)
        {
            ConsoleWrite($"Error: {ex.Message}");
            return Task.FromResult((-1, -1));
        }
    }
}
```

---

### 5. Add Entry Point with Test Support

You can test locally using the built-in bot test runner:

```csharp
private static async Task Main(string[] args)
{
    await BotTestRunner.Run<MyBotPlayer>(args);
}
```

Then run:

```bash
dotnet run --project MyBot --test
```

---

### 6. Join a Live Tournament

Once ready, use the following CLI format to play against others:

```bash
dotnet run --project MyBot --name "MyBot" --tournament-id <TOURNAMENT_ID>
```

Replace values with:

- `TOURNAMENT_ID`: ID from the Web UI
- `WEBAPP_URL`: App endpoint (e.g. `https://tictactoe-webui...`)
- `SIGNALR_URL`: SignalR Hub URL (e.g. `https://tictactoe-signalr...`)

---

## ✅ Best Practices

- Always create a **parameterless constructor** for local tests.
- Use `ConsoleWrite` and `ConsoleRead<T>` for terminal I/O (not `Console.WriteLine`).
- Implement and isolate logic via `IPlayerStrategy`.
- Validate moves before sending (stay within bounds, avoid overwrites).
- Catch and log exceptions to avoid crashing during games.
- Keep bots **deterministic** for fairness (unless intentional).

---

## ❗ Notes

- Spectre.Console powers the console UI — do not use raw `Console.Read/Write`.
- Use dependency injection and interfaces for modular bots.
- Full guide: [GitHub - TicTacToe Tournament](https://github.com/rzavalik/TicTacToe.Tournament)

---

Happy coding and good luck in the tournament! 🧠🤖🎮
