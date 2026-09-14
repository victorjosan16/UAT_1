/**
 * Provider-independent analytics facade. Swap `sink` to wire up a real
 * provider later; gameplay code only ever calls `track`. Failures here
 * must never throw into gameplay code.
 */
export type AnalyticsEvent =
  | "app_open"
  | "game_start"
  | "placement"
  | "perfect"
  | "game_over"
  | "restart"
  | "level_complete"
  | "endless_unlocked"
  | "challenge_created"
  | "challenge_opened"
  | "challenge_started"
  | "challenge_completed"
  | "share_clicked"
  | "daily_started"
  | "daily_completed"
  | "leaderboard_opened";

export type AnalyticsPayload = Record<string, string | number | boolean | null>;

export interface AnalyticsSink {
  send(event: AnalyticsEvent, payload: AnalyticsPayload): void;
}

class ConsoleSink implements AnalyticsSink {
  send(event: AnalyticsEvent, payload: AnalyticsPayload): void {
    if (import.meta.env.DEV) {
      // eslint-disable-next-line no-console
      console.debug(`[analytics] ${event}`, payload);
    }
  }
}

class AnalyticsService {
  private sink: AnalyticsSink = new ConsoleSink();

  setSink(sink: AnalyticsSink): void {
    this.sink = sink;
  }

  track(event: AnalyticsEvent, payload: AnalyticsPayload = {}): void {
    try {
      this.sink.send(event, payload);
    } catch {
      // Analytics failure must never break gameplay.
    }
  }
}

export const analyticsService = new AnalyticsService();
