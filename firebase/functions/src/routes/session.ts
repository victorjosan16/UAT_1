import { Router } from "express";
import { db, Timestamp, FieldValue } from "../lib/firestore";
import { requireAuth, type AuthedRequest } from "../lib/auth";
import { parseBody } from "../lib/respond";
import { checkRateLimit } from "../lib/rateLimit";
import { SessionStartBodySchema, SessionCompleteBodySchema } from "../validation/schemas";
import { revalidateRun } from "../lib/revalidate";
import { updateLeaderboardsAndStats, computeRanks } from "../services/leaderboardService";
import { GAME_VERSION, RULES_VERSION, dailySeed, utcDateKey, randomId, type RunSummary, type GameMode } from "../lib/gameRules";

export const sessionRouter = Router();

const SESSION_TTL_MS = 45 * 60 * 1000;

interface SessionDoc {
  playerId: string;
  mode: GameMode;
  seed: string;
  gameVersion: string;
  rulesVersion: number;
  status: "active" | "completed";
  createdAt: Timestamp;
  expiresAt: Timestamp;
}

sessionRouter.post("/api/session/start", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(SessionStartBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;

  const allowed = await checkRateLimit(`session_start_${uid}`, 30);
  if (!allowed) {
    res.status(429).json({ error: "Too many requests", code: "RATE_LIMITED" });
    return;
  }

  // Daily Tower's seed is always server-derived from the UTC date — never
  // trust a client-supplied seed for it (that's the whole point of a
  // shared daily challenge). Every other mode may pass its own seed
  // (Classic/Endless generate one locally; Challenge replays a friend's).
  const seed = body.mode === "DAILY" ? dailySeed(utcDateKey(), GAME_VERSION) : (body.seed ?? `${body.mode.toLowerCase()}:${randomId(12)}`);

  const sessionId = randomId(20);
  const now = Timestamp.now();
  const expiresAt = Timestamp.fromMillis(now.toMillis() + SESSION_TTL_MS);

  const session: SessionDoc = {
    playerId: uid,
    mode: body.mode,
    seed,
    gameVersion: GAME_VERSION,
    rulesVersion: RULES_VERSION,
    status: "active",
    createdAt: now,
    expiresAt,
  };
  await db.collection("sessions").doc(sessionId).set(session);

  res.json({ sessionId, seed, gameVersion: GAME_VERSION, serverTimeMs: now.toMillis() });
});

type CompleteOutcome = { ok: true; validation: ReturnType<typeof revalidateRun> } | { ok: false; reason: string };

sessionRouter.post("/api/session/complete", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(SessionCompleteBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;
  const summary = body.summary as RunSummary;

  const sessionRef = db.collection("sessions").doc(body.sessionId);

  const outcome = await db.runTransaction<CompleteOutcome>(async (tx) => {
    const snap = await tx.get(sessionRef);
    if (!snap.exists) return { ok: false, reason: "SESSION_NOT_FOUND" };
    const session = snap.data() as SessionDoc;

    if (session.playerId !== uid) return { ok: false, reason: "FORBIDDEN" };
    if (session.status !== "active") return { ok: false, reason: "SESSION_ALREADY_COMPLETED" };
    if (session.expiresAt.toMillis() < Date.now()) return { ok: false, reason: "SESSION_EXPIRED" };
    if (session.seed !== summary.seed) return { ok: false, reason: "SEED_MISMATCH" };
    if (session.gameVersion !== summary.gameVersion) return { ok: false, reason: "VERSION_MISMATCH" };
    if (session.mode !== summary.mode) return { ok: false, reason: "MODE_MISMATCH" };

    const validation = revalidateRun(summary);

    tx.update(sessionRef, {
      status: "completed",
      completedAt: FieldValue.serverTimestamp(),
      validationStatus: validation.valid ? "accepted" : "rejected",
      validatedScore: validation.valid ? summary.score : 0,
      validatedHeight: validation.valid ? summary.height : 0,
      validatedPerfectCount: validation.valid ? summary.perfectCount : 0,
      validatedAverageAccuracy: validation.valid ? summary.averageAccuracy : 0,
      rejectionReason: validation.valid ? null : validation.reason,
    });

    return { ok: true, validation };
  });

  if (!outcome.ok) {
    const statusByReason: Record<string, number> = {
      SESSION_NOT_FOUND: 404,
      FORBIDDEN: 403,
      SESSION_ALREADY_COMPLETED: 409,
      SESSION_EXPIRED: 410,
      SEED_MISMATCH: 422,
      VERSION_MISMATCH: 422,
      MODE_MISMATCH: 422,
    };
    res.status(statusByReason[outcome.reason] ?? 400).json({ error: outcome.reason, code: outcome.reason });
    return;
  }

  if (!outcome.validation.valid) {
    res.json({ accepted: false, validatedScore: 0, rank: { daily: null, weekly: null, allTime: null, percentile: null } });
    return;
  }

  const playerSnap = await db.collection("players").doc(uid).get();
  const nickname = (playerSnap.data()?.nickname as string | undefined) ?? "PLAYER";

  await updateLeaderboardsAndStats(uid, nickname, summary);
  const rank = await computeRanks(summary.score);

  res.json({ accepted: true, validatedScore: summary.score, rank });
});
