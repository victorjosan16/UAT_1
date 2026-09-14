import { leaderboardService } from "./LeaderboardService";
import { dailyService } from "./DailyService";
import { challengeService } from "./ChallengeService";
import { LocalStorageService } from "@/storage/LocalStorage";
import { randomId } from "@/utils/rng";
import type { GameMode, RunSummary } from "@/types";

/**
 * Bridges a finished run to the network: starts a session up front
 * (best-effort — gameplay never waits on it), then submits the result at
 * game-over. On any network failure the submission is queued locally and
 * flushed on the next `online` event, per docs/ARCHITECTURE.md's offline
 * data flow — a run is never lost just because the network is down.
 */
export class RunSubmitter {
  private sessionIdPromise: Promise<string> | null = null;

  beginRun(mode: GameMode, seed: string): void {
    this.sessionIdPromise = leaderboardService
      .startSession(mode, seed)
      .then((res) => res.sessionId)
      .catch(() => `local_${randomId(12)}`);
  }

  /** The current run's session id, once resolved (falls back to a local id if the network is unavailable). */
  getSessionId(): Promise<string> {
    return this.sessionIdPromise ?? Promise.resolve(`local_${randomId(12)}`);
  }

  async completeRun(summary: RunSummary, extra: { dailyDateKey?: string; challengeId?: string } = {}): Promise<void> {
    const sessionId = (await this.sessionIdPromise) ?? `local_${randomId(12)}`;

    try {
      await leaderboardService.completeSession(sessionId, summary);
    } catch {
      LocalStorageService.enqueuePending({ id: randomId(12), kind: "session", payload: { sessionId, summary }, createdAtMs: Date.now() });
    }

    if (extra.dailyDateKey) {
      try {
        await dailyService.submitScore(sessionId);
      } catch {
        LocalStorageService.enqueuePending({ id: randomId(12), kind: "dailyScore", payload: { sessionId }, createdAtMs: Date.now() });
      }
    }

    if (extra.challengeId) {
      try {
        await challengeService.submitAttempt(extra.challengeId, sessionId);
      } catch {
        LocalStorageService.enqueuePending({ id: randomId(12), kind: "challengeAttempt", payload: { challengeId: extra.challengeId, sessionId }, createdAtMs: Date.now() });
      }
    }
  }
}

export const runSubmitter = new RunSubmitter();

window.addEventListener("online", () => {
  void flushPendingQueue();
});

async function flushPendingQueue(): Promise<void> {
  const queue = LocalStorageService.getPendingQueue();
  if (queue.length === 0) return;
  const remaining = [];
  for (const item of queue) {
    try {
      if (item.kind === "session") {
        const payload = item.payload as { sessionId: string; summary: RunSummary };
        await leaderboardService.completeSession(payload.sessionId, payload.summary);
      } else if (item.kind === "dailyScore") {
        const payload = item.payload as { sessionId: string };
        await dailyService.submitScore(payload.sessionId);
      } else if (item.kind === "challengeAttempt") {
        const payload = item.payload as { challengeId: string; sessionId: string };
        await challengeService.submitAttempt(payload.challengeId, payload.sessionId);
      }
    } catch {
      remaining.push(item);
    }
  }
  LocalStorageService.setPendingQueue(remaining);
}
