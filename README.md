# TicTacToe Tournament 🎮

Real-time .NET 8 Tic Tac Toe Tournament platform powered by SignalR, Azure, Docker, and OpenAI bots.

![License](https://img.shields.io/github/license/rzavalik/TicTacToe.Tournament?color=blue)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Build Status](https://img.shields.io/github/actions/workflow/status/rzavalik/TicTacToe.Tournament/ci.yml?branch=main)

---

# 📋 Table of Contents
- [About](#about)
- [Features](#features)
- [Architecture Overview](#architecture-overview)
- [Getting Started](#getting-started)
- [OpenAI Bot](#openai-bot)
- [Contributing](#contributing)
- [License](#license)
- [Sponsor](#sponsor)

---

# 📖 About

![Bots playing against each other](https://github.com/user-attachments/assets/24b26135-a5b2-4b31-8f95-50ea728a7d96)

**TicTacToe Tournament** is a real-time, cloud-native tournament system built with **.NET 8** and **SignalR**.  
It allows multiple bot players (or future human players) to connect, play matches, and orchestrates tournaments automatically.

Built for scalability and AI integration.

---

# 🚀 Features

- ⚡ Real-time multiplayer via SignalR
- ☁️ Azure-ready architecture
- 🐳 Docker-friendly deployment
- 🧠 OpenAI-powered Bot Player
- 🎯 Fully tested with xUnit, Moq, and Shouldly
- 🛡️ Clean architecture and extensible design
- 📈 Easy to add your own bots and strategies

---

# 🏛️ Architecture Overview

The system is composed of:
- **ServerApp**: Hosts the tournaments and SignalR hub.
- **WebApp**: UI for tournament creation and monitoring.
- **Player Clients**: DumbPlayer, SmartPlayer, OpenAIClientPlayer.

👉 See full [Architecture Documentation](./docs/architecture.md).

---

# 🛠️ Getting Started

### Prerequisites
- .NET 8 SDK
- (Optional) Docker

---

# 🧠 OpenAI Bot

The **OpenAIClientPlayer** uses the ChatGPT API to select moves based on the current board state.

👉 See [OpenAI Bot Documentation](./docs/openai-bot.md).

---

# 🤝 Contributing

We welcome contributions!

Please read the [Contributing Guidelines](./CONTRIBUTING.md) before opening a pull request.

---

# 📝 License

Distributed under the **GPL-3.0** license.  
See [LICENSE](./LICENSE) for more information.

---

# ❤️ Sponsor

If you find this project useful, please consider [buying me a coffee](https://www.buymeacoffee.com/rzavalik)! ☕

---
