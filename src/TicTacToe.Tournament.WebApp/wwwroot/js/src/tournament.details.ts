// === IMPORTS ===
import { startSpinner, stopSpinner, safeFetchJson, flashElement } from "./helpers.js";
import { renderMatches, renderLeaderboard, renderPlayers, updateMatchBoard } from "./renderers.js";
import { MatchDto, LeaderboardEntryDto, PlayerDto, TournamentDetailsDto, SymbolMap, MatchStatus } from "./models.js";
import { TournamentHubClient } from "./tournament.hub.js";

// === GLOBALS ===
const tournamentId = (document.getElementById("tournament") as HTMLElement)?.dataset.tournamentId!;
const hub = (window as any).tournamentHub as TournamentHubClient;
// === LOADERS ===
function renderTournamentMeta(tournament: TournamentDetailsDto): void {
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
export async function loadTournamentDetails(tournamentId: string): Promise<void> {
    if (!tournamentId)
        return;
    startSpinner("reloadTournamentBtn");
    try {
        const tournament = await safeFetchJson<TournamentDetailsDto>(`/tournament/${tournamentId}`);
        renderTournamentMeta(tournament);
        renderMatches(tournament.matches);
        renderPlayers(Object.entries(tournament.registeredPlayers)
            .map(([id, name]) => ({ id, name }))
            .sort((a, b) => a.name.localeCompare(b.name)));
        renderLeaderboard(tournament.leaderboard);
        updateTournamentUI(tournament.status, tournament);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', (error as Error).message || "Failed to load tournament.", 'error');
    }
    finally {
        stopSpinner("reloadTournamentBtn");
    }
}
export function toggleMatchesPanel(tournamentId: string): void {
    const toggleMatchesBtn = document.getElementById('toggleMatchesBtn')!;
    const container = document.getElementById('matchesContainer');
    if (!container) {
        console.error("toggleMatchesPanel: matchesContainer not found.");
        return;
    }
    if (container.classList.contains('d-none')) {
        container.classList.remove('d-none');
        toggleMatchesBtn.innerText = 'Hide Matches';
    }
    else {
        container.classList.add('d-none');
        toggleMatchesBtn.innerText = 'Show Matches';
    }
}
async function loadMatches(): Promise<void> {
    if (!tournamentId)
        return;
    startSpinner("reloadMatchesBtn");
    try {
        const matches = await safeFetchJson<MatchDto[]>(`/tournament/${tournamentId}/matches`);
        renderMatches(matches);
        if (matches.every(m => MatchStatus[Number(m.status)] == 'Finished' || MatchStatus[Number(m.status)] == 'Cancelled')) {
            const res = await fetch(`/tournament/${tournamentId}`);
            if (res.ok) {
                const data = await res.json();
                await updateTournamentUI(data.status, data);
            }
        }
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', (error as Error).message || "Failed to load matches.", 'error');
    }
    finally {
        stopSpinner("reloadMatchesBtn");
    }
}
async function loadLeaderboard(): Promise<void> {
    if (!tournamentId)
        return;
    startSpinner("reloadLeaderboardBtn");
    try {
        const leaderboard = await safeFetchJson<LeaderboardEntryDto[]>(`/tournament/${tournamentId}/leaderboard`);
        renderLeaderboard(leaderboard);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', (error as Error).message || "Failed to load leaderboard.", 'error');
    }
    finally {
        stopSpinner("reloadLeaderboardBtn");
    }
}
async function loadPlayers(): Promise<void> {
    if (!tournamentId)
        return;
    startSpinner("reloadPlayersBtn");
    try {
        const players = await safeFetchJson<PlayerDto[]>(`/tournament/${tournamentId}/players`);
        renderPlayers(players);
        checkNumOfPlayersToStart(players.length);
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', (error as Error).message || "Failed to load players.", 'error');
    }
    finally {
        stopSpinner("reloadPlayersBtn");
    }
}
async function drawBoard(matchId: string, board: string[][], match: MatchDto | null) {
    let container = document.getElementById("boardContainer");
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
            cell.innerHTML = SymbolMap[raw] ?? raw ?? " ";
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
    if (match == null) {
        const res = await safeFetchJson<MatchDto>(`/tournament/${tournamentId}/match/current`);
    }
    if (match == null) {
        let subtitle = document.getElementById("matchSubtitle");
        if (subtitle != null) {
            if (!subtitle.classList.contains("d-none")) {
                subtitle.classList.add("d-none");
            }
        }
    }
    else {
        const playerA = match.playerAName ?? match.playerAId ?? '';
        const playerB = match.playerBName ?? match.playerBId ?? '';
        let subtitle = document.getElementById("matchSubtitle");
        if (!subtitle) {
            subtitle = document.createElement("h5");
            subtitle.id = "matchSubtitle";
            container.appendChild(subtitle);
        }
        else {
            if (subtitle.classList.contains("d-none")) {
                subtitle.classList.remove("d-none");
            }
        }
        subtitle.innerHTML = `${playerA} (<i class="fa-solid fa-xmark"></i>) vs ${playerB} (<i class="fa- regular fa-circle"></i>)`;
    }
}
function renderPodium(sorted: [string, number][], players: Record<string, string>) {
    const podium = [
        { place: 2, color: 'blue', height: 120, medal: '🥈' },
        { place: 1, color: 'gold', height: 180, medal: '🥇' },
        { place: 3, color: 'green', height: 80, medal: '🥉' },
    ];
    return podium.map((p, i) => {
        var name = '';
        try { name = (sorted[i]['1'] as any).playerName; }
        catch { name = ''; }
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
function checkNumOfPlayersToStart(numPlayers: number) {
    try {
        const canStart = numPlayers >= 2;
        const startButton = document.getElementById("startBtn");
        if (startButton != null) {
            if (canStart) {
                if (startButton.classList.contains("disabled")) {
                    startButton.classList.remove("disabled");
                }
                startButton.attributes.removeNamedItem("disabled");
            }
            else {
                if (!startButton.classList.contains("disabled")) {
                    startButton.classList.add("disabled");
                }
            }
        }
    }
    catch {
        console.error('Could not update the start button limitation.');
    }
}
function renderPlannedTournamentUI(data: any) {
    const center = document.getElementById("center")!;
    if (center.classList.contains("ongoing") ||
        center.classList.contains("finished") ||
        center.classList.contains("cancelled")) {
        center.innerHTML = 'Loading...';
        center.classList.remove("ongoing");
        center.classList.remove("finished");
        center.classList.remove("cancelled");
    }
    if (!center.classList.contains("planned")) {
        center.classList.add("planned");
    }
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
    checkNumOfPlayersToStart(Object.keys(data.registeredPlayers).length);
}
async function updateTournamentUI(status: string, data: any) {
    const center = document.getElementById("center")!;
    center.innerHTML = "";
    const titleElement = document.getElementById("tournamentTitle");
    if (titleElement && data.name) {
        titleElement!.firstChild!.textContent = data.name + " ";
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
async function renderOngoingTournamentUI(data: any) {
    const center = document.getElementById("center")!;
    if (center.classList.contains("planned") ||
        center.classList.contains("finished") ||
        center.classList.contains("cancelled")) {
        center.innerHTML = '';
        center.classList.remove("planned");
        center.classList.remove("finished");
        center.classList.remove("cancelled");
    }
    if (!center.classList.contains("ongoing")) {
        center.classList.add("ongoing");
    }
    let title = document.getElementById("matchTitle");
    if (!title) {
        title = document.createElement("h3");
        title.id = "matchTitle";
        title.textContent = "Current Match";
        center.appendChild(title);
    }
    const res = await fetch(`/tournament/${tournamentId}/match/current`);
    if (await checkNoCurrentMatch(res) == false) {
        const match = await res.json();
        await drawBoard(match.id, match.board, match);
    }
    let cancelBtn = document.getElementById("cancelBtn");
    if (!cancelBtn) {
        center.innerHTML += '<br><button id="cancelBtn" class="btn btn-danger">Cancel Tournament</button>';
        cancelBtn = document.getElementById("cancelBtn")!;
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
}
async function checkNoCurrentMatch(res: any) {
    try {
        res = res ?? await fetch(`/tournament/${tournamentId}/match/current`);
        if (res.status != 200) {
            const center = document.getElementById("center")!;
            if (center.classList.contains("ongoing")) {
                let container = document.getElementById("boardContainer");
                if (!container) {
                    container = document.createElement("div");
                    container.id = "boardContainer";
                    const center = document.getElementById("center")!;
                    center.appendChild(container);
                }
                container.innerHTML = `
                    <div class="d-flex flex-column align-items-center justify-content-center" style="min-height: 200px;">
                        <div class="display-1 text-muted">🕒</div>
                        <div class="text-muted">Waiting for the next match...</div>
                    </div>
                `;
            }

            return true;
        }
        return false;
    }
    catch {
        console.log('Failed to retrieve current match.');
        return true;
    }
}
function renderFinishedTournamentUI(data: any) {
    const center = document.getElementById("center")!;
    if (center.classList.contains("planned") ||
        center.classList.contains("ongoing") ||
        center.classList.contains("cancelled")) {
        center.innerHTML = 'Loading...';
        center.classList.remove("planned");
        center.classList.remove("ongoing");
        center.classList.remove("cancelled");
    }
    if (!center.classList.contains("finished")) {
        center.classList.add("finished");
    }
    const sorted = Object.entries(data.leaderboard as Record<string, number>)
        .sort(([, scoreA], [, scoreB]) => (scoreB as number) - (scoreA as number));
    center.innerHTML = `
        <h3>🏁 Tournament Finished</h3>
        <div class="podium-container d-flex justify-content-center align-items-end mt-4">
            ${renderPodium(sorted, data.registeredPlayers)}
        </div>
    `;
}

function renderCancelledTournamentUI() {
    const center = document.getElementById("center")!;
    if (center.classList.contains("planned") ||
        center.classList.contains("ongoing") ||
        center.classList.contains("finished")) {
        center.innerHTML = 'Loading...';
        center.classList.remove("planned");
        center.classList.remove("ongoing");
        center.classList.remove("finished");
    }
    if (!center.classList.contains("cancelled")) {
        center.classList.add("cancelled");
    }
    center.innerHTML = "<h3 class='text-danger'>Tournament Cancelled</h3>";
}
function subscribeToTournament() {
    if (hub?.connectionStatus == 'Connected') {
        try {
            hub.subscribeToTournament(tournamentId);
        }
        catch { }
    }
    else {
        setTimeout(subscribeToTournament, 500);
    }
}
// === ACTIONS ===
async function renameTournament(): Promise<void> {
    if (!tournamentId)
        return;
    const { value: newName } = await Swal.fire({
        title: 'Rename Tournament',
        input: 'text',
        inputLabel: 'New tournament name:',
        inputPlaceholder: 'Enter new name',
        showCancelButton: true,
        inputValidator: (value: string) => {
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
            titleElement!.firstChild!.textContent = newName + " ";
            flashElement(titleElement);
        }
        document.title = newName + " - Tournament";
    }
    catch (error) {
        console.error(error);
        await Swal.fire('Error', (error as Error).message || "Failed to rename tournament.", 'error');
    }
}
(window as any).renameTournament = renameTournament;
async function renamePlayer(playerId: string): Promise<void> {
    if (!tournamentId)
        return;
    const { value: newName } = await Swal.fire({
        title: 'Rename Player',
        input: 'text',
        inputLabel: 'New player name:',
        inputPlaceholder: 'Enter new name',
        showCancelButton: true,
        inputValidator: (value: string) => {
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
        await Swal.fire('Error', (error as Error).message || "Failed to rename player.", 'error');
    }
}
(window as any).renamePlayer = renamePlayer;
export async function fetchMatch(tournamentId: string, matchId: string): Promise<MatchDto | null> {
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
(window as any).fetchMatch = fetchMatch;
// === HUB EVENTS ===
hub.onPlayerRegistered(async () => {
    console.log("Player registered → Reloading players and tournament UI.");
    await loadPlayers();
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await Promise.all([
            updateTournamentUI(data.status, data),
            checkNumOfPlayersToStart(data.registeredPlayers.length)
        ]);
    }
    await loadLeaderboard();
});
hub.onRefreshLeaderboard(() => loadLeaderboard());
hub.onTournamentStarted(async () => {
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
});
hub.onTournamentUpdated(async () => {
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
});
hub.onTournamentCancelled(async () => { 
    const res = await fetch(`/tournament/${tournamentId}`);
    if (res.ok) {
        const data = await res.json();
        await updateTournamentUI(data.status, data);
    }
});
hub.onMatchStarted(async (matchId: string) => {
    await fetchMatch(String(tournamentId), matchId);
});
hub.onMatchEnded(async (matchId: string) => {
    await Promise.all([
        fetchMatch(String(tournamentId), matchId),
        checkNoCurrentMatch(null)
    ]);
});
hub.onReceiveBoard(async (matchId: string, board: string[][]) => {
    await Promise.all([
        drawBoard(matchId, board, null),
        updateMatchBoard(matchId, board)
    ]);
});
hub.onTournamentUpdated(async () => {
    console.log("Tournament updated → reloading leaderboard and matches...");
    await Promise.all([
        loadLeaderboard(),
        loadMatches(),
        checkNoCurrentMatch(null),
        loadPlayers()
    ]);
});
hub.onPlayerRegistered(async () => {
    console.log("Player registered → reloading players...");
    await loadPlayers();
});
// === DOM EVENTS ===
document.getElementById("toggleMatchesBtn")?.addEventListener('click', () => { toggleMatchesPanel(tournamentId); });
document.getElementById("reloadPlayersBtn")?.addEventListener("click", loadPlayers);
document.getElementById("reloadLeaderboardBtn")?.addEventListener("click", loadLeaderboard);
document.getElementById("reloadMatchesBtn")?.addEventListener("click", loadMatches);
document.getElementById("renameTournamentBtn")?.addEventListener("click", renameTournament);
document.querySelectorAll(".renamePlayerBtn").forEach(button => {
    button.addEventListener("click", (e) => {
        const playerId = (e.currentTarget as HTMLElement).getAttribute("data-player-id");
        if (playerId) {
            renamePlayer(playerId);
        }
    });
});
// === INITIALIZATION ===
document.addEventListener("DOMContentLoaded", async () => {
    await loadTournamentDetails(tournamentId);
    subscribeToTournament();
});
