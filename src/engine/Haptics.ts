/**
 * Thin wrapper around `navigator.vibrate`, behind a feature check. Never
 * assume the API exists (desktop browsers, iOS Safari, etc. don't have it).
 */
export class HapticsManager {
  private enabled = true;
  private readonly supported: boolean;

  constructor() {
    this.supported = typeof navigator !== "undefined" && typeof navigator.vibrate === "function";
  }

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  isSupported(): boolean {
    return this.supported;
  }

  private vibrate(pattern: number | number[]): void {
    if (!this.enabled || !this.supported) return;
    navigator.vibrate(pattern);
  }

  place(): void {
    this.vibrate(10);
  }

  /** Intensity scales gently with streak: light -> medium -> a short pattern at big streaks. */
  perfect(streak: number): void {
    if (streak >= 10) this.vibrate([14, 20, 14, 20, 22]);
    else if (streak >= 5) this.vibrate([16, 16, 24]);
    else this.vibrate(22);
  }

  comboMilestone(): void {
    this.vibrate([16, 30, 16]);
  }

  nearMiss(): void {
    this.vibrate([12, 24, 12]);
  }

  newRecord(): void {
    this.vibrate([20, 30, 20, 30, 40]);
  }

  failure(): void {
    this.vibrate([40, 40, 80]);
  }
}
