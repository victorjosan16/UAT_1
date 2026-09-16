import { doc, setDoc } from "firebase/firestore";
import { ensureSignedIn, getFirestoreDb } from "./firebase";
import { LocalStorageService } from "@/storage/LocalStorage";
import { normalizeNickname } from "@/utils/nickname";
import { randomId } from "@/utils/rng";
import { withTimeout } from "@/utils/async";

const SIGN_IN_TIMEOUT_MS = 4000;

export interface PlayerIdentity {
  playerId: string;
  nickname: string;
}

/**
 * Guest-first identity: Firebase Anonymous Auth creates a stable uid on
 * first launch (no interruption before first play), which Firebase
 * persists across reloads — the same device keeps the same player. The
 * uid *is* the player id everywhere (client and leaderboard); nicknames
 * are the only thing the player chooses. Upgrading to a real account
 * later only means linking a credential to this same uid.
 */
export class PlayerService {
  /**
   * Never lets a broken/slow/unconfigured Firebase project block the
   * start screen — gameplay must be instant regardless of backend state
   * (see docs/GAME_DESIGN.md "no loading screens"). Firebase sign-in is
   * raced against a short timeout; either a failure or a timeout falls
   * back to a local-only guest id, and the game runs fully offline. If
   * Firebase does come through later, subsequent launches pick it up
   * normally (nothing here is a permanent downgrade).
   */
  async ensureIdentity(): Promise<PlayerIdentity> {
    let playerId = LocalStorageService.getPlayerId();

    try {
      const user = await withTimeout(ensureSignedIn(), SIGN_IN_TIMEOUT_MS);
      playerId = user.uid;
      LocalStorageService.setPlayerId(playerId);
    } catch {
      // Offline, misconfigured project, or just slow — fall back to a local guest id.
      if (!playerId) {
        playerId = `local_${randomId(12)}`;
        LocalStorageService.setPlayerId(playerId);
      }
    }

    let nickname = LocalStorageService.getNickname();
    if (!nickname) {
      nickname = normalizeNickname(`PLAYER-${playerId.slice(-4).toUpperCase()}`);
      LocalStorageService.setNickname(nickname);
    }

    return { playerId, nickname };
  }

  /** Updates the nickname locally (instant) and, best-effort, in Firestore so it's tied to this player's identity — see firestore.rules `players/{playerId}`. */
  setNickname(playerId: string, nickname: string): string {
    const trimmed = normalizeNickname(nickname);
    LocalStorageService.setNickname(trimmed);
    void this.syncNickname(playerId, trimmed);
    return trimmed;
  }

  private async syncNickname(playerId: string, nickname: string): Promise<void> {
    try {
      const db = getFirestoreDb();
      await setDoc(doc(db, "players", playerId), { nickname, updatedAt: Date.now() });
    } catch {
      // Offline, unauthenticated (local guest id), or rules-denied — the nickname still works fully offline from localStorage.
    }
  }
}

export const playerService = new PlayerService();
