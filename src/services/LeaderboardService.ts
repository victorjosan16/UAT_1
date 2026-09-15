import { collection, doc, getDocs, limit, orderBy, query, runTransaction } from "firebase/firestore";
import { apiClient, type ApiClient } from "./ApiClient";
import { ensureSignedIn, getFirestoreDb } from "./firebase";
import { utcDateKey } from "@/utils/dailySeed";
import { isoWeekKey } from "@/utils/isoWeek";
import type { RunSummary } from "@/types";

const LEADERBOARD_FETCH_LIMIT = 50;
/** Generous sanity caps — this is client-direct with no server validation, so rules (and this) only block wildly malformed values, not real cheating. */
const MAX_PLAUSIBLE_SCORE = 10_000_000;
const MAX_PLAUSIBLE_HEIGHT = 100_000;

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

function leaderboardCollectionSegments(window: LeaderboardWindow): [string, ...string[]] {
  if (window === "daily") return ["leaderboard_daily", utcDateKey(), "entries"];
  if (window === "weekly") return ["leaderboard_weekly", isoWeekKey(), "entries"];
  return ["leaderboard_alltime"];
}

function leaderboardDocSegments(window: LeaderboardWindow, playerId: string): [string, ...string[]] {
  return [...leaderboardCollectionSegments(window), playerId];
}

export class LeaderboardService {
  constructor(private readonly api: ApiClient = apiClient) {}

  /**
   * Reads directly from Firestore — there's no Cloud Functions API behind
   * `/leaderboard/*` deployed (that needs the Blaze plan), so this bypasses
   * it entirely rather than failing every time. See firestore.rules for the
   * only validation these writes get.
   */
  async fetchLeaderboard(window: LeaderboardWindow): Promise<LeaderboardResponse> {
    const db = getFirestoreDb();
    const colRef = collection(db, ...leaderboardCollectionSegments(window));
    const snap = await getDocs(query(colRef, orderBy("score", "desc"), limit(LEADERBOARD_FETCH_LIMIT)));
    const entries: LeaderboardEntry[] = snap.docs.map((docSnap, index) => {
      const data = docSnap.data() as { nickname?: string; score?: number; height?: number };
      return { rank: index + 1, playerId: docSnap.id, nickname: data.nickname ?? "PLAYER", score: data.score ?? 0, height: data.height ?? 0 };
    });
    return { window, entries, you: null };
  }

  /**
   * Writes this run's score directly to each leaderboard window (one doc
   * per player per window — mirrors the unused server-side schema exactly),
   * but only if it beats what's already stored, so each window only ever
   * holds personal bests.
   */
  async submitScore(playerId: string, nickname: string, score: number, height: number): Promise<void> {
    if (!Number.isFinite(score) || !Number.isFinite(height)) return;
    const boundedScore = Math.max(0, Math.min(MAX_PLAUSIBLE_SCORE, Math.round(score)));
    const boundedHeight = Math.max(0, Math.min(MAX_PLAUSIBLE_HEIGHT, Math.round(height)));

    await ensureSignedIn().catch(() => undefined);
    const db = getFirestoreDb();
    const windows: LeaderboardWindow[] = ["all-time", "daily", "weekly"];

    await Promise.all(
      windows.map((window) =>
        runTransaction(db, async (tx) => {
          const ref = doc(db, ...leaderboardDocSegments(window, playerId));
          const snap = await tx.get(ref);
          const existing = snap.data() as { score?: number } | undefined;
          if (existing?.score !== undefined && existing.score >= boundedScore) return;
          tx.set(ref, { nickname, score: boundedScore, height: boundedHeight, updatedAt: Date.now() });
        }).catch((error: unknown) => console.error(`Leaderboard submit (${window}) failed:`, error)),
      ),
    );
  }

  startSession(mode: string, seed?: string): Promise<SessionStartResponse> {
    return this.api.post<SessionStartResponse>("/session/start", { mode, seed });
  }

  completeSession(sessionId: string, summary: RunSummary): Promise<SessionCompleteResponse> {
    return this.api.post<SessionCompleteResponse>("/session/complete", { sessionId, summary });
  }
}

export const leaderboardService = new LeaderboardService();
