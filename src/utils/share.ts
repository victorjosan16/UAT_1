import { GAME_NAME } from "@/branding";

export interface ShareCardData {
  knowledgeIQ: number;
  correctCount: number;
  totalQuestions: number;
  score: number;
  bestStreak: number;
  topPercent: number | null;
  url: string;
}

export function buildShareText(data: ShareCardData): string {
  const lines = [
    GAME_NAME,
    "",
    `Q5 IQ ${data.knowledgeIQ}`,
    `${data.correctCount}/${data.totalQuestions}`,
    `${data.score.toLocaleString("en-US")} POINTS`,
  ];
  if (data.bestStreak >= 3) lines.push(`🔥 ×${data.bestStreak}`);
  if (data.topPercent !== null) lines.push(`TOP ${data.topPercent.toFixed(0)}%`);
  lines.push("", "CAN YOU BEAT ME?", data.url);
  return lines.join("\n");
}

/** Web Share API where supported, clipboard fallback otherwise. Never throws into gameplay. */
export async function shareResult(data: ShareCardData): Promise<"shared" | "copied" | "failed"> {
  return shareLink(GAME_NAME, buildShareText(data), data.url);
}

/**
 * Lower-level share primitive for anything that isn't a full score card
 * (e.g. Question of the Day). `text` must already include `url` wherever
 * it belongs in the message — the Web Share API takes them separately,
 * but the clipboard fallback only ever copies `text`.
 */
export async function shareLink(title: string, text: string, url: string): Promise<"shared" | "copied" | "failed"> {
  try {
    if (navigator.share) {
      await navigator.share({ title, text, url });
      return "shared";
    }
  } catch {
    // User cancelled or share failed — fall through to clipboard.
  }
  try {
    await navigator.clipboard.writeText(text);
    return "copied";
  } catch {
    return "failed";
  }
}
