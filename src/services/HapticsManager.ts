import { LocalStorageService } from "@/storage/LocalStorage";

/** Thin wrapper over the Vibration API — no-ops silently where unsupported (iOS Safari, desktop) or disabled in Preferences. */
class HapticsManager {
  private isEnabled(): boolean {
    try {
      return LocalStorageService.getPreferences().hapticsEnabled;
    } catch {
      return true;
    }
  }

  private vibrate(pattern: number | number[]): void {
    if (!this.isEnabled()) return;
    if (typeof navigator === "undefined" || typeof navigator.vibrate !== "function") return;
    navigator.vibrate(pattern);
  }

  correct(): void {
    this.vibrate(15);
  }

  wrong(): void {
    this.vibrate([12, 40, 12]);
  }

  streak(): void {
    this.vibrate([10, 30, 10, 30, 20]);
  }

  complete(): void {
    this.vibrate([20, 40, 20, 40, 40]);
  }
}

export const hapticsManager = new HapticsManager();
