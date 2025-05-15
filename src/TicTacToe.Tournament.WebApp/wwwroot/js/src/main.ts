// main.ts
import { TournamentHubClient } from "./tournament.hub.js";
import { MatchDto, LeaderboardEntryDto, PlayerDto } from "./models.js";
import { startSpinner, stopSpinner, safeFetchJson, flashElement } from "./helpers.js";
import { renderMatches, renderLeaderboard, renderPlayers } from "./renderers.js";

const hub = new TournamentHubClient();

hub.onAny((event: any, data: any) => {
    console.log(`[GlobalHub] ${event}`, data);
});

(window as any).tournamentHub = hub;

export const connectionReady = hub
    .start()
    .then(() => console.log("SignalR connected."))
    .catch(err => {
        console.error("SignalR connection failed:", err);
        throw err;
    });

// === LOADERS ===

const tournamentId = (document.getElementById("tournament") as HTMLElement)?.dataset?.tournamentId;

export async function loadMatches() {
    if (!tournamentId) return;
    startSpinner("reloadMatchesBtn");
    try {
        const matches = await safeFetchJson<MatchDto[]>(`/tournament/${tournamentId}/matches`);
        renderMatches(matches);
    } catch (error) {
        console.error(error);
        Swal.fire('Error', (error as Error).message || "Failed to load matches.", 'error');
    } finally {
        stopSpinner("reloadMatchesBtn");
    }
}

export async function loadLeaderboard() {
    if (!tournamentId) return;
    startSpinner("reloadLeaderboardBtn");
    try {
        const players = await safeFetchJson<LeaderboardEntryDto[]>(`/tournament/${tournamentId}/leaderboard`);
        renderLeaderboard(players);
    } catch (error) {
        console.error(error);
        Swal.fire('Error', (error as Error).message || "Failed to load leaderboard.", 'error');
    } finally {
        stopSpinner("reloadLeaderboardBtn");
    }
}

export async function loadPlayers() {
    if (!tournamentId) return;
    startSpinner("reloadPlayersBtn");
    try {
        const players = await safeFetchJson<PlayerDto[]>(`/tournament/${tournamentId}/players`);
        renderPlayers(players);
    } catch (error) {
        console.error(error);
        Swal.fire('Error', (error as Error).message || "Failed to load players.", 'error');
    } finally {
        stopSpinner("reloadPlayersBtn");
    }
}
