# 📍 TicTacToe.Tournament - Roadmap

This document describes the planned next steps for the TicTacToe.Tournament project development.

## 🌟 Short-term Goals

1. **Improve WebUI Synchronization**  
   - Ensure the Web UI accurately reflects tournament state transitions and handles real-time updates robustly.
   - This includes fixing potential regressions or display inconsistencies when matches start, end, or change players.

2. **Local Development with Docker**  
   - Create documentation and adapt the ServerApp to run locally without requiring Azure SignalR, using a Docker command to simulate the environment.

## 🛠️ Mid-term Improvements

3. **Separate WebAPI Services**  
   - Restructure the WebApp backend by creating dedicated APIs:
     - `api/tournament`
     - `api/player`
     - `api/auth`

4. **Multi-Tenant Tournament Management**  
   - Allow each authenticated user to manage their own tournaments.
   - Tournaments will support setting:
     - Timeout configuration (match or tournament timeout).
     - Minimum and maximum number of players.

5. **Flutter Mobile App**  
   - Start the development of a Flutter-based mobile app to allow users to join and play tournaments directly from Android and iOS devices.

## ☁️ Architecture Evolution

6. **Storage Provider Abstraction with CloudStorageORM**  
   - Replace the current direct Azure Blob Storage usage with **[CloudStorageORM](https://github.com/rzavalik/CloudStorageORM)**, enabling multi-cloud compatibility and a pluggable architecture.

7. **Stateless Design and Memory Optimization**  
   - Evolve the system to be more stateless by reducing in-memory state.
   - Load only the necessary data when needed and delegate persistence and query logic to the storage provider.

---

# ✅ Notes

- This roadmap is subject to adjustments based on new requirements, user feedback, and contributions.
- Contributions are welcome! Feel free to submit issues or pull requests.
