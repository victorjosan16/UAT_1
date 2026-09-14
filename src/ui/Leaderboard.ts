import { el } from "./dom";
import { leaderboardService, type LeaderboardWindow, type LeaderboardEntry } from "@/services/LeaderboardService";

const WINDOWS: { key: LeaderboardWindow; label: string }[] = [
  { key: "daily", label: "TODAY" },
  { key: "weekly", label: "THIS WEEK" },
  { key: "all-time", label: "ALL TIME" },
];

export class Leaderboard {
  readonly root: HTMLElement;
  private readonly listEl: HTMLElement;
  private readonly tabButtons = new Map<LeaderboardWindow, HTMLButtonElement>();
  private currentPlayerId: string | null = null;

  constructor(onClose: () => void) {
    const title = el("h1", { className: "tt-title", text: "LEADERBOARD", style: "font-size:26px" });

    const tabs = el(
      "div",
      { style: "display:flex;gap:8px;margin-bottom:8px" },
      WINDOWS.map(({ key, label }) => {
        const btn = el("button", { className: "tt-btn tt-btn--ghost", text: label, style: "padding:8px 14px;width:auto;max-width:none;font-size:12px" }) as HTMLButtonElement;
        btn.addEventListener("click", () => void this.load(key));
        this.tabButtons.set(key, btn);
        return btn;
      }),
    );

    this.listEl = el("div", { style: "width:100%;max-width:340px;display:flex;flex-direction:column;gap:6px;overflow-y:auto;max-height:50vh" });

    const closeBtn = el("button", { className: "tt-btn tt-btn--secondary", text: "CLOSE" }) as HTMLButtonElement;
    closeBtn.addEventListener("click", onClose);

    this.root = el("div", { className: "tt-screen", id: "leaderboard-screen" }, [title, tabs, this.listEl, closeBtn]);
    this.setVisible(false);
  }

  async open(playerId: string | null, window: LeaderboardWindow = "daily"): Promise<void> {
    this.currentPlayerId = playerId;
    this.setVisible(true);
    await this.load(window);
  }

  private async load(window: LeaderboardWindow): Promise<void> {
    for (const [key, btn] of this.tabButtons) btn.style.opacity = key === window ? "1" : "0.5";
    this.listEl.replaceChildren(el("p", { style: "color:var(--text-dim)", text: "Loading…" }));
    try {
      const result = await leaderboardService.fetchLeaderboard(window);
      this.renderEntries(result.entries);
    } catch {
      this.listEl.replaceChildren(el("p", { style: "color:var(--text-dim)", text: "Leaderboard unavailable offline." }));
    }
  }

  private renderEntries(entries: LeaderboardEntry[]): void {
    if (entries.length === 0) {
      this.listEl.replaceChildren(el("p", { style: "color:var(--text-dim)", text: "No scores yet — be the first!" }));
      return;
    }
    this.listEl.replaceChildren(
      ...entries.map((entry) =>
        el(
          "div",
          {
            style: `display:flex;justify-content:space-between;padding:10px 14px;border-radius:12px;background:${
              entry.playerId === this.currentPlayerId ? "rgba(79,209,255,0.18)" : "var(--panel)"
            };border:1px solid var(--panel-border)`,
          },
          [
            el("span", { text: `#${entry.rank}  ${entry.nickname}` }),
            el("span", { style: "font-variant-numeric:tabular-nums", text: entry.score.toLocaleString("en-US") }),
          ],
        ),
      ),
    );
  }

  setVisible(visible: boolean): void {
    this.root.style.display = visible ? "flex" : "none";
  }
}
