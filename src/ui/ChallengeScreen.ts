import { el } from "./dom";
import type { ChallengeDetails, ChallengeAttemptResponse } from "@/services/ChallengeService";

export interface ChallengeScreenCallbacks {
  onAccept: (challenge: ChallengeDetails) => void;
  onTryAgain: (challenge: ChallengeDetails) => void;
  onChallengeSomeoneElse: () => void;
  onClose: () => void;
}

export class ChallengeScreen {
  readonly root: HTMLElement;
  private body: HTMLElement;

  constructor(private readonly callbacks: ChallengeScreenCallbacks) {
    this.body = el("div", { style: "display:flex;flex-direction:column;align-items:center;gap:16px;width:100%" });
    this.root = el("div", { className: "tt-screen", id: "challenge-screen" }, [this.body]);
    this.setVisible(false);
  }

  showInvite(challenge: ChallengeDetails): void {
    const acceptBtn = el("button", { className: "tt-btn tt-btn--primary", text: "ACCEPT CHALLENGE" }) as HTMLButtonElement;
    acceptBtn.addEventListener("click", () => this.callbacks.onAccept(challenge));

    this.body.replaceChildren(
      el("p", { className: "tt-subtitle", text: `${challenge.creatorNickname.toUpperCase()}'S TOWER` }),
      el("div", { className: "tt-stats" }, [stat("HEIGHT", String(challenge.creatorHeight)), stat("SCORE", challenge.creatorScore.toLocaleString("en-US"))]),
      el("p", { className: "tt-hint", text: "CAN YOU BEAT IT?" }),
      acceptBtn,
    );
    this.setVisible(true);
  }

  showResult(challenge: ChallengeDetails, result: ChallengeAttemptResponse): void {
    const tryAgainBtn = el("button", { className: "tt-btn tt-btn--primary", text: "TRY AGAIN" }) as HTMLButtonElement;
    tryAgainBtn.addEventListener("click", () => this.callbacks.onTryAgain(challenge));

    const otherBtn = el("button", { className: "tt-btn tt-btn--secondary", text: "CHALLENGE SOMEONE ELSE" }) as HTMLButtonElement;
    otherBtn.addEventListener("click", this.callbacks.onChallengeSomeoneElse);

    const headline = result.outcome === "WIN" ? "YOU WIN!" : result.outcome === "SO_CLOSE" ? "SO CLOSE!" : "SO CLOSE!";

    this.body.replaceChildren(
      el("div", { className: "tt-stats" }, [
        stat("YOU", result.yourScore.toLocaleString("en-US")),
        stat(challenge.creatorNickname.toUpperCase(), result.opponentScore.toLocaleString("en-US")),
      ]),
      el("p", { className: "tt-subtitle", text: headline, style: result.outcome === "WIN" ? "color:#ffd54f" : "" }),
      tryAgainBtn,
      otherBtn,
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
