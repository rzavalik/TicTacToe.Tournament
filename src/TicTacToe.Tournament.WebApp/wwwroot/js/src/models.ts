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
    leaderboard: LeaderboardEntryDto[];
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
    "88": `<i class="fa-solid fa-xmark" style="font-size:130%"></i>`,
    "79": `<i class="fa-regular fa-circle"></i>`
};

export interface MatchDto {
    id: string;
    playerAId: string;
    playerAName: string;
    playerAMark: string;
    playerBId: string;
    playerBName: string;
    playerBMark: string;
    winner: string;
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
    playerId: string;
    playerName: string;
    totalPoints: number;
    wins: number;
    draws: number;
    losses: number;
    walkovers: number;
}