import { el } from "./dom";
import { GAME_NAME, TAGLINES } from "@/branding";
import type { RunSummary } from "@/types";

export interface GameOverStats extends RunSummary {
  best: number;
  worldRank: number | null;
  topPercent: number | null;
  isNewRecord: boolean;
}

export interface GameOverCallbacks {
  onPlayAgain: () => void;
  onChallengeFriend: () => void;
  onOpenLeaderboard: () => void;
  onShare: () => void;
}

export class GameOverScreen {
  readonly root: HTMLElement;
  private readonly statsContainer: HTMLElement;
  private readonly recordBanner: HTMLElement;

  constructor(callbacks: GameOverCallbacks) {
    const title = el("h1", { className: "tt-title", text: GAME_NAME, style: "font-size:28px;margin-bottom:0" });
    this.recordBanner = el("p", { className: "tt-subtitle", text: TAGLINES.newRecord, style: "color:#ffd54f" });
    this.recordBanner.hidden = true;

    this.statsContainer = el("div", { className: "tt-stats" });

    const playAgainBtn = el("button", { className: "tt-btn tt-btn--primary", text: "PLAY AGAIN" }) as HTMLButtonElement;
    playAgainBtn.addEventListener("click", callbacks.onPlayAgain);

    const challengeBtn = el("button", { className: "tt-btn tt-btn--secondary", text: "CHALLENGE A FRIEND" }) as HTMLButtonElement;
    challengeBtn.addEventListener("click", callbacks.onChallengeFriend);

    const leaderboardBtn = el("button", { className: "tt-btn tt-btn--ghost", text: "LEADERBOARD" }) as HTMLButtonElement;
    leaderboardBtn.addEventListener("click", callbacks.onOpenLeaderboard);

    const shareBtn = el("button", { className: "tt-btn tt-btn--ghost", text: "SHARE" }) as HTMLButtonElement;
    shareBtn.addEventListener("click", callbacks.onShare);

    this.root = el("div", { className: "tt-screen", id: "game-over-screen" }, [
      title,
      this.recordBanner,
      this.statsContainer,
      playAgainBtn,
      challengeBtn,
      leaderboardBtn,
      shareBtn,
    ]);
    this.setVisible(false);
  }

  show(stats: GameOverStats): void {
    this.recordBanner.hidden = !stats.isNewRecord;
    this.statsContainer.replaceChildren(
      stat("SCORE", stats.score.toLocaleString("en-US")),
      stat("HEIGHT", String(stats.height)),
      stat("BEST", stats.best.toLocaleString("en-US")),
      stat("PERFECT", String(stats.perfectCount)),
      ...(stats.topPercent !== null ? [stat("TOP", `${stats.topPercent.toFixed(1)}%`)] : []),
      ...(stats.worldRank !== null ? [stat("WORLD RANK", `#${stats.worldRank.toLocaleString("en-US")}`)] : []),
    );
    this.setVisible(true);
  }

  setVisible(visible: boolean): void {
    this.root.style.display = visible ? "flex" : "none";
  }
}

function stat(label: string, value: string): HTMLElement {
  return el("div", { className: "tt-stat" }, [el("div", { className: "tt-stat__label", text: label }), el("div", { className: "tt-stat__value", text: value })]);
}
