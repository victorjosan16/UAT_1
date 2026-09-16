import type { Language } from "@/types";

/**
 * Storage abstraction: every localStorage read/write in the app goes
 * through here, both so preferences have a typed schema in one place and
 * so the persistence layer can be swapped later without touching callers.
 * NEVER treat anything stored here as authoritative for leaderboards —
 * it is client-controlled and only for local, non-competitive state.
 */

const SUPPORTED_LANGUAGES: readonly Language[] = ["en", "ro", "es", "pt", "hi", "id", "ru"];

function detectDefaultLanguage(): Language {
  try {
    const nav = typeof navigator !== "undefined" ? navigator.language.toLowerCase() : "";
    if (nav.startsWith("ro")) return "ro";
  } catch {
    // navigator unavailable (SSR/tests) — fall through to the default below.
  }
  return "en";
}

export interface Preferences {
  soundEnabled: boolean;
  hapticsEnabled: boolean;
  tutorialCompleted: boolean;
  reducedEffects: boolean;
  /** True once the player has explicitly confirmed a nickname (vs. the silent auto-generated default) — gates the one-time nickname prompt. */
  nicknameConfirmed: boolean;
  /** True once a real Placement Quiz has produced this player's KR — false for a legacy player whose rating was defaulted (see PlayerService). */
  placementCompleted: boolean;
}

export interface LocalBests {
  bestScore: number;
  bestKnowledgeIQ: number;
  bestStreak: number;
  totalQuizzesPlayed: number;
  dailyStreak: number;
  lastDailyDateKey: string | null;
}

export interface PendingSubmission {
  id: string;
  kind: "quizSession" | "challengeAttempt" | "dailyScore";
  payload: unknown;
  createdAtMs: number;
}

const KEYS = {
  playerId: "q5.playerId",
  nickname: "q5.nickname",
  preferences: "q5.preferences",
  localBests: "q5.localBests",
  pendingQueue: "q5.pendingQueue",
  currentLevel: "q5.currentLevel",
  language: "q5.language",
  rating: "q5.rating",
  lastKnownRankPrefix: "q5.lastKnownRank.",
  milestonesReached: "q5.milestonesReached",
  lastSeenMonthKey: "q5.lastSeenMonthKey",
} as const;

const DEFAULT_PREFERENCES: Preferences = {
  soundEnabled: true,
  hapticsEnabled: true,
  tutorialCompleted: false,
  reducedEffects: false,
  nicknameConfirmed: false,
  placementCompleted: false,
};

const DEFAULT_BESTS: LocalBests = {
  bestScore: 0,
  bestKnowledgeIQ: 0,
  bestStreak: 0,
  totalQuizzesPlayed: 0,
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

  /** Which of the 20 curriculum levels (or beyond, for Endless) the player is currently on in Level Journey. */
  getCurrentLevel(): number {
    const raw = safeGet(KEYS.currentLevel);
    const parsed = raw ? Number.parseInt(raw, 10) : 1;
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
  },
  setCurrentLevel(level: number): void {
    safeSet(KEYS.currentLevel, String(level));
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

  /** Falls back to the browser's own language on first launch (Romanian if it starts with "ro", else English), then remembers whatever the player picks. */
  getLanguage(): Language {
    const raw = safeGet(KEYS.language);
    return raw && (SUPPORTED_LANGUAGES as readonly string[]).includes(raw) ? (raw as Language) : detectDefaultLanguage();
  },
  setLanguage(language: Language): void {
    safeSet(KEYS.language, language);
  },

  /** Knowledge Rating (KR) — see quiz/RatingEngine.ts. Null only before a rating has ever been assigned. */
  getRating(): number | null {
    const raw = safeGet(KEYS.rating);
    const parsed = raw ? Number(raw) : NaN;
    return Number.isFinite(parsed) ? parsed : null;
  },
  setRating(rating: number): void {
    safeSet(KEYS.rating, String(Math.round(rating)));
  },

  /** Last leaderboard position seen for a scope — lets the UI show "#184 -> #142" only when a genuine prior snapshot exists (never a fabricated one). */
  getLastKnownRank(scope: string): number | null {
    const raw = safeGet(KEYS.lastKnownRankPrefix + scope);
    const parsed = raw ? Number(raw) : NaN;
    return Number.isFinite(parsed) ? parsed : null;
  },
  setLastKnownRank(scope: string, rank: number): void {
    safeSet(KEYS.lastKnownRankPrefix + scope, String(rank));
  },

  /** One-time milestone celebrations (Top 100/50/10/#1 — see MASTER PROMPT §17/18): each key fires its animation once, ever. */
  getMilestonesReached(): string[] {
    const raw = safeGet(KEYS.milestonesReached);
    if (!raw) return [];
    try {
      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? parsed.filter((v): v is string => typeof v === "string") : [];
    } catch {
      return [];
    }
  },
  hasMilestoneBeenReached(key: string): boolean {
    return this.getMilestonesReached().includes(key);
  },
  markMilestoneReached(key: string): void {
    const current = this.getMilestonesReached();
    if (current.includes(key)) return;
    safeSet(KEYS.milestonesReached, JSON.stringify([...current, key]));
  },

  /** The UTC month key ("2026-09") this client last saw — a mismatch with the real current month means a Season just changed (see MASTER PROMPT §20). Null only before the very first check. */
  getLastSeenMonthKey(): string | null {
    return safeGet(KEYS.lastSeenMonthKey);
  },
  setLastSeenMonthKey(monthKey: string): void {
    safeSet(KEYS.lastSeenMonthKey, monthKey);
  },
};
