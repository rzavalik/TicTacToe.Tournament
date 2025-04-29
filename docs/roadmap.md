# 📍 TicTacToe.Tournament - Roadmap

This document describes the planned next steps for the TicTacToe.Tournament project development.

## 🌟 Short-term Goals

1. **Fix WebUI Update Issues**  
   - Solve the current problem where the WebUI does not update properly during Tournament state changes (e.g., Planned → Ongoing → Finished).

2. **Local Development with Docker**  
   - Create documentation and adapt the ServerApp to run locally without requiring Azure SignalR, using a Docker command to simulate the environment.

3. **Infrastructure as Code (IaC)**  
   - Create Terraform scripts to automate the deployment of the platform.
   - Integrate Terraform into the CI pipeline for seamless infrastructure provisioning.

## 🛠️ Mid-term Improvements

4. **Separate WebAPI Services**  
   - Restructure the WebApp backend by creating dedicated APIs:
     - `api/tournament`
     - `api/player`
     - `api/auth`

5. **Multi-Tenant Tournament Management**  
   - Allow each authenticated user to manage their own tournaments.
   - Tournaments will support setting:
     - Timeout configuration (match or tournament timeout).
     - Minimum and maximum number of players.

6. **Flutter Mobile App**  
   - Start the development of a Flutter-based mobile app to allow users to join and play tournaments directly from Android and iOS devices.

## ☁️ Architecture Evolution

7. **Storage Provider Abstraction**  
   - Replace the current direct Azure Blob Storage usage with **[CloudStorageORM](https://github.com/rzavalik/CloudStorageORM)**, making the storage provider pluggable and more flexible across different cloud environments.

---

# ✅ Notes

- This roadmap is subject to adjustments based on new requirements, user feedback, and contributions.
- Contributions are welcome! Feel free to submit issues or pull requests.
