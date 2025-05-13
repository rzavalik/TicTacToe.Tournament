// models.ts

// === TOURNAMENTS ===

export interface TournamentSummaryDto {
    id: string;
    name: string;
    registeredPlayersCount: number;
    matchCount: number;
    status: string;
}

export interface TournamentDetailsDto {
    id: string;
    name: string;
    status: string;
    registeredPlayers: Record<string, string>;
    leaderboard: Record<string, number>;
    startTime?: string;
    endTime?: string;
    duration?: string;
    matches: MatchDto[];
}

// === MATCHES ===

export const MatchStatus: Record<number, string> = {
    0: "Planned",
    1: "Ongoing",
    2: "Finished",
    3: "Cancelled"
};

export const SymbolMap: Record<string, string> = {
    "32": " ",
    "88": "X",
    "79": "O"
};

export interface MatchDto {
    id: string;
    playerAName: string;
    playerBName: string;
    playerAId: string;
    playerBId: string;
    board: string[][];
    status: string;
    duration: string | null;
}

export interface GameResultDto {
    matchId: string;
    winnerId: string | null;
    board: string[][];
    isDraw: boolean;
}

// === PLAYERS ===

export interface PlayerDto {
    id: string;
    name: string;
}

export interface LeaderboardEntryDto {
    name: string;
    score: number;
}
