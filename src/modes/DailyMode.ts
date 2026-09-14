import { dailySeed, utcDateKey } from "@/utils/dailySeed";
import { GAME_VERSION } from "@/branding";
import type { GameMode } from "@/types";

export interface DailyRunConfig {
  mode: GameMode;
  seed: string;
  dateKey: string;
  targetScoreToBeat: null;
}

/**
 * One deterministic global seed per UTC calendar day. The client computes
 * this the same way the Worker does (`worker/services/daily.ts`) purely
 * for display before the network round-trip completes; the Worker's
 * server-clock value is authoritative for anything competitive.
 */
export function createDailyRun(now: Date = new Date()): DailyRunConfig {
  const dateKey = utcDateKey(now);
  return { mode: "DAILY", seed: dailySeed(dateKey, GAME_VERSION), dateKey, targetScoreToBeat: null };
}
