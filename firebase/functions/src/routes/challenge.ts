import { Router } from "express";
import { db, Timestamp } from "../lib/firestore";
import { requireAuth, optionalAuth, type AuthedRequest } from "../lib/auth";
import { parseBody } from "../lib/respond";
import { SessionIdBodySchema } from "../validation/schemas";
import { randomId, evaluateChallengeOutcome } from "../lib/gameRules";

export const challengeRouter = Router();

const CHALLENGE_TTL_MS = 7 * 24 * 60 * 60 * 1000;

interface SessionDocForChallenge {
  playerId: string;
  mode: string;
  seed: string;
  gameVersion: string;
  status: string;
  validationStatus?: string;
  validatedScore?: number;
  validatedHeight?: number;
}

interface ChallengeDoc {
  creatorPlayerId: string;
  creatorNickname: string;
  creatorScore: number;
  creatorHeight: number;
  seed: string;
  gameVersion: string;
  createdAt: Timestamp;
  expiresAt: Timestamp;
}

async function loadValidatedSession(sessionId: string, uid: string): Promise<SessionDocForChallenge | null> {
  const snap = await db.collection("sessions").doc(sessionId).get();
  if (!snap.exists) return null;
  const session = snap.data() as SessionDocForChallenge;
  if (session.playerId !== uid) return null;
  if (session.status !== "completed" || session.validationStatus !== "accepted") return null;
  return session;
}

challengeRouter.post("/api/challenge", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(SessionIdBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;

  const session = await loadValidatedSession(body.sessionId, uid);
  if (!session) {
    res.status(422).json({ error: "Session is not a validated run", code: "INVALID_SESSION" });
    return;
  }

  const playerSnap = await db.collection("players").doc(uid).get();
  const nickname = (playerSnap.data()?.nickname as string | undefined) ?? "PLAYER";

  const challengeId = randomId(8);
  const now = Timestamp.now();
  const doc: ChallengeDoc = {
    creatorPlayerId: uid,
    creatorNickname: nickname,
    creatorScore: session.validatedScore ?? 0,
    creatorHeight: session.validatedHeight ?? 0,
    seed: session.seed,
    gameVersion: session.gameVersion,
    createdAt: now,
    expiresAt: Timestamp.fromMillis(now.toMillis() + CHALLENGE_TTL_MS),
  };
  await db.collection("challenges").doc(challengeId).set(doc);

  const url = `${req.protocol}://${req.get("host")}/challenge/${challengeId}`;
  res.json({ challengeId, url, expiresAt: doc.expiresAt.toDate().toISOString() });
});

challengeRouter.get("/api/challenge/:id", optionalAuth, async (req, res) => {
  const challengeId = String(req.params.id ?? "");
  const snap = await db.collection("challenges").doc(challengeId).get();
  if (!snap.exists) {
    res.status(404).json({ error: "Challenge not found", code: "CHALLENGE_NOT_FOUND" });
    return;
  }
  const challenge = snap.data() as ChallengeDoc;
  const expired = challenge.expiresAt.toMillis() < Date.now();

  res.json({
    challengeId,
    creatorNickname: challenge.creatorNickname,
    creatorScore: challenge.creatorScore,
    creatorHeight: challenge.creatorHeight,
    seed: challenge.seed,
    gameVersion: challenge.gameVersion,
    expired,
  });
});

challengeRouter.post("/api/challenge/:id/attempt", requireAuth, async (req: AuthedRequest, res) => {
  const body = parseBody(SessionIdBodySchema, req.body, res);
  if (!body) return;
  const uid = req.uid as string;
  const challengeId = String(req.params.id ?? "");

  const [challengeSnap, session] = await Promise.all([db.collection("challenges").doc(challengeId).get(), loadValidatedSession(body.sessionId, uid)]);

  if (!challengeSnap.exists) {
    res.status(404).json({ error: "Challenge not found", code: "CHALLENGE_NOT_FOUND" });
    return;
  }
  const challenge = challengeSnap.data() as ChallengeDoc;

  if (challenge.expiresAt.toMillis() < Date.now()) {
    res.status(410).json({ error: "Challenge has expired", code: "CHALLENGE_EXPIRED" });
    return;
  }
  if (!session || session.mode !== "CHALLENGE" || session.seed !== challenge.seed) {
    res.status(422).json({ error: "Session does not match this challenge's seed", code: "SEED_MISMATCH" });
    return;
  }

  const yourScore = session.validatedScore ?? 0;
  const yourHeight = session.validatedHeight ?? 0;

  const attemptRef = db.collection("challenges").doc(challengeId).collection("attempts").doc(uid);
  await db.runTransaction(async (tx) => {
    const existing = await tx.get(attemptRef);
    const prevBest = (existing.data()?.score as number | undefined) ?? 0;
    if (yourScore > prevBest) {
      tx.set(attemptRef, { score: yourScore, height: yourHeight, updatedAt: Timestamp.now() }, { merge: true });
    }
  });

  const outcome = evaluateChallengeOutcome(yourScore, challenge.creatorScore);

  res.json({
    accepted: true,
    yourScore,
    yourHeight,
    opponentScore: challenge.creatorScore,
    opponentHeight: challenge.creatorHeight,
    outcome,
  });
});
