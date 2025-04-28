# TicTacToe Tournament - OpenAIClientPlayer Bot

This document explains how the **OpenAIClientPlayer** works inside the TicTacToe Tournament project.

---

# 🤖 Overview

The **OpenAIClientPlayer** is a special type of player bot that uses **OpenAI's Chat API** (ChatGPT models like gpt-3.5-turbo or gpt-4) to decide its moves during matches.

It connects to the Tournament ServerApp via **SignalR**, just like other bots (SmartPlayer, DumbPlayer).

The OpenAIClientPlayer reads the current board state, constructs a natural language prompt, sends it to OpenAI's API, and uses the model's response to choose its next move.

---

# ⚡ How it Works

1. **Connect to ServerApp** using SignalR and register as a player.
2. **When it's its turn**:
    - It generates a **prompt** including the board status.
    - Sends the prompt as a **Chat Message** to the **OpenAI Chat API**.
    - Receives a **response** suggesting a move (in the format: `row,column`).
3. **Parse** the response to extract the intended move.
4. **Retry** if parsing fails (up to 3 attempts).
5. **Submit the move** back to the ServerApp via SignalR.


---

# 📉 Example of a Prompt

```text
You are playing Tic Tac Toe. Here is the board:

Row 0: [X, , ]
Row 1: [O, X, ]
Row 2: [ , , O]

Please respond ONLY with your next move as two numbers separated by a comma: row,column
Example: 0,2

Your move:
```

Expected OpenAI response:
```text
2,0
```


---

# 🚀 Requirements

- **OpenAI API Key** (mandatory).
- **Configuration File (`appsettings.json`)** must include:
  - `Server:WebEndpoint` - URL of the WebApp.
  - `Server:SignalREndpoint` - URL of the SignalR Hub.
  - `Bot:OpenAIAPIKey` - Your OpenAI API key.


---

# 🔧 Configuration

You must configure your bot by providing an `appsettings.json` file with the following structure:

```json
{
  "Server": {
    "WebEndpoint": "https://your-webapp-endpoint",
    "SignalREndpoint": "https://your-signalr-endpoint"
  },
  "Bot": {
    "OpenAIAPIKey": "your-openai-api-key"
  }
}
```

Place the `appsettings.json` file alongside your executable or configure your environment to point to it.


---

# 🛠️ Technical Details

| Topic | Details |
|:---|:---|
| API Used | Chat Completion API (gpt-3.5-turbo, gpt-4) |
| Communication | OpenAIService handles HTTP calls and prompt crafting |
| Retry Logic | Retries up to 3 times if parsing fails |
| Move Parsing | Splits OpenAI response by comma and parses as integers |
| Error Handling | Timeout support and basic retry mechanism |


---

# 🚀 Future Enhancements

- Validate OpenAI's response format more robustly.
- Allow users to select the model dynamically.
- Tune prompt templates for smarter play.
- Add telemetry on request success/failures.


---

# 💬 Questions or Issues?

If you encounter any issues while using the OpenAIClientPlayer, feel free to open an [Issue](https://github.com/rzavalik/TicTacToe.Tournament/issues).

Happy hacking! 🚀

