// === IMPORTS ===
import { startSpinner, stopSpinner, safeFetchJson, flashElement } from "./helpers.js";
import { renderMatches, renderLeaderboard, renderPlayers, updateMatchBoard } from "./renderers.js";
import { SymbolMap } from "./models.js";
// === GLOBALS ===
const tournamentId = document.getElementById("tournament")?.dataset.tournamentId;
const hub = window.tournamentHub;
// === LOADERS ===
function renderTournamentMeta(tournament) {
    const meta = document.getElementById("tournamentMeta");
    if (!meta)
        return;
    meta.innerHTML = `
        <p class="small text-center">
            <strong>Start:</strong> ${tournament.startTime ? new Date(tournament.startTime).toLocaleString() : '-'}  
            <strong>End:</strong> ${tournament.endTime ? new Date(tournament.endTime).toLocaleString() : '-'}  
            <strong>Duration:</strong> ${tournament.duration || '-'}
        </p>
    `;
}
export async function loadTournamentDetails(tournamentId) {
    if (!tournamentId)
        return;
    startSpinner("reloadTournamentBtn");
    try {
        const tournament = await safeFetchJson(`/tournament/${tournamentId}`);
        renderTournamentMeta(tournament);
        renderMatches(tournament.matches);
        renderPlayers(Object.entries(tournament.registeredPlayers)
            .map(([id, name]) => ({ id, name }))
            .sort((a, b) => a.name.localeCompare(b.name)));
        renderLeaderboard(Object.entries(tournament.leaderboard)
            .map(([id, score]) => ({
            name: tournament.registeredPlayers[id] ?? id,
            score
        }))
            .sort((a, b) => b.score - a.score));
        updateTournamentUI(tournament.status, tournament);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to load tournament.", 'error');
    }
    finally {
        stopSpinner("reloadTournamentBtn");
    }
}
export function toggleMatchesPanel(tournamentId) {
    const container = document.getElementById('matchesContainer');
    if (!container) {
        console.error("toggleMatchesPanel: matchesContainer not found.");
        return;
    }
    if (container.classList.contains('d-none')) {
        container.classList.remove('d-none');
    }
    else {
        container.classList.add('d-none');
    }
}
window.toggleMatchesPanel = toggleMatchesPanel;
async function loadMatches() {
    if (!tournamentId)
        return;
    startSpinner("reloadMatchesBtn");
    try {
        const matches = await safeFetchJson(`/tournament/${tournamentId}/matches`);
        renderMatches(matches);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to load matches.", 'error');
    }
    finally {
        stopSpinner("reloadMatchesBtn");
    }
}
async function loadLeaderboard() {
    if (!tournamentId)
        return;
    startSpinner("reloadLeaderboardBtn");
    try {
        const leaderboard = await safeFetchJson(`/tournament/${tournamentId}/leaderboard`);
        renderLeaderboard(leaderboard);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to load leaderboard.", 'error');
    }
    finally {
        stopSpinner("reloadLeaderboardBtn");
    }
}
async function loadPlayers() {
    if (!tournamentId)
        return;
    startSpinner("reloadPlayersBtn");
    try {
        const players = await safeFetchJson(`/tournament/${tournamentId}/players`);
        renderPlayers(players);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to load players.", 'error');
    }
    finally {
        stopSpinner("reloadPlayersBtn");
    }
}
function drawBoard(matchId, board) {
    let container = document.getElementById("boardContainer");
    if (!container) {
        container = document.createElement("div");
        container.id = "boardContainer";
        const center = document.getElementById("center");
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
            cell.textContent = SymbolMap[raw] ?? raw ?? " ";
            cell.classList.add("tic-cell");
            if (c < 2)
                cell.classList.add("border-right");
            if (r < 2)
                cell.classList.add("border-bottom");
            row.appendChild(cell);
        }
        table.appendChild(row);
    }
    container.appendChild(table);
}
function renderPodium(sorted, players) {
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
function renderPlannedTournamentUI(data) {
    const center = document.getElementById("center");
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
        }
        catch (err) {
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
        }
        catch (err) {
            console.error("Unexpected error:", err);
            alert("❌ An unexpected error occurred while cancelling the tournament.");
        }
    });
}
async function updateTournamentUI(status, data) {
    const center = document.getElementById("center");
    center.innerHTML = "";
    const titleElement = document.getElementById("tournamentTitle");
    if (titleElement && data.name) {
        titleElement.firstChild.textContent = data.name + " ";
    }
    const matchesContainer = document.getElementById("matches");
    if (matchesContainer) {
        if (["Ongoing", "Finished", "Cancelled"].includes(status)) {
            matchesContainer.classList.remove("d-none");
        }
        else {
            matchesContainer.classList.add("d-none");
        }
    }
    if (status === "Planned") {
        renderPlannedTournamentUI(data);
    }
    else if (status === "Ongoing") {
        await renderOngoingTournamentUI(data);
    }
    else if (status === "Finished") {
        renderFinishedTournamentUI(data);
    }
    else if (status === "Cancelled") {
        renderCancelledTournamentUI();
    }
    const meta = document.getElementById("tournamentMeta");
    if (meta != null) {
        if (status === "Planned") {
            meta.innerHTML = ``;
        }
        else {
            meta.innerHTML = `
                <p class="small text-center"><strong>Start:</strong> ${data.startTime ? new Date(data.startTime).toLocaleString() : '-'}  
                   <strong>End:</strong> ${data.endTime ? new Date(data.endTime).toLocaleString() : '-'}  
                   <strong>Duration:</strong> ${data.duration || '-'}</p>
            `;
        }
    }
}
async function renderOngoingTournamentUI(data) {
    const center = document.getElementById("center");
    const leaderboardContainer = document.getElementById("right");
    leaderboardContainer.innerHTML = "";
    let title = document.getElementById("matchTitle");
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
            }
            catch (err) {
                console.error("Unexpected error:", err);
                alert("❌ An unexpected error occurred while cancelling the tournament.");
            }
        });
        return;
    }
    const match = await res.json();
    drawBoard(match.id, match.board);
    const playerA = data.registeredPlayers[match.playerAId] ?? match.playerAId;
    const playerB = data.registeredPlayers[match.playerBId] ?? match.playerBId;
    let subtitle = document.getElementById("matchSubtitle");
    if (!subtitle) {
        subtitle = document.createElement("h5");
        subtitle.id = "matchSubtitle";
        center.appendChild(subtitle);
    }
    subtitle.textContent = `${playerA} vs ${playerB}`;
    let cancelBtn = document.getElementById("cancelBtn");
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
            }
            catch (err) {
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
}
function renderFinishedTournamentUI(data) {
    const center = document.getElementById("center");
    const sorted = Object.entries(data.leaderboard)
        .sort(([, scoreA], [, scoreB]) => scoreB - scoreA);
    center.innerHTML = `
        <h3>🏁 Tournament Finished</h3>
        <div class="podium-container d-flex justify-content-center align-items-end mt-4">
            ${renderPodium(sorted, data.registeredPlayers)}
        </div>
    `;
}
function renderCancelledTournamentUI() {
    const center = document.getElementById("center");
    center.innerHTML = "<h3 class='text-danger'>Tournament Cancelled</h3>";
}
// === ACTIONS ===
async function renameTournament() {
    if (!tournamentId)
        return;
    const { value: newName } = await Swal.fire({
        title: 'Rename Tournament',
        input: 'text',
        inputLabel: 'New tournament name:',
        inputPlaceholder: 'Enter new name',
        showCancelButton: true,
        inputValidator: (value) => {
            if (!value)
                return 'Please enter a valid name!';
        }
    });
    if (!newName)
        return;
    try {
        await safeFetchJson(`/tournament/${tournamentId}/rename`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ newName })
        });
        await Swal.fire('Success!', 'Tournament renamed.', 'success');
        const titleElement = document.getElementById("tournamentTitle");
        if (titleElement) {
            titleElement.firstChild.textContent = newName + " ";
            flashElement(titleElement);
        }
        document.title = newName + " - Tournament";
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to rename tournament.", 'error');
    }
}
window.renameTournament = renameTournament;
async function renamePlayer(playerId) {
    if (!tournamentId)
        return;
    const { value: newName } = await Swal.fire({
        title: 'Rename Player',
        input: 'text',
        inputLabel: 'New player name:',
        inputPlaceholder: 'Enter new name',
        showCancelButton: true,
        inputValidator: (value) => {
            if (!value)
                return 'Please enter a valid name!';
        }
    });
    if (!newName)
        return;
    try {
        await safeFetchJson(`/tournament/${tournamentId}/player/${playerId}/rename`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ newName })
        });
        await Swal.fire('Success!', 'Player renamed.', 'success');
        loadPlayers();
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', error.message || "Failed to rename player.", 'error');
    }
}
window.renamePlayer = renamePlayer;
export async function fetchMatch(tournamentId, matchId) {
    try {
        const res = await fetch(`/tournament/${tournamentId}/match/${matchId}`);
        if (!res.ok) {
            console.error(`Failed to fetch match ${matchId}:`, res.statusText);
            return null;
        }
        return await res.json();
    }
    catch (error) {
        console.error(`Error fetching match ${matchId}:`, error);
        return null;
    }
}
window.fetchMatch = fetchMatch;
// === HUB EVENTS ===
hub.onPlayerRegistered(async () => {
    console.log("Player registered → Reloading players and tournament UI.");
    await loadPlayers();
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
    await loadLeaderboard();
});
hub.onRefreshLeaderboard(() => loadLeaderboard());
hub.onTournamentUpdated(async () => {
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
});
hub.onTournamentCancelled(() => updateTournamentUI("Cancelled", {}));
hub.onMatchStarted(async (matchId) => {
    await fetchMatch(String(tournamentId), matchId);
});
hub.onMatchEnded(async (matchId) => {
    await fetchMatch(String(tournamentId), matchId);
});
hub.onReceiveBoard(async (matchId, board) => {
    await drawBoard(matchId, board);
    await updateMatchBoard(matchId, board);
});
hub.onTournamentUpdated(async () => {
    console.log("Tournament updated → Reloading leaderboard and matches...");
    await Promise.all([
        loadLeaderboard(),
        loadMatches()
    ]);
});
hub.onTournamentUpdated(async () => {
    console.log("Tournament updated → Reloading leaderboard and matches...");
    await Promise.all([
        loadLeaderboard(),
        loadMatches()
    ]);
});
hub.onPlayerRegistered(async () => {
    console.log("Player registered → Reloading players...");
    await loadPlayers();
});
// === DOM EVENTS ===
document.getElementById("toggleMatchesPanel")?.addEventListener('click', () => { toggleMatchesPanel(tournamentId); });
document.getElementById("reloadPlayersBtn")?.addEventListener("click", loadPlayers);
document.getElementById("reloadLeaderboardBtn")?.addEventListener("click", loadLeaderboard);
document.getElementById("reloadMatchesBtn")?.addEventListener("click", loadMatches);
document.getElementById("renameTournamentBtn")?.addEventListener("click", renameTournament);
document.querySelectorAll(".renamePlayerBtn").forEach(button => {
    button.addEventListener("click", (e) => {
        const playerId = e.currentTarget.getAttribute("data-player-id");
        if (playerId) {
            renamePlayer(playerId);
        }
    });
});
// === INITIALIZATION ===
document.addEventListener("DOMContentLoaded", async () => {
    await loadTournamentDetails(tournamentId);
    try {
        setTimeout(() => hub.subscribeToTournament(tournamentId), 2000);
    }
    catch { }
});
