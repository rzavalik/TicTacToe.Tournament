# How to Create a Bot

To participate in the TicTacToe Tournament, you must implement a custom bot by extending the `BasePlayerClient` class. This guide explains how to get started.

---

## 🧱 Step-by-Step Instructions

### 1. Create a New Project
Create a new .NET class library or console project:
```bash
dotnet new classlib -n MyBot
```

### 2. Add Reference to Tournament BasePlayer
Reference the BasePlayer project from your bot project:
```bash
dotnet add reference ../TicTacToe.Tournament.BasePlayer/TicTacToe.Tournament.BasePlayer.csproj
```

### 3. Implement Your Bot Class
Create a new class that inherits from `BasePlayerClient`:
```csharp
public class MyBotClient : BasePlayerClient
{
    public MyBotClient(string botName, Guid tournamentId, string webAppEndpoint, string signalrEndpoint, IHttpClient httpClient, ISignalRClientBuilder signalRBuilder)
        : base(botName, tournamentId, webAppEndpoint, signalrEndpoint, httpClient, signalRBuilder)
    {
    }

    public MyBotClient()
        : base(
            botName: "MyBot",
            tournamentId: Guid.NewGuid(),
            webAppEndpoint: "http://localhost",
            signalrEndpoint: "http://localhost",
            httpClient: new FakeHttpClient(),
            signalRBuilder: new FakeSignalRClientBuilder())
    { }

    protected override Task<(int row, int col)> MakeMoveAsync(Mark[][] board)
    {
        // Add your strategy here
        return Task.FromResult((0, 0));
    }
}
```

### 4. Use the Bot Test App
You can test your bot with the provided bot test runner:
```csharp
private static async Task Main(string[] args)
{
    await BotTestRunner.Run<MyBotClient>(args);
}
```
Then run:
```bash
dotnet run --project MyBot -- --test
```

This will execute a test routine using your bot's logic in an isolated environment.

---

## ✅ Best Practices
- Always implement a parameterless constructor for testing purposes.
- Use fake clients like `FakeHttpClient` and `FakeSignalRClientBuilder` for testing.
- Ensure your bot is deterministic and does not rely on randomness unless needed.
- Implement validation for board states and avoid illegal moves.

---

Happy bot coding! 🤖🎮
