const hub = window.tournamentHub;
async function loadTournaments() {
    try {
        const response = await fetch("/tournament/list");
        const data = await response.json();
        const tableBody = document.getElementById("tournamentBody");
        tableBody.innerHTML = "";
        for (const t of data) {
            const row = document.createElement("tr");
            var color = 'bg-secondary';
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
            `;
            tableBody.appendChild(row);
        }
    }
    catch (err) {
        const tableBody = document.getElementById("tournamentBody");
        tableBody.innerHTML = "";
        const row = document.createElement("tr");
        row.innerHTML = `<td colspan="4">❌ Failed to load tournaments.</td>`;
        tableBody.appendChild(row);
        console.error(err);
    }
}
async function createTournament() {
    const res = await fetch("/tournament/new", { method: "POST" });
    const result = await res.json();
    console.log(`Tournament created: ${result.id}`);
    window.location.href = '/tournament/view/' + result.id;
}
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
    console.log(`${event}`, data);
});
document.getElementById("createTournamentBtn")?.addEventListener("click", createTournament);
loadTournaments();
export {};
//# sourceMappingURL=tournamentIndex.js.map