import { randomId } from "./rng";

/**
 * Shared nickname validation/normalization — used by the client (optimistic
 * local display) and by the Cloud Functions API (the authoritative check
 * before a nickname is stored). Keeping this in one module means the two
 * can never silently drift apart on what counts as a valid nickname.
 */
export function normalizeNickname(raw: string): string {
  const cleaned = raw
    .replace(/[^a-zA-Z0-9 _-]/g, "")
    .trim()
    .slice(0, 16);
  return cleaned.length > 0 ? cleaned : `PLAYER-${randomId(4)}`;
}
