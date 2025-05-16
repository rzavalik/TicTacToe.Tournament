# TicTacToe Tournament - Architecture

Welcome to the **TicTacToe Tournament** project! This document provides a high-level overview of the system architecture.

---

# 📊 System Overview

TicTacToe Tournament is a real-time, scalable tournament platform built with **.NET 8**, **SignalR**, **Azure**, and **Docker**.

It allows multiple players (bots or human clients) to register in a tournament, and plays matches in real-time.

---

# 🌍 Components

| Component | Description |
|:---|:---|
| **ServerApp** | Hosts the SignalR Hub, manages tournaments, matches, and player state. Connects to Azure services if configured. |
| **WebApp** | Admin and Monitoring Web Interface. Allows creating, starting, canceling tournaments, and watching matches live. |
| **Player Bots** | SmartPlayer, DumbPlayer, and OpenAIClientPlayer. Each bot connects to the ServerApp and plays autonomously. |
| **Azure Storage** | (Optional) Persist tournament data (e.g., matches, player lists) to Azure Blob Storage. |


---

# 🔗 Communication Flow

Mermaid Diagram:

```mermaid
flowchart LR
    WebApp -->|SignalR| ServerApp
    DumbPlayer -->|SignalR| ServerApp
    SmartPlayer -->|SignalR| ServerApp
    OpenAIClientPlayer -->|SignalR| ServerApp
    ServerApp -->|Azure Blob Storage| Storage
```

- WebApp and Player Bots **connect via SignalR**.
- ServerApp orchestrates matches and game turns.
- ServerApp optionally **persists state** in Azure Blob Storage.


---

# 🛠️ Main Technologies

- **.NET 8**
- **ASP.NET Core SignalR**
- **Entity Framework (internal usage)**
- **Azure Blob Storage** (optional persistence)
- **Docker** (containerized deployment)
- **Playwright** (future UI testing)


---

# 📊 Repository Structure

```
TicTacToe.Tournament
├── docs/                                  # Project documentation (architecture, how-to guides)
├── infra/                                 # Terraform scripts for Azure infrastructure
├── src/                                   # All source code projects
│   ├── TicTacToe.Tournament.Auth/                   # Authentication and identity utilities
│   ├── TicTacToe.Tournament.BasePlayer/            # Base class and strategy interface for building bots
│   ├── TicTacToe.Tournament.DumbPlayer/            # Auto-play bot that selects random moves
│   ├── TicTacToe.Tournament.Models/                # Shared models and DTOs (e.g., Tournament, Match)
│   ├── TicTacToe.Tournament.MyBotPlayer/           # Sample custom bot for development/testing
│   ├── TicTacToe.Tournament.OpenAIClientPlayer/    # Bot that uses OpenAI's API to play
│   ├── TicTacToe.Tournament.Server/                # Domain logic for managing players and matches
│   ├── TicTacToe.Tournament.Server.App/            # SignalR Hub and real-time game orchestration
│   ├── TicTacToe.Tournament.SmartPlayer/           # Console-driven player (user inputs row/column)
│   └── TicTacToe.Tournament.WebApp/                # Admin web UI for tournament control and monitoring
├── tests/                                # Unit and BDD test projects
│   ├── TicTacToe.Tournament.Auth.Tests/
│   ├── TicTacToe.Tournament.Models.Tests/
│   ├── TicTacToe.Tournament.Player.Tests/
│   ├── TicTacToe.Tournament.Server.App.Tests/
│   ├── TicTacToe.Tournament.Server.Tests/
│   ├── TicTacToe.Tournament.WebApp.SpecTests/       # Reqnroll/SpecFlow BDD tests
│   └── TicTacToe.Tournament.WebApp.Tests/           # WebApp unit and integration tests
├── .github/                              # GitHub workflows and issue templates
├── Directory.Build.props                 # Shared MSBuild properties for all projects
├── README.md                             # Project overview and usage instructions
└── TicTacToe.Tournament.sln              # Visual Studio solution file
```


---

# 💡 Future Improvements

- Match replay storage and visualization.
- Bot leaderboards and statistics.
- Multi-round tournaments.
- Full Docker Compose stack.
- Azure DevOps/GitHub Actions CI/CD pipeline.


---

# 📢 Feedback

If you have suggestions to improve the architecture or new ideas, feel free to open an [Issue](https://github.com/rzavalik/TicTacToe.Tournament/issues).

Let's build it better together! ✨

