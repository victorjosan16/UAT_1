import { collection, doc, getDoc, limit as fbLimit, onSnapshot, orderBy, query, setDoc, type Unsubscribe } from "firebase/firestore";
import { getFirestoreDb } from "./firebase";
import { utcDateKey } from "@/utils/dailySeed";
import { isoWeekKey } from "@/utils/isoWeek";

export type LeaderboardScope = "allTime" | "daily" | "weekly";

export interface LeaderboardEntry {
  playerId: string;
  nickname: string;
  score: number;
  knowledgeIQ: number;
  updatedAt: number;
}

export interface SubmitScoreInput {
  playerId: string;
  nickname: string;
  score: number;
  knowledgeIQ: number;
}

const TOP_N = 20;

function collectionPathFor(scope: LeaderboardScope): string {
  if (scope === "daily") return `leaderboard_daily/${utcDateKey()}/entries`;
  if (scope === "weekly") return `leaderboard_weekly/${isoWeekKey()}/entries`;
  return "leaderboard_alltime";
}

/**
 * Leaderboard reads/writes go straight to Firestore from the client (no
 * Cloud Functions deployment — see firestore.rules for what little
 * server-side validation exists instead). Real players and real scores
 * only: nothing here ever fabricates an entry.
 */
export const LeaderboardService = {
  /**
   * Writes (or updates) this player's entry in every scope, but only when
   * the new score beats whatever is already there for that scope/window —
   * a leaderboard should show a player's BEST run, not their latest one.
   */
  async submitScore(input: SubmitScoreInput): Promise<void> {
    const scopes: LeaderboardScope[] = ["allTime", "daily", "weekly"];
    await Promise.all(scopes.map((scope) => submitToScope(scope, input)));
  },

  /** Live top-N for a scope (real-time via onSnapshot — no polling). Returns an unsubscribe function; callers must call it on unmount. */
  subscribe(scope: LeaderboardScope, onEntries: (entries: LeaderboardEntry[]) => void): Unsubscribe {
    const db = getFirestoreDb();
    const q = query(collection(db, collectionPathFor(scope)), orderBy("score", "desc"), fbLimit(TOP_N));
    return onSnapshot(
      q,
      (snapshot) => {
        const entries = snapshot.docs.map((docSnap) => ({ playerId: docSnap.id, ...(docSnap.data() as Omit<LeaderboardEntry, "playerId">) }));
        onEntries(entries);
      },
      () => {
        // Offline, blocked, or rules-denied — the ranking screen just keeps showing local stats instead.
        onEntries([]);
      },
    );
  },
};

async function submitToScope(scope: LeaderboardScope, input: SubmitScoreInput): Promise<void> {
  try {
    const db = getFirestoreDb();
    const ref = doc(db, collectionPathFor(scope), input.playerId);
    const existing = await getDoc(ref);
    const existingScore = existing.exists() ? (existing.data().score as number) : -1;
    if (input.score <= existingScore) return;

    await setDoc(ref, {
      nickname: input.nickname,
      score: input.score,
      knowledgeIQ: input.knowledgeIQ,
      updatedAt: Date.now(),
    });
  } catch {
    // Offline, blocked, or rules-denied — the run's own local bests already recorded it; the leaderboard just misses this update.
  }
}
