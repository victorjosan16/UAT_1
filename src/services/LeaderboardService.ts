import { apiClient, type ApiClient } from "./ApiClient";
import type { RunSummary } from "@/types";

export type LeaderboardWindow = "daily" | "weekly" | "all-time";

export interface LeaderboardEntry {
  rank: number;
  playerId: string;
  nickname: string;
  score: number;
  height: number;
}

export interface LeaderboardResponse {
  window: LeaderboardWindow;
  entries: LeaderboardEntry[];
  you: LeaderboardEntry | null;
}

export interface SessionStartResponse {
  sessionId: string;
  seed: string;
  gameVersion: string;
  serverTimeMs: number;
}

export interface SessionCompleteResponse {
  accepted: boolean;
  validatedScore: number;
  rank: {
    daily: number | null;
    weekly: number | null;
    allTime: number | null;
    percentile: number | null;
  };
}

export class LeaderboardService {
  constructor(private readonly api: ApiClient = apiClient) {}

  fetchLeaderboard(window: LeaderboardWindow): Promise<LeaderboardResponse> {
    return this.api.get<LeaderboardResponse>(`/leaderboard/${window}`);
  }

  startSession(mode: string, seed?: string): Promise<SessionStartResponse> {
    return this.api.post<SessionStartResponse>("/session/start", { mode, seed });
  }

  completeSession(sessionId: string, summary: RunSummary): Promise<SessionCompleteResponse> {
    return this.api.post<SessionCompleteResponse>("/session/complete", { sessionId, summary });
  }
}

export const leaderboardService = new LeaderboardService();
