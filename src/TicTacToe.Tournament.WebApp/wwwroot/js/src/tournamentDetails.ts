import { TournamentHubClient } from "./tournamentHubClient.js";

const hub = (window as any).tournamentHub;
const tournamentId = (document.getElementById("tournament") as HTMLElement).dataset.tournamentId;
const MatchStatus = {
    0: "Planned",
    1: "Ongoing",
    2: "Finished",
    3: "Cancelled"
};
const symbolMap: Record<string, string> = {
    "32": " ",
    "88": "X",
    "79": "O"
};
let matchesExpanded = false;

async function fetchMatches(tournamentId: string) {
    const res = await fetch(`/tournament/${tournamentId}/matches`);
    if (!res.ok) return [];

    return await res.json();
}

async function toggleMatchesPanel(tournamentId: string) {
    const container = document.getElementById("matchesPanel");
    if (matchesExpanded) {
        container!.innerHTML = "";
        matchesExpanded = false;
        return;
    }

    const matches = await fetchMatches(tournamentId);
    container!.innerHTML = matches.map((match: any, idx: number) => `
        <div class="match-entry col-md-3 mt-3" data-match-id="${match.id}">
            <div class="small">
                <div><strong>Match ${idx + 1}:</strong></div>
                <div>${match.playerAName} vs ${match.playerBName}</div>
                <div><strong>Status:</strong> ${MatchStatus[match.status as keyof typeof MatchStatus] ?? match.status}</div>
                <div><strong>Duration:</strong> ${match.duration || '-'}</div>
            </div>
            ${renderMatchBoard(match.board)}
        </div>
    `).join('');

    matchesExpanded = true;
}
(window as any).toggleMatchesPanel = toggleMatchesPanel;

function renderMatchBoard(board: string[][]): string {
    const emptyRow = ["", "", ""];
    const emptyBoard = [emptyRow, emptyRow, emptyRow];

    if (!board || board.every(row => row === null)) {
        board = emptyBoard;
    }

    return `
        <table class="match-board">
            ${board.map(row => `
                <tr>
                    ${row.map(cell => `<td>${cell === "Empty" ? "" : symbolMap[cell] ?? cell ?? " "}</td>`).join('')}
                </tr>`).join('')}
        </table>
    `;
}

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
            const raw = board[r]?.[c];
            cell.textContent = symbolMap[raw] ?? raw ?? " ";
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
    ul.className = "small";
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
    table.className = "table table-striped small";
    const tbody = document.createElement("tbody");
    leaderboard.forEach((entry: any, idx: number) => {
        const row = document.createElement("tr");
        row.innerHTML = `<td>${idx + 1}</td><td>${entry.name}</td><td>${entry.score}</td>`;
        tbody.appendChild(row);
    });
    table.appendChild(tbody);
    right.appendChild(table);
}

function renderPodium(sorted: [string, number][], players: Record<string, string>) {
    const podium = [
        { place: 2, color: 'blue', height: 120, medal: '🥈' },
        { place: 1, color: 'gold', height: 180, medal: '🥇' },
        { place: 3, color: 'green', height: 80, medal: '🥉' },
    ];

    return podium.map((p, i) => {
        const [playerId, score] = sorted[p.place - 1] || [null, null];
        const name = playerId ? players[playerId] ?? "Unknown" : "-";

        return `
            <div class="text-center mx-2">
                <div style="font-weight:bold;">${name}</div>
                <div style="
                    height: ${p.height}px;
                    width: 80px;
                    background: ${p.color};
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    color: white;
                    font-size: 2em;
                    font-weight: bold;
                ">${p.medal}</div>
            </div>
        `;
    }).join('');
}

async function updateMatch(matchId: string) {
    const res = await fetch(`/tournament/${tournamentId}/match/${matchId}`);
    if (!res.ok) return;

    const match = await res.json();

    const matchDiv = document.querySelector(`[data-match-id="${matchId}"]`) as HTMLDivElement | null;
    if (!matchDiv) return;

    matchDiv.innerHTML = `
        <div class="small">
            <div><strong>${match.playerAName} vs ${match.playerBName}</strong></div>
            <div><strong>Status:</strong> ${MatchStatus[match.status as keyof typeof MatchStatus] ?? match.status}</div>
            <div><strong>Duration:</strong> ${match.duration ?? "-"}</div>
        </div>
        ${renderMatchBoard(match.board)}
    `;
}

async function updateTournamentUI(status: string, data: any) {
    const center = document.getElementById("center")!;
    const leaderboardContainer = document.getElementById("right")!;
    center.innerHTML = "";
    leaderboardContainer.innerHTML = "";

    const matchesContainer = document.getElementById("matches");
    if (matchesContainer) {
        if (["Ongoing", "Finished", "Cancelled"].includes(status)) {
            matchesContainer.classList.remove("d-none");
        } else {
            matchesContainer.classList.add("d-none");
        }
    }

    if (status === "Planned") {
        const numPlayers = Object.keys(data.registeredPlayers).length;
        const canStart = numPlayers >= 2;

        center.innerHTML = `
            <h3>Match not started</h3>
            <blockquote class="blockquote text-center">
                <p>Use this ID to register to this tournament:</p>
                <h6 style="border:1px solid #666">${tournamentId}</h6>
            </blockquote>
            <br/>
            <br/>
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

        const res = await fetch(`/tournament/${tournamentId}/match/current`);
        if (res.status != 200) {
            center.innerHTML = `
                <div class="d-flex flex-column align-items-center justify-content-center" style="min-height: 200px;">
                    <div class="display-1 text-muted">🕒</div>
                    <div class="text-muted">Waiting for the next match...</div>
                </div>
                <br/>
                <button id="cancelBtn" class="btn btn-danger">Cancel Tournament</button>
            `;
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

            return;
        }

        const match = await res.json();

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
        center.innerHTML = `
            <h3>🏁 Tournament Finished</h3>
            <div class="podium-container d-flex justify-content-center align-items-end mt-4">
                ${renderPodium(sorted, data.registeredPlayers)}
            </div>
        `;
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
hub.onMatchStarted(async (matchId: string) => {
    await updateMatch(matchId);
});

hub.onMatchEnded(async (matchId: string) => {
    await updateMatch(matchId);
});

hub.onReceiveBoard(async (matchId: string, board: string[][]) => {
    await updateMatch(matchId);
    await drawBoard(board);
});

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