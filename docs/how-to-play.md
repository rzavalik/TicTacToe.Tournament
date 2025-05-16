# How to Play TicTacToe Tournament

Welcome to the TicTacToe Tournament! This guide explains how to participate in a tournament using one of the available players: DumbPlayer, SmartPlayer, or OpenAIClientPlayer.

---

## 🕹️ Step-by-Step: How to Join a Tournament

### 1. Download a Player

Choose and download the latest version of one of the players:

- **DumbPlayer**
- **SmartPlayer**
- **OpenAIClientPlayer**

These can be found in the latest GitHub [Releases](https://github.com/rzavalik/TicTacToe.Tournament/releases).

---

### 2. Create a Tournament

1. Visit the Web UI:
   👉 https://tictactoe-webui.victoriousriver-bb51cd74.eastus2.azurecontainerapps.io/

2. Click on **"Create Tournament"**.
3. Copy the generated **Tournament ID** — you'll need it to join the game.

---

### 3. Launch a Player

Each player will ask you for:

- **Player Name**
- **Tournament ID**

If you are deploying it yourself, you will need to change the `Hub URL` as well as `Web UI endpoint` in the `appSettings.json` of the desired bot.
You can get the Hub URL and Web UI endpoint from Terraform output, or directly from the app if hosted publicly.

---

## 🧠 Player Types & Strategies

### 🟢 DumbPlayer

- Plays completely automatically.
- Chooses randomly an available empty square.
- No human interaction required.

### 🔵 SmartPlayer

- Prompts the user to enter the move (row and column).
	- So you will hit a number from 0-2 and hit enter for Row.
	- Then you will hit a number from 0-2 and hit enter for Column.
	- If the position is valid, it will accept the move.
	- If the position is invalid, you need to provide again the move.
- Ideal for manually playing against bots or other users.
- Simple console-based interface.

### 🧠 OpenAIClientPlayer

- Uses OpenAI's ChatGPT API to decide the best move.
- Requires a valid OpenAI API Key.
- Instructions to configure it are available here:
  👉 [OpenAI Bot Setup](https://github.com/rzavalik/TicTacToe.Tournament/blob/main/docs/openai-bot.md)

---

Have fun playing and building smarter bots!