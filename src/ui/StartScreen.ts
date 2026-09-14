import { el } from "./dom";
import { GAME_NAME, GAME_SUBTITLE, TAGLINES } from "@/branding";
import { LocalStorageService } from "@/storage/LocalStorage";

export interface StartScreenCallbacks {
  onPlayClassic: () => void;
  onPlayDaily: () => void;
  onOpenLeaderboard: () => void;
}

export class StartScreen {
  readonly root: HTMLElement;

  constructor(callbacks: StartScreenCallbacks) {
    const bests = LocalStorageService.getLocalBests();
    const tutorialDone = LocalStorageService.getPreferences().tutorialCompleted;

    const title = el("h1", { className: "tt-title", text: GAME_NAME });
    const subtitle = el("p", { className: "tt-subtitle", text: GAME_SUBTITLE });

    const playBtn = el("button", { className: "tt-btn tt-btn--primary", text: tutorialDone ? "PLAY" : TAGLINES.tap }) as HTMLButtonElement;
    playBtn.addEventListener("click", callbacks.onPlayClassic);

    const dailyBtn = el("button", { className: "tt-btn tt-btn--secondary", text: bests.dailyStreak > 0 ? `DAILY TOWER 🔥 ${bests.dailyStreak}` : "DAILY TOWER" }) as HTMLButtonElement;
    dailyBtn.addEventListener("click", callbacks.onPlayDaily);

    const leaderboardBtn = el("button", { className: "tt-btn tt-btn--ghost", text: "LEADERBOARD" }) as HTMLButtonElement;
    leaderboardBtn.addEventListener("click", callbacks.onOpenLeaderboard);

    const bestRow =
      bests.highScore > 0
        ? el("div", { className: "tt-stats" }, [
            statCard("BEST SCORE", bests.highScore.toLocaleString("en-US")),
            statCard("BEST HEIGHT", String(bests.highestFloor)),
          ])
        : el("div");

    this.root = el("div", { className: "tt-screen", id: "start-screen" }, [title, subtitle, bestRow, playBtn, dailyBtn, leaderboardBtn]);
  }

  setVisible(visible: boolean): void {
    this.root.style.display = visible ? "flex" : "none";
  }
}

function statCard(label: string, value: string): HTMLElement {
  return el("div", { className: "tt-stat" }, [el("div", { className: "tt-stat__label", text: label }), el("div", { className: "tt-stat__value", text: value })]);
}
