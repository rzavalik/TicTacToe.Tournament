declare const signalR: any;

declare global {
    interface Window {
        SIGNALR_HUB_URL: string;
    }
}

type Guid = string;
type Board = number[][];
type AnyHandler = (...args: any[]) => void;

export class TournamentHubClient {
    private connection: any;
    private globalListeners: ((event: string, data: any) => void)[] = [];
    public connectionStatus: string = 'Disconnected';

    private handlers: { [event: string]: AnyHandler | null } = {
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

    constructor() {
        const endpoint = window.SIGNALR_HUB_URL || "https://default-server-url";

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(endpoint)
            .withAutomaticReconnect()
            .build();

        this.connection.onclose(() => {
            this.connectionStatus = 'Disconnected';
            console.warn("SignalR disconnected.")
        });
        this.connection.onreconnected(() => {
            this.connectionStatus = 'Connected';
            console.warn("SignalR reconnected.")
        });
        this.connection.onreconnecting(() => {
            this.connectionStatus = 'Connecting';
            console.warn("SignalR Reconnecting...")
        });

        Object.keys(this.handlers).forEach(event => {
            this.connection.on(event, (...args: any[]) => {
                console.log(`🔹 ${event}`, ...args);
                this.handlers[event]?.(...args);
                this.notifyGlobal(event, args);
            });
        });
    }

    public async start(): Promise<void> {
        try {
            await this.connection.start();
            console.log("✅ TournamentHub connected.");
        } catch (err) {
            console.error("❌ Error connecting TournamentHub:", err);
        }
    }

    public onAny(handler: (event: string, data: any) => void): void {
        this.globalListeners.push(handler);
    }

    private notifyGlobal(event: string, data: any): void {
        for (const listener of this.globalListeners) {
            listener(event, data);
        }

        const evt = new CustomEvent("tournament-hub-event", {
            detail: { event, data }
        });
        window.dispatchEvent(evt);
    }

    public async subscribeToTournament(tournamentId: string): Promise<void> {
        try {
            await this.connection.invoke("SpectateTournament", tournamentId);
            console.log(`✅ Subscribed to tournament ${tournamentId}`);
        } catch (err) {
            console.error("❌ Failed to subscribe to tournament:", err);
        }
    }

    onRegistered(handler: (...args: any[]) => void): void {
        this.handlers.OnRegistered = handler;
    }

    onPlayerRegistered(handler: (...args: any[]) => void): void {
        this.handlers.OnPlayerRegistered = handler;
    }

    onTournamentCreated(handler: (...args: any[]) => void): void {
        this.handlers.OnTournamentCreated = handler;
    }

    onTournamentUpdated(handler: (...args: any[]) => void): void {
        this.handlers.OnTournamentUpdated = handler;
    }

    onTournamentCancelled(handler: (...args: any[]) => void): void {
        this.handlers.OnTournamentCancelled = handler;
    }

    onTournamentStarted(handler: (...args: any[]) => void): void {
        this.handlers.OnTournamentStarted = handler;
    }

    onMatchStarted(handler: (...args: any[]) => void): void {
        this.handlers.OnMatchStarted = handler;
    }

    onMatchEnded(handler: (...args: any[]) => void): void {
        this.handlers.OnMatchEnded = handler;
    }

    onOpponentMoved(handler: (...args: any[]) => void): void {
        this.handlers.OnOpponentMoved = handler;
    }

    onYourTurn(handler: (...args: any[]) => void): void {
        this.handlers.OnYourTurn = handler;
    }

    onReceiveBoard(handler: (...args: any[]) => void): void {
        this.handlers.OnReceiveBoard = handler;
    }

    onRefreshLeaderboard(handler: (...args: any[]) => void): void {
        this.handlers.OnRefreshLeaderboard = handler;
    }
}
