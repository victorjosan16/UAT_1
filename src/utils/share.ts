import { GAME_NAME } from "@/branding";

export interface ShareCardData {
  height: number;
  score: number;
  topPercent: number | null;
  url: string;
}

export function buildShareText(data: ShareCardData): string {
  const lines = [
    GAME_NAME,
    "",
    `${data.height} FLOORS`,
    `${data.score.toLocaleString("en-US")} POINTS`,
  ];
  if (data.topPercent !== null) lines.push(`TOP ${data.topPercent.toFixed(1)}%`);
  lines.push("", "CAN YOU BUILD HIGHER?", data.url);
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
