import { apiClient, ApiError, type ApiClient } from "./ApiClient";
import { LocalStorageService } from "@/storage/LocalStorage";
import { randomId } from "@/utils/rng";

export interface PlayerIdentity {
  playerId: string;
  nickname: string;
}

export interface PlayerStats {
  highScore: number;
  highestFloor: number;
  bestPerfectStreak: number;
  bestAverageAccuracy: number;
  totalTowersBuilt: number;
}

/**
 * Guest-first identity: the very first launch silently creates a local
 * anonymous player (no interruption before first play). Auth can be added
 * later without changing callers — they only ever see `PlayerIdentity`.
 */
export class PlayerService {
  constructor(private readonly api: ApiClient = apiClient) {}

  ensureIdentity(): PlayerIdentity {
    let playerId = LocalStorageService.getPlayerId();
    let nickname = LocalStorageService.getNickname();

    if (!playerId) {
      playerId = `guest_${randomId(10)}`;
      LocalStorageService.setPlayerId(playerId);
    }
    if (!nickname) {
      nickname = `PLAYER-${randomId(4)}`;
      LocalStorageService.setNickname(nickname);
    }

    // Fire-and-forget registration; gameplay never waits on this.
    void this.registerRemote(playerId, nickname);

    return { playerId, nickname };
  }

  private async registerRemote(playerId: string, nickname: string): Promise<void> {
    try {
      const result = await this.api.post<{ playerId: string; token: string }>("/player", { playerId, nickname });
      LocalStorageService.setPlayerToken(result.token);
    } catch (error) {
      if (!(error instanceof ApiError)) throw error;
      // Offline or backend unavailable — local guest identity still works fully offline.
    }
  }

  setNickname(nickname: string): void {
    const trimmed = normalizeNickname(nickname);
    LocalStorageService.setNickname(trimmed);
    void this.api.post("/player", { playerId: LocalStorageService.getPlayerId(), nickname: trimmed }).catch(() => undefined);
  }

  async fetchStats(): Promise<PlayerStats | null> {
    try {
      return await this.api.get<PlayerStats>("/player/stats");
    } catch {
      return null;
    }
  }
}

export function normalizeNickname(raw: string): string {
  const cleaned = raw
    .replace(/[^a-zA-Z0-9 _-]/g, "")
    .trim()
    .slice(0, 16);
  return cleaned.length > 0 ? cleaned : `PLAYER-${randomId(4)}`;
}

export const playerService = new PlayerService();
