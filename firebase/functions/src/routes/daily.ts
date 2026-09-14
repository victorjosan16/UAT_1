import { Router } from "express";
import { db, Timestamp } from "../lib/firestore";
import { requireAuth, type AuthedRequest } from "../lib/auth";
import { parseBody } from "../lib/respond";
import { SessionIdBodySchema } from "../validation/schemas";
import { GAME_VERSION, dailySeed, utcDateKey } from "../lib/gameRules";

export const dailyRouter = Router();

interface DailyPlayerStats {
  bestFloor: number;
  bestScore: number;
  attemptCount: number;
  perfectCount: number;
}

dailyRouter.get("/api/daily", requireAuth, async (req: AuthedRequest, res) => {
  const uid = req.uid as string;
  const dateKey = utcDateKey();
  const seed = dailySeed(dateKey, GAME_VERSION);

  await db.collection("daily_meta").doc(dateKey).set({ seed, dateKey }, { merge: true });
  const playersRef = db.collection("daily_meta").doc(dateKey).collection("players");

  const [mineSnap, topSnap, playerSnap] = await Promise.all([
    playersRef.doc(uid).get(),
    playersRef.orderBy("bestFloor", "desc").limit(1).get(),
    db.collection("players").doc(uid).get(),
  ]);

  const mine = mineSnap.data() as DailyPlayerStats | undefined;
  const top = topSnap.docs[0]?.data() as DailyPlayerStats | undefined;

  let yourRank: number | null = null;
  if (mine) {
    const aheadSnap = await playersRef.where("bestFloor", ">", mine.bestFloor).count().get();
    yourRank = aheadSnap.data().count + 1;
  }

  res.json({
    dateKey,
    seed,
    todaysBestFloor: top?.bestFloor ?? 0,
    yourBestFloor: mine?.bestFloor ?? 0,
    yourBestScore: mine?.bestScore ?? 0,
    yourRank,
    attemptCount: mine?.attemptCount ?? 0,
    streak: (playerSnap.data()?.dailyStreak as number | undefined) ?? 0,
  });
});

dailyRouter.post("/api/daily/score", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(SessionIdBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;

  const sessionSnap = await db.collection("sessions").doc(body.sessionId).get();
  if (!sessionSnap.exists) {
    res.status(404).json({ error: "Session not found", code: "SESSION_NOT_FOUND" });
    return;
  }
  const session = sessionSnap.data() as {
    playerId: string;
    mode: string;
    status: string;
    validationStatus?: string;
    validatedScore?: number;
    validatedHeight?: number;
    validatedPerfectCount?: number;
  };

  if (session.playerId !== uid) {
    res.status(403).json({ error: "Session belongs to a different player", code: "FORBIDDEN" });
    return;
  }
  if (session.mode !== "DAILY" || session.status !== "completed" || session.validationStatus !== "accepted") {
    res.status(422).json({ error: "Session is not a validated Daily Tower run", code: "INVALID_SESSION" });
    return;
  }

  const dateKey = utcDateKey();
  const playerRef = db.collection("players").doc(uid);
  const dailyPlayerRef = db.collection("daily_meta").doc(dateKey).collection("players").doc(uid);

  const streak = await db.runTransaction(async (tx) => {
    const [dailySnap, playerSnap] = await Promise.all([tx.get(dailyPlayerRef), tx.get(playerRef)]);
    const existing = dailySnap.data() as DailyPlayerStats | undefined;
    const player = playerSnap.data() as { dailyStreak?: number; lastDailyDateKey?: string } | undefined;

    tx.set(
      dailyPlayerRef,
      {
        bestFloor: Math.max(existing?.bestFloor ?? 0, session.validatedHeight ?? 0),
        bestScore: Math.max(existing?.bestScore ?? 0, session.validatedScore ?? 0),
        perfectCount: Math.max(existing?.perfectCount ?? 0, session.validatedPerfectCount ?? 0),
        attemptCount: (existing?.attemptCount ?? 0) + 1,
        updatedAt: Timestamp.now(),
      },
      { merge: true },
    );

    let nextStreak = player?.dailyStreak ?? 0;
    if (player?.lastDailyDateKey !== dateKey) {
      const yesterday = utcDateKey(new Date(Date.now() - 86_400_000));
      nextStreak = player?.lastDailyDateKey === yesterday ? (player?.dailyStreak ?? 0) + 1 : 1;
      tx.set(playerRef, { dailyStreak: nextStreak, lastDailyDateKey: dateKey }, { merge: true });
    }
    return nextStreak;
  });

  const aheadSnap = await db
    .collection("daily_meta")
    .doc(dateKey)
    .collection("players")
    .where("bestFloor", ">", session.validatedHeight ?? 0)
    .count()
    .get();

  res.json({ accepted: true, rank: aheadSnap.data().count + 1, streak });
});
