import { Router } from "express";
import { db } from "../lib/firestore";
import { requireAuth, type AuthedRequest } from "../lib/auth";
import { parseBody } from "../lib/respond";
import { NicknameBodySchema } from "../validation/schemas";

export const playerRouter = Router();

/**
 * Upserts the caller's profile. There is no separate "playerId" in the
 * request — it's always the verified Firebase Auth uid, never something
 * the client gets to assert (see docs/SECURITY.md).
 */
playerRouter.post("/api/player", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(NicknameBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;

  await db
    .collection("players")
    .doc(uid)
    .set(
      {
        nickname: body.nickname,
        lastSeenAt: new Date(),
      },
      { merge: true },
    );

  const existing = await db.collection("players").doc(uid).get();
  if (!existing.data()?.createdAt) {
    await db.collection("players").doc(uid).set({ createdAt: new Date() }, { merge: true });
  }

  res.json({ playerId: uid });
});

playerRouter.get("/api/player/stats", requireAuth, async (req: AuthedRequest, res) => {
  const uid = req.uid as string;

  const [playerSnap, bestSnap] = await Promise.all([db.collection("players").doc(uid).get(), db.collection("leaderboard_alltime").doc(uid).get()]);

  const player = playerSnap.data() as { totalTowersBuilt?: number; bestPerfectStreak?: number; bestAverageAccuracy?: number } | undefined;
  const best = bestSnap.data() as { score?: number; height?: number } | undefined;

  res.json({
    highScore: best?.score ?? 0,
    highestFloor: best?.height ?? 0,
    bestPerfectStreak: player?.bestPerfectStreak ?? 0,
    bestAverageAccuracy: player?.bestAverageAccuracy ?? 0,
    totalTowersBuilt: player?.totalTowersBuilt ?? 0,
  });
});
