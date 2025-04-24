# Code Challenge - 2025/04

![Sample Tournament Ongoing](https://github.com/user-attachments/assets/c28d07f9-1e20-4d16-9cd9-d0e427fe84df)
![Tournament Finished](https://github.com/user-attachments/assets/855c76f8-da71-4509-97fd-7acd815d3e6e)

## Welcome Aboard, Bot Designer!

Welcome to the **TicTacToe Tournament**, a modern .NET 8 application for managing automated bot competitions.

This system includes a user interface built with ASP.NET MVC that allows users to manage tournaments and spectate live matches. At the core of the communication architecture is **SignalR**, enabling real-time interaction between the UI and the participating bots.

The backend is composed of two services:
- **Web UI**: ASP.NET MVC site
- **AppServer**: SignalR hub orchestrator and persistence engine

Both services are deployed to [Azure Container Instances](https://learn.microsoft.com/en-us/azure/container-instances/container-instances-overview) and communicate in real time via SignalR.

```mermaid
graph TD
    subgraph Client Side
        UI[Web Admin UI]
        Bot1[SmartBotClient]
        Bot2[DumbBotClient]
        Bot3[Xyz-BotClient]
    end

    subgraph Server Side
        AppServer[AppServer: SignalR Hub]
    end

    UI -->|SignalR| AppServer
    Bot1 -->|SignalR| AppServer
    Bot2 -->|SignalR| AppServer
    Bot3 -->|SignalR| AppServer
```

The **AppServer** hosts the SignalR Hub, handling all real-time communication. The **Web UI** allows users to create tournaments, monitor games, and manage players. Both services rely on SignalR for real-time low-latency communication.

To simplify persistence, tournament state is saved as JSON files within [Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction). Each tournament has its own folder inside the `games` container, containing files like `tournament.json`, `players.json`, etc.

> **Note:** An [Azure Blob Storage Lifecycle Management](https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview) policy automatically deletes old tournaments **after 4 hours**, optimizing storage usage.

---

## Authentication Flow

```mermaid
sequenceDiagram
    participant Bot as Bot Client
    participant App as AppServer (SignalR Hub)
    participant UI as Web UI
    participant Blob as Azure Blob Storage

    UI->>App: Request to create tournament
    App->>Blob: Check if tournament exists
    alt Tournament exists
        Blob-->>App: Load tournament state
    else Tournament does not exist
        App->>Blob: Save new tournament state
        App-->>UI: Acknowledge creation
        App-->>Bot: Notify all via SignalR
    end

    Bot->>UI: Authenticate to join tournament
    UI->>App: Validate tournament ID
    App-->>UI: Confirm tournament exists
    UI->>Bot: Return SignalR Token

    Bot->>App: Register via SignalR (using token)
    App->>Blob: Save updated tournament state
```

This sequence shows how the Web UI creates a tournament and how bots join using SignalR. All communication is persisted through Azure Blob Storage.

---

## Tournament Lifecycle Flow

```mermaid
flowchart TD
    A[Web UI creates the Tournament] --> B[AppServer checks tournament in Blob Storage]
    B --> C[If not, initializes tournament state]
    C --> D[Returns TournamentId to UI]

    subgraph Bots
        E1[Bot1 authenticates via UI]
        E2[Bot2 authenticates via UI]
        E3[Bot3 authenticates via UI]
    end

    D --> E1 --> F1[UI returns Token to Bot1]
    D --> E2 --> F2[UI returns Token to Bot2]
    D --> E3 --> F3[UI returns Token to Bot3]

    F1 --> G1[Bot1 registers via SignalR]
    F2 --> G2[Bot2 registers via SignalR]
    F3 --> G3[Bot3 registers via SignalR]

    G1 --> H1[AppServer saves Bot1 state]
    G2 --> H2[AppServer saves Bot2 state]
    G3 --> H3[AppServer saves Bot3 state]

    I[Web UI starts the Tournament] --> J[AppServer generates matches]
    J --> K[Matches run sequentially with timeouts]

    K --> L[Players notified via SignalR]
    L --> M[Bots play within 1 minute]

    M --> N[AppServer validates move and updates state]
    N --> O[State saved in Blob Storage]
    O --> P{More matches?}
    P -->|Yes| K
    P -->|No| Q[AppServer computes leaderboard]
    Q --> R[Web UI shows rankings]
```

Matches follow a home/away format. Each bot competes at least twice against each opponent. Timeouts, draws, and forfeits are handled programmatically.

| Result   | Points |
|----------|--------|
| Win      |   3    |
| Draw     |   1    |
| Loss     |   0    |
| Forfeit  |  -1    |

---

## ❓ FAQ

<details>
<summary><strong>How do I create a bot for the TicTacToe Tournament?</strong></summary>

See [How to Create a Bot.md](./How-To-Create-A-Bot.md). It provides everything you need to build a bot that integrates with the tournament engine.

</details>

<details>

<summary><strong>Where do I find the tournament architecture and SignalR setup?</strong></summary>

In this [README.md](./README.md) there's an in-depth explanation of the system design, tournament flows, and infrastructure.

</details>

<details>
<summary><strong>What’s SignalR and Azure SignalR Offering?</strong></summary>

[SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) is a real-time communication library for ASP.NET. It simplifies bi-directional communication between client and server.

The [Azure SignalR Service](https://learn.microsoft.com/azure/azure-signalr/signalr-overview) offers a managed, scalable infrastructure for SignalR apps, handling scale-out, connections, and backplane messaging.

</details>

<details>
<summary><strong>How Does SignalR Relate to AWS SQS and Kafka?</strong></summary>

While [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) is ideal for **real-time** messaging between clients and servers, [AWS SQS](https://aws.amazon.com/sqs/) and [Apache Kafka](https://kafka.apache.org/) focus on **asynchronous**, **durable**, and **scalable** messaging.

SignalR is designed for scenarios requiring immediate feedback — like games or chat. Kafka and SQS excel in high-throughput, distributed, fault-tolerant data streaming.

You can combine SignalR with Azure services like:
- [Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-overview)
- [Azure Event Hubs](https://learn.microsoft.com/azure/event-hubs/event-hubs-about)
- [Azure Event Grid](https://learn.microsoft.com/azure/event-grid/overview)

to support observability, guaranteed delivery, dead-lettering, and retries.

</details>
