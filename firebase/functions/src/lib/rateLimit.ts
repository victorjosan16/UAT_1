import { db, FieldValue, Timestamp } from "./firestore";

const WINDOW_MS = 60_000;

/**
 * Minimal Firestore-backed fixed-window rate limiter. Not perfectly
 * precise under heavy concurrency (a Firestore transaction serializes
 * per-document, which is exactly what we want here), but is enough to
 * blunt naive spam against session/score endpoints — the primary abuse
 * defense is App Check + server-side score re-validation, not this.
 */
export async function checkRateLimit(bucketId: string, maxPerMinute: number): Promise<boolean> {
  const ref = db.collection("rate_limits").doc(bucketId);
  const now = Timestamp.now();

  return db.runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const data = snap.data() as { windowStart?: Timestamp; count?: number } | undefined;

    const windowStart = data?.windowStart;
    const isSameWindow = windowStart && now.toMillis() - windowStart.toMillis() < WINDOW_MS;

    if (!isSameWindow) {
      tx.set(ref, { windowStart: now, count: 1 });
      return true;
    }

    const count = data?.count ?? 0;
    if (count >= maxPerMinute) return false;

    tx.set(ref, { count: FieldValue.increment(1) }, { merge: true });
    return true;
  });
}
