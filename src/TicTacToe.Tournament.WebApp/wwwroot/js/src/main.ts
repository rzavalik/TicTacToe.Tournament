import { TournamentHubClient } from './tournamentHubClient.js';

const hub = new TournamentHubClient();

hub.onAny((event, data) => {
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