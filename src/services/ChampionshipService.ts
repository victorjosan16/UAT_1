import {
  collection,
  doc,
  getDocs,
  limit as fbLimit,
  onSnapshot,
  orderBy,
  query,
  runTransaction,
  setDoc,
  updateDoc,
  where,
  type Unsubscribe,
} from "firebase/firestore";
import { getFirestoreDb } from "./firebase";
import { randomId } from "@/utils/rng";
import type { CategoryId } from "@/types";

export type ChampionshipStatus = "WAITING" | "COUNTDOWN" | "PLAYING" | "COMPLETE";

export interface ChampionshipLobby {
  status: ChampionshipStatus;
  seed: string;
  categoryId?: CategoryId;
  maxPlayers: number;
  playerCount: number;
  currentQuestionIndex: number;
  questionStartAt: number | null;
  countdownStartAt: number | null;
  createdAt: number;
}

export interface ChampionshipPlayer {
  playerId: string;
  nickname: string;
  score: number;
  correctCount: number;
  lastAnsweredIndex: number;
  joinedAt: number;
}

export const MAX_PLAYERS = 5;
export const MIN_PLAYERS_TO_START = 2;
export const WAIT_TIMEOUT_MS = 30_000;
export const COUNTDOWN_MS = 3_000;

function lobbyRef(lobbyId: string) {
  return doc(getFirestoreDb(), "championship_lobbies", lobbyId);
}

function playerRef(lobbyId: string, playerId: string) {
  return doc(getFirestoreDb(), "championship_lobbies", lobbyId, "players", playerId);
}

async function tryJoinLobby(lobbyId: string, playerId: string, nickname: string): Promise<boolean> {
  try {
    const db = getFirestoreDb();
    return await runTransaction(db, async (tx) => {
      const lobbySnap = await tx.get(lobbyRef(lobbyId));
      if (!lobbySnap.exists()) return false;
      const lobby = lobbySnap.data() as ChampionshipLobby;
      if (lobby.status !== "WAITING" || lobby.playerCount >= lobby.maxPlayers) return false;

      const playerSnap = await tx.get(playerRef(lobbyId, playerId));
      if (playerSnap.exists()) return true; // rejoin after a reload — already counted

      tx.set(playerRef(lobbyId, playerId), { nickname, score: 0, correctCount: 0, lastAnsweredIndex: -1, joinedAt: Date.now() });
      tx.update(lobbyRef(lobbyId), { playerCount: lobby.playerCount + 1 });
      return true;
    });
  } catch {
    return false;
  }
}

async function createLobby(playerId: string, nickname: string, categoryId?: CategoryId): Promise<string> {
  const lobbyId = randomId(10);
  const payload: ChampionshipLobby = {
    status: "WAITING",
    seed: randomId(12),
    maxPlayers: MAX_PLAYERS,
    playerCount: 1,
    currentQuestionIndex: 0,
    questionStartAt: null,
    countdownStartAt: null,
    createdAt: Date.now(),
    ...(categoryId ? { categoryId } : {}),
  };
  await setDoc(lobbyRef(lobbyId), payload);
  await setDoc(playerRef(lobbyId, playerId), { nickname, score: 0, correctCount: 0, lastAnsweredIndex: -1, joinedAt: Date.now() });
  return lobbyId;
}

/**
 * No matchmaking server: a client scans recent WAITING lobbies and tries to
 * claim a seat via a guarded transaction (first writer wins, everyone else's
 * transaction just no-ops on a stale read); if none has room, it creates a
 * new one and becomes its first player.
 */
export const ChampionshipService = {
  async findOrCreateLobby(playerId: string, nickname: string, categoryId?: CategoryId): Promise<string> {
    const db = getFirestoreDb();
    const lobbiesQuery = query(collection(db, "championship_lobbies"), where("status", "==", "WAITING"), orderBy("createdAt", "asc"), fbLimit(10));
    const snapshot = await getDocs(lobbiesQuery);

    for (const lobbyDoc of snapshot.docs) {
      const joined = await tryJoinLobby(lobbyDoc.id, playerId, nickname);
      if (joined) return lobbyDoc.id;
    }

    return createLobby(playerId, nickname, categoryId);
  },

  subscribeLobby(lobbyId: string, onChange: (lobby: ChampionshipLobby | null) => void): Unsubscribe {
    return onSnapshot(
      lobbyRef(lobbyId),
      (snap) => onChange(snap.exists() ? (snap.data() as ChampionshipLobby) : null),
      () => onChange(null),
    );
  },

  subscribePlayers(lobbyId: string, onChange: (players: ChampionshipPlayer[]) => void): Unsubscribe {
    const playersQuery = query(collection(getFirestoreDb(), "championship_lobbies", lobbyId, "players"), orderBy("score", "desc"));
    return onSnapshot(
      playersQuery,
      (snap) => onChange(snap.docs.map((d) => ({ playerId: d.id, ...(d.data() as Omit<ChampionshipPlayer, "playerId">) }))),
      () => onChange([]),
    );
  },

  /** Only meaningful pre-match — once PLAYING/COMPLETE a player just stops reporting progress instead. */
  async leaveLobby(lobbyId: string, playerId: string): Promise<void> {
    try {
      const db = getFirestoreDb();
      await runTransaction(db, async (tx) => {
        const lobbySnap = await tx.get(lobbyRef(lobbyId));
        if (!lobbySnap.exists()) return;
        const lobby = lobbySnap.data() as ChampionshipLobby;
        if (lobby.status !== "WAITING") return;
        tx.delete(playerRef(lobbyId, playerId));
        tx.update(lobbyRef(lobbyId), { playerCount: Math.max(0, lobby.playerCount - 1) });
      });
    } catch {
      // Best-effort — worst case a ghost seat sits in a lobby nobody else can fill until it times out.
    }
  },

  /** Fire-and-forget: a player's own running score/progress, same trust model as the leaderboard. */
  async reportProgress(lobbyId: string, playerId: string, data: { score: number; correctCount: number; lastAnsweredIndex: number }): Promise<void> {
    try {
      await updateDoc(playerRef(lobbyId, playerId), data);
    } catch {
      // Offline — final standings just show a stale row for this player; never blocks their own local run.
    }
  },

  /** Any client may call this; the transaction's own guards make it safe for several to race. */
  async tryStartCountdown(lobbyId: string): Promise<void> {
    try {
      const db = getFirestoreDb();
      await runTransaction(db, async (tx) => {
        const snap = await tx.get(lobbyRef(lobbyId));
        if (!snap.exists()) return;
        const lobby = snap.data() as ChampionshipLobby;
        if (lobby.status !== "WAITING") return;
        const full = lobby.playerCount >= lobby.maxPlayers;
        const timedOutWithEnough = lobby.playerCount >= MIN_PLAYERS_TO_START && Date.now() - lobby.createdAt >= WAIT_TIMEOUT_MS;
        if (!full && !timedOutWithEnough) return;
        tx.update(lobbyRef(lobbyId), { status: "COUNTDOWN", countdownStartAt: Date.now() });
      });
    } catch {
      // Another subscribed client will retry on its own next tick.
    }
  },

  async tryStartPlaying(lobbyId: string): Promise<void> {
    try {
      const db = getFirestoreDb();
      await runTransaction(db, async (tx) => {
        const snap = await tx.get(lobbyRef(lobbyId));
        if (!snap.exists()) return;
        const lobby = snap.data() as ChampionshipLobby;
        if (lobby.status !== "COUNTDOWN") return;
        if (!lobby.countdownStartAt || Date.now() - lobby.countdownStartAt < COUNTDOWN_MS) return;
        tx.update(lobbyRef(lobbyId), { status: "PLAYING", currentQuestionIndex: 0, questionStartAt: Date.now() });
      });
    } catch {
      // Another subscribed client will retry.
    }
  },

  async tryAdvanceQuestion(lobbyId: string, expectedIndex: number, totalQuestions: number): Promise<void> {
    try {
      const db = getFirestoreDb();
      await runTransaction(db, async (tx) => {
        const snap = await tx.get(lobbyRef(lobbyId));
        if (!snap.exists()) return;
        const lobby = snap.data() as ChampionshipLobby;
        if (lobby.status !== "PLAYING" || lobby.currentQuestionIndex !== expectedIndex) return;
        const nextIndex = expectedIndex + 1;
        if (nextIndex >= totalQuestions) {
          tx.update(lobbyRef(lobbyId), { status: "COMPLETE" });
        } else {
          tx.update(lobbyRef(lobbyId), { currentQuestionIndex: nextIndex, questionStartAt: Date.now() });
        }
      });
    } catch {
      // Another subscribed client will retry.
    }
  },
};
