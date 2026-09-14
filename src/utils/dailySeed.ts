/**
 * Deterministic per-UTC-day seed so every player faces identical Daily
 * Tower conditions. Never derive this from the client's local clock for
 * anything authoritative — the Worker computes/validates it server-side.
 */
export function utcDateKey(date: Date = new Date()): string {
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, "0");
  const d = String(date.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function dailySeed(dateKey: string, gameVersion: string): string {
  return `daily:${dateKey}:${gameVersion}`;
}
