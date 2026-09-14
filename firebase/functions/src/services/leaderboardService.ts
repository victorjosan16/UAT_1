import { db, FieldValue, Timestamp } from "../lib/firestore";
import { isoWeekKey } from "../lib/date";
import { utcDateKey, type RunSummary } from "../lib/gameRules";

interface LeaderboardEntryDoc {
  nickname: string;
  score: number;
  height: number;
  updatedAt: Timestamp;
}

async function bumpPersonalBest(ref: FirebaseFirestore.DocumentReference, nickname: string, score: number, height: number): Promise<void> {
  await db.runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const existing = snap.data() as LeaderboardEntryDoc | undefined;
    if (existing && existing.score >= score) return; // only personal bests are stored — keeps every leaderboard collection at one doc per player
    tx.set(ref, { nickname, score, height, updatedAt: Timestamp.now() });
  });
}

/**
 * A validated run always updates the player's all-time, this-week, and
 * today "best score" entries (one doc per player per window — see
 * docs/DATABASE.md), plus rolling aggregate stats on their player doc.
 * Called only after `revalidateRun` has accepted the submission.
 */
export async function updateLeaderboardsAndStats(uid: string, nickname: string, summary: RunSummary): Promise<void> {
  const dateKey = utcDateKey();
  const weekKey = isoWeekKey();

  const playerRef = db.collection("players").doc(uid);

  await Promise.all([
    bumpPersonalBest(db.collection("leaderboard_alltime").doc(uid), nickname, summary.score, summary.height),
    bumpPersonalBest(db.collection("leaderboard_daily").doc(dateKey).collection("entries").doc(uid), nickname, summary.score, summary.height),
    bumpPersonalBest(db.collection("leaderboard_weekly").doc(weekKey).collection("entries").doc(uid), nickname, summary.score, summary.height),
    // totalTowersBuilt increment() and the two "best ever" max-of fields need different write
    // semantics, so the max-of fields go through their own transaction just below.
    playerRef.set({ totalTowersBuilt: FieldValue.increment(1) }, { merge: true }),
    db.runTransaction(async (tx) => {
      const snap = await tx.get(playerRef);
      const data = snap.data() as { bestPerfectStreak?: number; bestAverageAccuracy?: number } | undefined;
      tx.set(
        playerRef,
        {
          bestPerfectStreak: Math.max(data?.bestPerfectStreak ?? 0, summary.bestPerfectStreak),
          bestAverageAccuracy: Math.max(data?.bestAverageAccuracy ?? 0, summary.averageAccuracy),
        },
        { merge: true },
      );
    }),
  ]);
}

export interface RankResult {
  daily: number | null;
  weekly: number | null;
  allTime: number | null;
  percentile: number | null;
}

async function rankFor(collectionRef: FirebaseFirestore.CollectionReference, score: number): Promise<{ rank: number; total: number } | null> {
  const [aheadSnap, totalSnap] = await Promise.all([collectionRef.where("score", ">", score).count().get(), collectionRef.count().get()]);
  const ahead = aheadSnap.data().count;
  const total = totalSnap.data().count;
  if (total === 0) return null;
  return { rank: ahead + 1, total };
}

export async function computeRanks(score: number): Promise<RankResult> {
  const dateKey = utcDateKey();
  const weekKey = isoWeekKey();

  const [daily, weekly, allTime] = await Promise.all([
    rankFor(db.collection("leaderboard_daily").doc(dateKey).collection("entries"), score),
    rankFor(db.collection("leaderboard_weekly").doc(weekKey).collection("entries"), score),
    rankFor(db.collection("leaderboard_alltime"), score),
  ]);

  const percentile = allTime ? Math.max(0.1, (allTime.rank / allTime.total) * 100) : null;

  return {
    daily: daily?.rank ?? null,
    weekly: weekly?.rank ?? null,
    allTime: allTime?.rank ?? null,
    percentile,
  };
}
