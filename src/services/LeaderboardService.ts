import { collection, doc, getCountFromServer, getDoc, getDocs, limit as fbLimit, onSnapshot, orderBy, query, setDoc, where, type Unsubscribe } from "firebase/firestore";
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

export interface MyRank {
  rank: number;
  score: number;
}

export interface AroundMeEntry extends LeaderboardEntry {
  rank: number;
  isMe: boolean;
}

export interface MilestoneCutoffs {
  top10: number | null;
  top50: number | null;
  top100: number | null;
}

export interface MilestoneDistance {
  milestoneRank: 10 | 50 | 100;
  placesAway: number;
}

const TOP_N = 20;
const AROUND_ME_RADIUS = 3;
const MILESTONE_SAMPLE_SIZE = 100;

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

  /** One-off (not live) — only meaningful to compute when the player's own row isn't already visible in the top N. Returns null if they haven't scored in this scope yet. */
  async getMyRank(scope: LeaderboardScope, playerId: string): Promise<MyRank | null> {
    try {
      const db = getFirestoreDb();
      const path = collectionPathFor(scope);
      const mine = await getDoc(doc(db, path, playerId));
      if (!mine.exists()) return null;
      const score = mine.data().score as number;

      const higherCountQuery = query(collection(db, path), where("score", ">", score));
      const countSnapshot = await getCountFromServer(higherCountQuery);
      return { rank: countSnapshot.data().count + 1, score };
    } catch {
      return null;
    }
  },

  /**
   * A handful of real neighbors just above and below this player — never
   * the whole table. A player ranked #18,420 gets something meaningful to
   * look at instead of a meaningless number (see MASTER PROMPT §14).
   * Returns null if the player hasn't scored in this scope yet.
   */
  async getAroundMe(scope: LeaderboardScope, playerId: string, radius: number = AROUND_ME_RADIUS): Promise<AroundMeEntry[] | null> {
    try {
      const db = getFirestoreDb();
      const path = collectionPathFor(scope);
      const mineSnap = await getDoc(doc(db, path, playerId));
      if (!mineSnap.exists()) return null;
      const mine = mineSnap.data() as Omit<LeaderboardEntry, "playerId">;

      const higherCountQuery = query(collection(db, path), where("score", ">", mine.score));
      const aboveQuery = query(collection(db, path), where("score", ">", mine.score), orderBy("score", "asc"), fbLimit(radius));
      const belowQuery = query(collection(db, path), where("score", "<", mine.score), orderBy("score", "desc"), fbLimit(radius));

      const [countSnapshot, aboveSnap, belowSnap] = await Promise.all([getCountFromServer(higherCountQuery), getDocs(aboveQuery), getDocs(belowQuery)]);
      const myRank = countSnapshot.data().count + 1;

      const aboveDescending = [...aboveSnap.docs].reverse();
      const above: AroundMeEntry[] = aboveDescending.map((docSnap, i) => ({
        playerId: docSnap.id,
        ...(docSnap.data() as Omit<LeaderboardEntry, "playerId">),
        rank: myRank - (aboveDescending.length - i),
        isMe: false,
      }));

      const below: AroundMeEntry[] = belowSnap.docs.map((docSnap, i) => ({
        playerId: docSnap.id,
        ...(docSnap.data() as Omit<LeaderboardEntry, "playerId">),
        rank: myRank + 1 + i,
        isMe: false,
      }));

      const meEntry: AroundMeEntry = { playerId, ...mine, rank: myRank, isMe: true };
      return [...above, meEntry, ...below];
    } catch {
      return null;
    }
  },

  /**
   * The score sitting at rank 10/50/100, from a single top-100 read — only
   * populated when that many real entries actually exist (see MASTER
   * PROMPT §47/48: never fabricate a milestone that isn't backed by real
   * players).
   */
  async getMilestoneCutoffs(scope: LeaderboardScope): Promise<MilestoneCutoffs> {
    try {
      const db = getFirestoreDb();
      const q = query(collection(db, collectionPathFor(scope)), orderBy("score", "desc"), fbLimit(MILESTONE_SAMPLE_SIZE));
      const snap = await getDocs(q);
      const scores = snap.docs.map((d) => d.data().score as number);
      return {
        top10: scores.length >= 10 ? (scores[9] ?? null) : null,
        top50: scores.length >= 50 ? (scores[49] ?? null) : null,
        top100: scores.length >= 100 ? (scores[99] ?? null) : null,
      };
    } catch {
      return { top10: null, top50: null, top100: null };
    }
  },
};

/**
 * Pure — how many places away from the nearest not-yet-reached milestone.
 * Only ever answers using a milestone the cutoffs prove is real (see
 * getMilestoneCutoffs); returns null once no further real milestone
 * exists to chase (already Top 10, or not enough players yet for any).
 */
export function distanceToNextMilestone(myRank: number, cutoffs: MilestoneCutoffs): MilestoneDistance | null {
  // Checked from the least exclusive milestone to the most exclusive — a rank inside Top 100 but
  // outside Top 50 should target Top 50 next, not jump straight to "distance from Top 10".
  if (cutoffs.top100 !== null && myRank > 100) return { milestoneRank: 100, placesAway: myRank - 100 };
  if (cutoffs.top50 !== null && myRank > 50) return { milestoneRank: 50, placesAway: myRank - 50 };
  if (cutoffs.top10 !== null && myRank > 10) return { milestoneRank: 10, placesAway: myRank - 10 };
  return null;
}

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
