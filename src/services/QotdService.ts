import { doc, getDoc, increment, setDoc } from "firebase/firestore";
import { getFirestoreDb } from "./firebase";

export interface QotdStats {
  correctCount: number;
  totalCount: number;
}

/** Never shown below this — a "31% answered correctly" from 2 people isn't a real statistic (see MASTER PROMPT §47/48). */
const MIN_SAMPLE_SIZE = 10;

function statsRef(dateKey: string) {
  return doc(getFirestoreDb(), "qotd_stats", dateKey);
}

/**
 * One shared counter per day, aggregating every player's answer — real
 * data only, atomically incremented so concurrent answers from many
 * players can never race or clobber each other (unlike the leaderboard's
 * read-then-write pattern, this is a pure counter with no "best" to
 * preserve).
 */
export const QotdService = {
  async submitAnswer(dateKey: string, correct: boolean): Promise<void> {
    try {
      await setDoc(
        statsRef(dateKey),
        { correctCount: increment(correct ? 1 : 0), totalCount: increment(1) },
        { merge: true },
      );
    } catch {
      // Offline or blocked — the player still sees their own correct/wrong result locally.
    }
  },

  /** Null if there isn't yet a real enough sample to quote a percentage from. */
  async getCorrectPercent(dateKey: string): Promise<number | null> {
    try {
      const snap = await getDoc(statsRef(dateKey));
      if (!snap.exists()) return null;
      const data = snap.data() as QotdStats;
      if (data.totalCount < MIN_SAMPLE_SIZE) return null;
      return Math.round((data.correctCount / data.totalCount) * 100);
    } catch {
      return null;
    }
  },
};
