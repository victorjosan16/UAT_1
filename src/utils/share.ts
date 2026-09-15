import { GAME_NAME } from "@/branding";

export interface ShareCardData {
  footballIQ: number;
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
    `FOOTBALL IQ ${data.footballIQ}`,
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
  const text = buildShareText(data);
  try {
    if (navigator.share) {
      await navigator.share({ title: GAME_NAME, text, url: data.url });
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
