// tournamentIndex.ts
import { startSpinner, stopSpinner, safeFetchJson } from "./helpers.js";
const hub = window.tournamentHub;
// === LOADERS ===
async function loadTournaments() {
    startSpinner("reloadTournamentsBtn");
    try {
        const tournaments = await safeFetchJson("/tournament/list");
        renderTournaments(tournaments);
    }
    catch (error) {
        console.error(error);
        Swal.fire('Error', error.message || "Failed to load tournaments.", 'error');
    }
    finally {
        stopSpinner("reloadTournamentsBtn");
    }
}
function renderTournaments(tournaments) {
    const tableBody = document.getElementById("tournamentBody");
    tableBody.innerHTML = "";
    tournaments.forEach(t => {
        const row = document.createElement("tr");
        let color = "bg-secondary";
        if (t.status === 'Planned') {
            color = 'bg-info text-dark';
        }
        else if (t.status === 'Ongoing') {
            color = 'bg-success';
        }
        else if (t.status === 'Cancelled') {
            color = 'bg-danger';
        }
        row.innerHTML = `
            <td><a href="/tournament/view/${t.id}">${t.name}</a></td>
            <td><span class="badge ${color}">${t.status}</span></td>
            <td>${t.registeredPlayersCount}</td>
            <td>${t.matchCount}</td>
            <td>${renderTournamentActions(t)}</td>
        `;
        tableBody.appendChild(row);
    });
}
function renderTournamentActions(tournament) {
    let buttons = '';
    if (tournament.status === "Planned") {
        buttons += `
            <button class="btn btn-sm btn-success me-1" onclick="startTournament('${tournament.id}')">
                <i class="fas fa-play"></i> Start
            </button>`;
        buttons += `
            <button class="btn btn-sm btn-warning me-1" onclick="cancelTournament('${tournament.id}')">
                <i class="fas fa-ban"></i> Cancel
            </button>`;
    }
    if (tournament.status === "Finished" || tournament.status === "Cancelled") {
        buttons += `
            <button class="btn btn-sm btn-danger me-1" onclick="deleteTournament('${tournament.id}')">
                <i class="fas fa-trash"></i> Delete
            </button>`;
    }
    buttons += `
        <button class="btn btn-sm btn-primary" onclick="viewTournament('${tournament.id}')">
            <i class="fas fa-eye"></i> View
        </button>`;
    return buttons;
}
// === ACTIONS ===
async function viewTournament(tournamentId) {
    window.location.href = `/tournament/view/${tournamentId}`;
}
window.viewTournament = viewTournament;
async function createTournament() {
    window.location.href = "/tournament/create";
}
window.createTournament = createTournament;
async function deleteTournament(tournamentId) {
    const result = await Swal.fire({
        title: 'Delete Tournament',
        text: 'Are you sure you want to delete this tournament? This action cannot be undone!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    });
    if (!result.isConfirmed)
        return;
    try {
        await safeFetchJson(`/tournament/${tournamentId}`, { method: "DELETE" });
        await Swal.fire('Deleted!', 'Tournament has been deleted.', 'success');
        await loadTournaments();
    }
    catch (error) {
        console.error(error);
        Swal.fire('Error', error.message || "Failed to delete tournament.", 'error');
    }
}
window.deleteTournament = deleteTournament;
async function cancelTournament(tournamentId) {
    const result = await Swal.fire({
        title: 'Cancel Tournament',
        text: 'Are you sure you want to cancel this tournament?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ffc107',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, cancel it!',
        cancelButtonText: 'Back'
    });
    if (!result.isConfirmed)
        return;
    try {
        await safeFetchJson(`/tournament/${tournamentId}/cancel`, { method: "POST" });
        await Swal.fire('Cancelled!', 'Tournament has been cancelled.', 'success');
        await loadTournaments();
    }
    catch (error) {
        console.error(error);
        Swal.fire('Error', error.message || "Failed to cancel tournament.", 'error');
    }
}
window.cancelTournament = cancelTournament;
async function startTournament(tournamentId) {
    const result = await Swal.fire({
        title: 'Start Tournament',
        text: 'Are you sure you want to start this tournament?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, start!',
        cancelButtonText: 'Cancel'
    });
    if (!result.isConfirmed)
        return;
    try {
        await safeFetchJson(`/tournament/${tournamentId}/start`, { method: "POST" });
        await Swal.fire('Started!', 'Tournament has been started.', 'success');
        await loadTournaments();
    }
    catch (error) {
        console.error(error);
        Swal.fire('Error', error.message || "Failed to start tournament.", 'error');
    }
}
window.startTournament = startTournament;
// === HUB LISTENERS ===
hub.onTournamentCreated(() => {
    console.log("Tournament created → Reloading list...");
    loadTournaments();
});
hub.onTournamentUpdated(() => {
    console.log("Tournament updated → Reloading list...");
    loadTournaments();
});
hub.onTournamentCancelled(() => {
    console.log("Tournament cancelled → Reloading list...");
    loadTournaments();
});
hub.onAny((event, data) => {
    console.log(`[Hub Event] ${event}`, data);
});
// === DOM EVENTS ===
document.getElementById("createTournamentBtn")?.addEventListener("click", createTournament);
// === INITIALIZATION ===
loadTournaments();
