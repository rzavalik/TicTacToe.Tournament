export class TournamentHubClient {
    constructor() {
        this.globalListeners = [];
        this.connectionStatus = 'Disconnected';
        this.handlers = {
            OnRegistered: null,
            OnPlayerRegistered: null,
            OnTournamentCreated: null,
            OnTournamentUpdated: null,
            OnTournamentCancelled: null,
            OnTournamentStarted: null,
            OnMatchStarted: null,
            OnMatchEnded: null,
            OnOpponentMoved: null,
            OnYourTurn: null,
            OnReceiveBoard: null,
            OnRefreshLeaderboard: null,
        };
        const endpoint = window.SIGNALR_HUB_URL || "https://default-server-url";
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(endpoint)
            .withAutomaticReconnect()
            .build();
        this.connection.onclose(() => {
            this.connectionStatus = 'Disconnected';
            console.warn("SignalR disconnected.");
        });
        this.connection.onreconnected(() => {
            this.connectionStatus = 'Connected';
            console.warn("SignalR reconnected.");
        });
        this.connection.onreconnecting(() => {
            this.connectionStatus = 'Connecting';
            console.warn("SignalR Reconnecting...");
        });
        Object.keys(this.handlers).forEach(event => {
            this.connection.on(event, (payload) => {
                console.log(`🔹 ${event}`, payload);
                this.handlers[event]?.(payload);
                this.notifyGlobal(event, payload);
            });
        });
    }
    async start() {
        try {
            await this.connection.start();
            console.log("✅ TournamentHub connected.");
        }
        catch (err) {
            console.error("❌ Error connecting TournamentHub:", err);
        }
    }
    onAny(handler) {
        this.globalListeners.push(handler);
    }
    notifyGlobal(event, data) {
        for (const listener of this.globalListeners) {
            listener(event, data);
        }
        const evt = new CustomEvent("tournament-hub-event", {
            detail: { event, data }
        });
        window.dispatchEvent(evt);
    }
    async subscribeToTournament(tournamentId) {
        try {
            await this.connection.invoke("SpectateTournament", tournamentId);
            console.log(`✅ Subscribed to tournament ${tournamentId}`);
        }
        catch (err) {
            console.error("❌ Failed to subscribe to tournament:", err);
        }
    }
    onRegistered(handler) {
        this.handlers.OnRegistered = handler;
    }
    onPlayerRegistered(handler) {
        this.handlers.OnPlayerRegistered = handler;
    }
    onTournamentCreated(handler) {
        this.handlers.OnTournamentCreated = handler;
    }
    onTournamentUpdated(handler) {
        this.handlers.OnTournamentUpdated = handler;
    }
    onTournamentCancelled(handler) {
        this.handlers.OnTournamentCancelled = handler;
    }
    onTournamentStarted(handler) {
        this.handlers.OnTournamentStarted = handler;
    }
    onMatchStarted(handler) {
        this.handlers.OnMatchStarted = handler;
    }
    onMatchEnded(handler) {
        this.handlers.OnMatchEnded = handler;
    }
    onOpponentMoved(handler) {
        this.handlers.OnOpponentMoved = handler;
    }
    onYourTurn(handler) {
        this.handlers.OnYourTurn = handler;
    }
    onReceiveBoard(handler) {
        this.handlers.OnReceiveBoard = handler;
    }
    onRefreshLeaderboard(handler) {
        this.handlers.OnRefreshLeaderboard = handler;
    }
}
//# sourceMappingURL=tournamentHubClient.js.map