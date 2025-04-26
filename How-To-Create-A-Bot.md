# How to Create a Bot

To participate in the TicTacToe Tournament, you must implement a custom bot by extending the `BasePlayerClient` class.  
This guide explains how to get started with the updated architecture.

---

## 🯡 Step-by-Step Instructions

### 1. Create a New Project
Create a new .NET class library or console project:
```bash
dotnet new console -n MyBot
```

### 2. Add a Reference to the Tournament BasePlayer
Reference the `BasePlayer` project from your bot project:
```bash
dotnet add reference ../TicTacToe.Tournament.BasePlayer/TicTacToe.Tournament.BasePlayer.csproj
```

### 3. Implement Your Strategy
Create a new class that implements `IPlayerStrategy`.  
This class will be responsible for your bot's decision-making logic:

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
        // Add your decision-making logic here
        return (0, 0);
    }
}
```

### 4. Implement Your Bot Client
Create a class that inherits from `BasePlayerClient` and connects your strategy:

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
            playerMark: mark,
            opponentMark: mark == Mark.X ? Mark.O : Mark.X,
            consoleWrite: base.ConsoleWrite,
            consoleRead: base.ConsoleRead<int>
        );
    }

    protected override Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board)
    {
        try
        {
            if (_strategy != null)
            {
                return Task.FromResult(_strategy.MakeMove(board));
            }
        }
        catch (Exception ex)
        {
            base.ConsoleWrite($"Error in MakeMove: {ex.Message}");
        }

        return Task.FromResult((-1, -1));
    }
}
```

### 5. Use the Bot Test Runner
You can quickly test your bot using the provided bot test runner:

```csharp
private static async Task Main(string[] args)
{
    await BotTestRunner.Run<MyBotPlayer>(args);
}
```
Then run:
```bash
dotnet run --project MyBot -- --test
```

This will simulate a test tournament to validate your bot's basic logic and behavior.

---

## ✅ Best Practices

- Always implement a **parameterless constructor** to allow test runners to instantiate your bot.
- Create a **separate `IPlayerStrategy`** class to isolate your bot's move logic.
- Always use:
  - `ConsoleWrite(message)` instead of `Console.WriteLine(message)`.
  - `ConsoleRead<T>(prompt)` instead of `Console.ReadLine()` or `Console.Read()`.
- Never use direct `Console.WriteLine` or `Console.ReadLine` — it will **break the live console layout** powered by Spectre.Console.
- Ensure your bot is **deterministic**, unless intentional randomness is part of your strategy.
- Validate board states and avoid illegal moves to prevent penalties or disqualifications.
- Properly handle exceptions during moves to ensure your bot does not crash the tournament.

---

## ❗ Important Notes

- **Avoid using `Console.Write` and `Console.Read` directly.**  
  Always use `BasePlayerClient.ConsoleWrite` and `BasePlayerClient.ConsoleRead<T>` to respect the tournament UI layout.
- **Isolate your move logic in the `IPlayerStrategy`** to facilitate **easier unit testing** and **modular development**.
- The game UI is powered by **Spectre.Console**, offering live updates for the board, tournament stats, and logs.

---

Happy bot building! 🤖🎾  
Good luck in the tournament!

---

