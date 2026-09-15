import { apiClient, ApiError, type ApiClient } from "./ApiClient";
import { ensureSignedIn } from "./firebase";
import { LocalStorageService } from "@/storage/LocalStorage";
import { normalizeNickname } from "@/utils/nickname";
import { randomId } from "@/utils/rng";
import { withTimeout } from "@/utils/async";

const SIGN_IN_TIMEOUT_MS = 4000;

export interface PlayerIdentity {
  playerId: string;
  nickname: string;
}

export interface PlayerStats {
  bestScore: number;
  bestFootballIQ: number;
  bestStreak: number;
  totalQuizzesPlayed: number;
  dailyStreak: number;
}

/**
 * Guest-first identity: Firebase Anonymous Auth creates a stable uid on
 * first launch (no interruption before first play), which Firebase
 * persists across reloads — the same device keeps the same player. The
 * uid *is* the player id everywhere (client and server); nicknames are
 * the only thing the player chooses. Upgrading to a real account later
 * only means linking a credential to this same uid.
 */
export class PlayerService {
  constructor(private readonly api: ApiClient = apiClient) {}

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

    // Fire-and-forget registration; gameplay never waits on this.
    void this.registerRemote(nickname);

    return { playerId, nickname };
  }

  private async registerRemote(nickname: string): Promise<void> {
    try {
      await this.api.post("/player", { nickname });
    } catch (error) {
      if (!(error instanceof ApiError)) throw error;
      // Offline or backend unavailable — local guest identity still works fully offline.
    }
  }

  setNickname(nickname: string): void {
    const trimmed = normalizeNickname(nickname);
    LocalStorageService.setNickname(trimmed);
    void this.api.post("/player", { nickname: trimmed }).catch(() => undefined);
  }

  async fetchStats(): Promise<PlayerStats | null> {
    try {
      return await this.api.get<PlayerStats>("/player/stats");
    } catch {
      return null;
    }
  }
}

export const playerService = new PlayerService();
