/**
 * Deterministic per-UTC-day seed so every player faces an identical Daily
 * Challenge (same questions, same order). Never derive this from the
 * client's local clock for anything authoritative once a server exists to
 * validate it — see docs/GAME_DESIGN.md §Daily Challenge.
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
