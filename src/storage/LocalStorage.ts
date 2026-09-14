/**
 * Storage abstraction: every localStorage read/write in the app goes
 * through here, both so preferences have a typed schema in one place and
 * so the persistence layer can be swapped later without touching callers.
 * NEVER treat anything stored here as authoritative for leaderboards —
 * it is client-controlled and only for local, non-competitive state.
 */

export interface Preferences {
  soundEnabled: boolean;
  hapticsEnabled: boolean;
  tutorialCompleted: boolean;
  reducedEffects: boolean;
}

export interface LocalBests {
  highScore: number;
  highestFloor: number;
  bestPerfectStreak: number;
  bestAverageAccuracy: number;
  totalTowersBuilt: number;
  dailyStreak: number;
  lastDailyDateKey: string | null;
}

export interface PendingSubmission {
  id: string;
  kind: "session" | "challengeAttempt" | "dailyScore";
  payload: unknown;
  createdAtMs: number;
}

const KEYS = {
  playerId: "tt.playerId",
  nickname: "tt.nickname",
  preferences: "tt.preferences",
  localBests: "tt.localBests",
  pendingQueue: "tt.pendingQueue",
} as const;

const DEFAULT_PREFERENCES: Preferences = {
  soundEnabled: true,
  hapticsEnabled: true,
  tutorialCompleted: false,
  reducedEffects: false,
};

const DEFAULT_BESTS: LocalBests = {
  highScore: 0,
  highestFloor: 0,
  bestPerfectStreak: 0,
  bestAverageAccuracy: 0,
  totalTowersBuilt: 0,
  dailyStreak: 0,
  lastDailyDateKey: null,
};

function safeGet(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeSet(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // Storage unavailable (private mode, quota, etc.) — fail silently; never break gameplay.
  }
}

function readJson<T>(key: string, fallback: T): T {
  const raw = safeGet(key);
  if (!raw) return fallback;
  try {
    return { ...fallback, ...(JSON.parse(raw) as Partial<T>) };
  } catch {
    return fallback;
  }
}

export const LocalStorageService = {
  getPlayerId(): string | null {
    return safeGet(KEYS.playerId);
  },
  setPlayerId(id: string): void {
    safeSet(KEYS.playerId, id);
  },
  getNickname(): string | null {
    return safeGet(KEYS.nickname);
  },
  setNickname(nickname: string): void {
    safeSet(KEYS.nickname, nickname);
  },

  getPreferences(): Preferences {
    return readJson(KEYS.preferences, DEFAULT_PREFERENCES);
  },
  setPreferences(preferences: Preferences): void {
    safeSet(KEYS.preferences, JSON.stringify(preferences));
  },
  updatePreferences(patch: Partial<Preferences>): Preferences {
    const next = { ...this.getPreferences(), ...patch };
    this.setPreferences(next);
    return next;
  },

  getLocalBests(): LocalBests {
    return readJson(KEYS.localBests, DEFAULT_BESTS);
  },
  setLocalBests(bests: LocalBests): void {
    safeSet(KEYS.localBests, JSON.stringify(bests));
  },

  getPendingQueue(): PendingSubmission[] {
    const raw = safeGet(KEYS.pendingQueue);
    if (!raw) return [];
    try {
      return JSON.parse(raw) as PendingSubmission[];
    } catch {
      return [];
    }
  },
  setPendingQueue(queue: PendingSubmission[]): void {
    safeSet(KEYS.pendingQueue, JSON.stringify(queue));
  },
  enqueuePending(item: PendingSubmission): void {
    const queue = this.getPendingQueue();
    queue.push(item);
    this.setPendingQueue(queue);
  },
};
