import { el, formatNumber } from "./dom";
import { LocalStorageService } from "@/storage/LocalStorage";
import { prefersReducedMotion } from "@/utils/motion";
import { TAGLINES } from "@/branding";

const SCORE_TWEEN_MS = 380;
const BIG_GAIN_THRESHOLD = 150;

export interface HUDCallbacks {
  onToggleSound: (enabled: boolean) => void;
  onToggleHaptics: (enabled: boolean) => void;
}

/**
 * Score/floor readout + settings toggles + floating feedback toasts.
 * Plain DOM/CSS overlay above the canvas — cheap, accessible, and only
 * touched on state changes (the canvas owns the animated scene).
 */
export class HUD {
  readonly root: HTMLElement;
  private readonly scoreEl: HTMLElement;
  private readonly floorEl: HTMLElement;
  private readonly soundBtn: HTMLButtonElement;
  private readonly hapticsBtn: HTMLButtonElement;
  private readonly offlineBanner: HTMLElement;
  private displayedScore = 0;
  private tweenHandle: number | null = null;

  constructor(callbacks: HUDCallbacks) {
    const prefs = LocalStorageService.getPreferences();

    this.scoreEl = el("div", { className: "tt-hud__score", text: "0" });
    this.floorEl = el("div", { className: "tt-hud__floor", text: "FLOOR 0" });

    this.soundBtn = el("button", { className: "tt-icon-btn", "aria-label": "Toggle sound", "aria-pressed": String(prefs.soundEnabled), text: "♪" }) as HTMLButtonElement;
    this.hapticsBtn = el("button", { className: "tt-icon-btn", "aria-label": "Toggle haptics", "aria-pressed": String(prefs.hapticsEnabled), text: "◉" }) as HTMLButtonElement;

    this.soundBtn.addEventListener("click", () => this.toggle(this.soundBtn, "soundEnabled", callbacks.onToggleSound));
    this.hapticsBtn.addEventListener("click", () => this.toggle(this.hapticsBtn, "hapticsEnabled", callbacks.onToggleHaptics));

    this.offlineBanner = el("div", { className: "tt-offline-banner", text: "CONNECTION LOST — SCORE WILL SYNC WHEN ONLINE" });
    this.offlineBanner.hidden = true;

    const hud = el("div", { className: "tt-hud" }, [this.scoreEl, this.floorEl]);
    const toggles = el("div", { className: "tt-hud__toggles" }, [this.soundBtn, this.hapticsBtn]);

    this.root = el("div", {}, [hud, toggles, this.offlineBanner]);
    window.addEventListener("online", () => this.setOffline(false));
    window.addEventListener("offline", () => this.setOffline(true));
    this.setOffline(!navigator.onLine);
  }

  private toggle(btn: HTMLButtonElement, key: "soundEnabled" | "hapticsEnabled", cb: (enabled: boolean) => void): void {
    const next = btn.getAttribute("aria-pressed") !== "true";
    btn.setAttribute("aria-pressed", String(next));
    LocalStorageService.updatePreferences({ [key]: next });
    cb(next);
  }

  setOffline(offline: boolean): void {
    this.offlineBanner.hidden = !offline;
  }

  /** Animates a count-up from the currently shown value rather than snapping — see docs request for score-feel. */
  setScore(score: number): void {
    const from = this.displayedScore;
    const delta = score - from;
    this.displayedScore = score;

    if (prefersReducedMotion() || delta === 0) {
      this.scoreEl.textContent = formatNumber(score);
      return;
    }

    if (Math.abs(delta) >= BIG_GAIN_THRESHOLD) this.pulseScore();

    if (this.tweenHandle !== null) cancelAnimationFrame(this.tweenHandle);
    const start = performance.now();
    const tick = (now: number): void => {
      const t = Math.min(1, (now - start) / SCORE_TWEEN_MS);
      const eased = 1 - Math.pow(1 - t, 3);
      this.scoreEl.textContent = formatNumber(from + delta * eased);
      if (t < 1) {
        this.tweenHandle = requestAnimationFrame(tick);
      } else {
        this.tweenHandle = null;
        this.scoreEl.textContent = formatNumber(score);
      }
    };
    this.tweenHandle = requestAnimationFrame(tick);
  }

  private pulseScore(): void {
    this.scoreEl.classList.remove("tt-hud__score--pulse");
    // Force reflow so re-adding the class restarts the CSS animation on rapid consecutive gains.
    void this.scoreEl.offsetWidth;
    this.scoreEl.classList.add("tt-hud__score--pulse");
  }

  showRecordBanner(): void {
    this.showFeedback(TAGLINES.newRecord, "perfect");
  }

  setFloor(floor: number): void {
    this.floorEl.textContent = `FLOOR ${floor}`;
  }

  resetScore(): void {
    if (this.tweenHandle !== null) cancelAnimationFrame(this.tweenHandle);
    this.tweenHandle = null;
    this.displayedScore = 0;
    this.scoreEl.textContent = "0";
  }

  showFeedback(text: string, kind: "nice" | "close" | "perfect", worldXFraction = 0.5): void {
    const toast = el("div", {
      className: `tt-feedback ${kind === "perfect" ? "tt-feedback--perfect" : "tt-feedback--score"}`,
      text,
      style: `top: 40%; left: ${worldXFraction * 100}%`,
    });
    this.root.appendChild(toast);
    toast.addEventListener("animationend", () => toast.remove());
    window.setTimeout(() => toast.remove(), 900);
  }

  showScorePopup(amount: number): void {
    this.showFeedback(`+${formatNumber(amount)}`, "close");
  }

  setVisible(visible: boolean): void {
    this.root.style.display = visible ? "" : "none";
  }
}
