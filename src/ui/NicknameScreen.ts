import { el } from "./dom";
import { normalizeNickname } from "@/utils/nickname";

export interface NicknameScreenCallbacks {
  onConfirm: (nickname: string) => void;
}

/**
 * One-time (and re-openable) prompt asking the player to pick the name
 * shown on the leaderboard, instead of silently keeping the auto-generated
 * "PLAYER-XXXX" default. Validation mirrors normalizeNickname so whatever
 * is shown here is exactly what ends up stored/displayed.
 */
export class NicknameScreen {
  readonly root: HTMLElement;
  private readonly input: HTMLInputElement;

  constructor(callbacks: NicknameScreenCallbacks) {
    const title = el("h1", { className: "tt-title", text: "YOUR NAME" });
    const subtitle = el("p", { className: "tt-subtitle", text: "SHOWN ON THE LEADERBOARD" });

    this.input = el("input", {
      className: "tt-input",
      type: "text",
      maxlength: "16",
      placeholder: "NICKNAME",
      autocomplete: "off",
      autocapitalize: "off",
      spellcheck: "false",
    });

    const confirmBtn = el("button", { className: "tt-btn tt-btn--primary", text: "CONTINUE" });
    const submit = (): void => callbacks.onConfirm(normalizeNickname(this.input.value));
    confirmBtn.addEventListener("click", submit);
    this.input.addEventListener("keydown", (event) => {
      if (event.key === "Enter") submit();
    });

    this.root = el("div", { className: "tt-screen", id: "nickname-screen" }, [title, subtitle, this.input, confirmBtn]);
    this.setVisible(false);
  }

  setVisible(visible: boolean): void {
    this.root.style.display = visible ? "flex" : "none";
    if (visible) requestAnimationFrame(() => this.input.focus());
  }

  setValue(nickname: string): void {
    this.input.value = nickname;
  }
}
