import { TournamentHubClient } from "./tournamentHubClient.js";

const hub = (window as any).tournamentHub;
const tournamentId = (document.getElementById("tournament") as HTMLElement).dataset.tournamentId;

function drawBoard(board: string[][]) {
    let container = document.getElementById("boardContainer") as HTMLDivElement | null;

    if (!container) {
        container = document.createElement("div");
        container.id = "boardContainer";
        const center = document.getElementById("center")!;
        center.appendChild(container);
    }

    container.innerHTML = ""; 

    const table = document.createElement("table");
    table.id = "boardTable";
    table.className = "table table-borderless text-center";

    for (let r = 0; r < 3; r++) {
        const row = document.createElement("tr");

        for (let c = 0; c < 3; c++) {
            const cell = document.createElement("td");

            const value = board[r]?.[c];
            if (value == null || value == '32') {
                cell.textContent = ' ';
            } else if (value == '88') {
                cell.textContent = 'X';
            } else if (value == '79') {
                cell.textContent = 'O';
            } else {
                cell.textContent = value;
            }

            cell.classList.add("tic-cell");
            if (c < 2) cell.classList.add("border-right");
            if (r < 2) cell.classList.add("border-bottom");

            row.appendChild(cell);
        }

        table.appendChild(row);
    }

    container.appendChild(table);
}

async function loadPlayers() {
    const res = await fetch(`/tournament/${tournamentId}/players`);
    const players = await res.json();
    const left = document.getElementById("left")!;
    left.innerHTML = "<h4>Players</h4>";

    const ul = document.createElement("ul");
    for (const p of players) {
        const li = document.createElement("li");
        li.innerHTML = `<span title="${p.name} (${p.id})">${p.name}</span>`;
        ul.appendChild(li);
    }

    left.appendChild(ul);
}

async function loadLeaderboard() {
    const res = await fetch(`/tournament/${tournamentId}/leaderboard`);
    const leaderboard = await res.json();
    const right = document.getElementById("right")!;
    right.innerHTML = "<h4>Leaderboard</h4>";

    const table = document.createElement("table");
    table.className = "table table-striped";
    const tbody = document.createElement("tbody");
    leaderboard.forEach((entry: any, idx: number) => {
        const row = document.createElement("tr");
        row.innerHTML = `<td>${idx + 1}</td><td>${entry.name}</td><td>${entry.score}</td>`;
        tbody.appendChild(row);
    });
    table.appendChild(tbody);
    right.appendChild(table);
}

async function updateTournamentUI(status: string, data: any) {
    const center = document.getElementById("center")!;
    const leaderboardContainer = document.getElementById("right")!;
    center.innerHTML = "";
    leaderboardContainer.innerHTML = "";

    if (status === "Planned") {
        const numPlayers = Object.keys(data.registeredPlayers).length;
        const canStart = numPlayers >= 2;

        center.innerHTML = `
            <h3>Match not started</h3>
            <button id="startBtn" class="btn btn-success" ${!canStart ? "disabled" : ""}>Start Tournament</button>
            <button id="cancelBtn" class="btn btn-danger">Cancel Tournament</button>
            ${!canStart ? `<p class="text-muted mt-2">At least 2 players are required to start the tournament.</p>` : ""}
        `;
        document.getElementById("startBtn")?.addEventListener("click", async () => {
            try {
                const response = await fetch(`/tournament/${tournamentId}/start`, { method: "POST" });

                if (!response.ok) {
                    const message = await response.text();
                    alert(`⚠️ Failed to start tournament: ${message}`);
                    return;
                }
            } catch (err) {
                console.error("Unexpected error:", err);
                alert("❌ An unexpected error occurred while starting the tournament.");
            }
        });
        document.getElementById("cancelBtn")?.addEventListener("click", async () => {
            try {
                const response = await fetch(`/tournament/${tournamentId}/cancel`, { method: "POST" });

                if (!response.ok) {
                    const message = await response.text();
                    alert(`⚠️ Failed to cancel tournament: ${message}`);
                    return;
                }
            } catch (err) {
                console.error("Unexpected error:", err);
                alert("❌ An unexpected error occurred while cancelling the tournament.");
            }
        });
    } else if (status === "Ongoing") {
        const center = document.getElementById("center")!;
        const leaderboardContainer = document.getElementById("right")!;
        leaderboardContainer.innerHTML = "";

        let title = document.getElementById("matchTitle") as HTMLHeadingElement | null;
        if (!title) {
            title = document.createElement("h3");
            title.id = "matchTitle";
            title.textContent = "Current Match";
            center.appendChild(title);
        }

        const match = await (await fetch(`/tournament/${tournamentId}/match/current`)).json();
        drawBoard(match.board);

        const playerA = data.registeredPlayers[match.playerAId] ?? match.playerAId;
        const playerB = data.registeredPlayers[match.playerBId] ?? match.playerBId;

        let subtitle = document.getElementById("matchSubtitle") as HTMLHeadingElement | null;
        if (!subtitle) {
            subtitle = document.createElement("h5");
            subtitle.id = "matchSubtitle";
            center.appendChild(subtitle);
        }
        subtitle.textContent = `${playerA} vs ${playerB}`;

        let cancelBtn = document.getElementById("cancelBtn") as HTMLButtonElement | null;
        if (!cancelBtn) {
            cancelBtn = document.createElement("button");
            cancelBtn.id = "cancelBtn";
            cancelBtn.textContent = "Cancel Tournament";
            cancelBtn.className = "btn btn-danger mt-3 d-block mx-auto";
            cancelBtn.addEventListener("click", async () => {
                try {
                    const response = await fetch(`/tournament/${tournamentId}/cancel`, { method: "POST" });
                    if (!response.ok) {
                        const message = await response.text();
                        alert(`⚠️ Failed to cancel tournament: ${message}`);
                        return;
                    }
                } catch (err) {
                    console.error("Unexpected error:", err);
                    alert("❌ An unexpected error occurred while cancelling the tournament.");
                }
            });
            center.appendChild(cancelBtn);
        }

        leaderboardContainer.innerHTML = "<h4>Leaderboard</h4>";
        const table = document.createElement("table");
        table.className = "table table-striped";
        const tbody = document.createElement("tbody");

        Object.entries(data.leaderboard).forEach(([playerId, score], idx) => {
            const name = data.registeredPlayers[playerId] ?? "Unknown";
            const row = document.createElement("tr");
            row.innerHTML = `<td>${idx + 1}</td><td>${name}</td><td>${score}</td>`;
            tbody.appendChild(row);
        });

        table.appendChild(tbody);
        leaderboardContainer.appendChild(table);
    } else if (status === "Finished") {
        const sorted = Object.entries(data.leaderboard as Record<string, number>)
            .sort(([, scoreA], [, scoreB]) => (scoreB as number) - (scoreA as number));
        center.innerHTML = "<h3>🏁 Tournament Finished</h3><ul style='list-style: none;'>" +
            sorted.slice(0, 3).map(([playerId, score], i) => {
                const name = data.registeredPlayers?.[playerId] ?? "Unknown";
                var medal: string = '';
                if (i == 0) {
                    medal = '🥇';
                } else if (i == 1) {
                    medal = '🥈';
                } else if (i == 2) {
                    medal = '🥉'
                }
                return `<li><strong>${medal}</strong> ${name} (${score} pts)</li>`;
            }).join("") +
            "</ul>";
    } else if (status === "Cancelled") {
        center.innerHTML = "<h3 class='text-danger'>Tournament Cancelled</h3>";
    }

    const meta = document.getElementById("tournamentMeta");
    if (meta != null) {
        if (status === "Planned") {
            meta.innerHTML = ``
        } else {
            meta.innerHTML = `
                <p class="small text-center"><strong>Start:</strong> ${data.startTime ? new Date(data.startTime).toLocaleString() : '-'}  
                   <strong>End:</strong> ${data.endTime ? new Date(data.endTime).toLocaleString() : '-'}  
                   <strong>Duration:</strong> ${data.duration || '-'}</p>
            `;
        }
    }
}

hub.onPlayerRegistered(() => loadPlayers());
hub.onRefreshLeaderboard(() => loadLeaderboard());
hub.onTournamentUpdated(async () => {
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
    loadPlayers();
    loadLeaderboard();
});
hub.onTournamentCancelled(() => updateTournamentUI("Cancelled", {}));
hub.onMatchStarted(() => updateTournamentUI("Ongoing", {}));
hub.onMatchEnded(() => updateTournamentUI("Ongoing", {}));
hub.onReceiveBoard((board: string[][]) => drawBoard(board));

document.addEventListener("DOMContentLoaded", async () => {

    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }

    loadPlayers();
    loadLeaderboard();

    setTimeout(() => hub.subscribeToTournament(tournamentId), 2000);
});

