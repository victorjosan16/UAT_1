import { doc, getDoc, setDoc } from "firebase/firestore";
import { getFirestoreDb } from "./firebase";
import { randomId } from "@/utils/rng";
import type { CategoryId, QuizMode } from "@/types";

export interface ChallengeDoc {
  creatorId: string;
  creatorNickname: string;
  mode: QuizMode;
  seed: string;
  categoryId?: CategoryId;
  level?: number;
  creatorScore: number;
  creatorKnowledgeIQ: number;
  createdAt: number;
}

const CHALLENGE_ID_LENGTH = 6;

/**
 * Async 1v1: a challenge doc pins down the exact same {mode, seed, options}
 * a fresh run already used, so whoever opens the link plays the identical
 * question set — no account needed on either side. Written/read straight
 * from Firestore (see firestore.rules `challenges/{id}`); never a source of
 * truth for anything beyond "what did the creator score".
 */
export const ChallengeService = {
  buildUrl(challengeId: string): string {
    return `${window.location.origin}/c/${challengeId}`;
  },

  parseIdFromLocation(): string | null {
    const match = /^\/c\/([A-Za-z0-9]+)$/.exec(window.location.pathname);
    return match?.[1] ?? null;
  },

  async create(input: Omit<ChallengeDoc, "createdAt">): Promise<string> {
    const id = randomId(CHALLENGE_ID_LENGTH);
    const db = getFirestoreDb();
    const payload: ChallengeDoc = { ...input, createdAt: Date.now() };
    // Firestore rejects `undefined` field values — omit categoryId/level entirely when the run didn't use them.
    const sanitized = Object.fromEntries(Object.entries(payload).filter(([, v]) => v !== undefined));
    await setDoc(doc(db, "challenges", id), sanitized);
    return id;
  },

  async fetch(challengeId: string): Promise<ChallengeDoc | null> {
    try {
      const db = getFirestoreDb();
      const snapshot = await getDoc(doc(db, "challenges", challengeId));
      return snapshot.exists() ? (snapshot.data() as ChallengeDoc) : null;
    } catch {
      return null;
    }
  },
};
