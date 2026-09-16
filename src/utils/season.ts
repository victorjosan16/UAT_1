import type { Language } from "@/types";

/**
 * Monthly leaderboard "Seasons" (see MASTER PROMPT §19-20) — a UTC
 * calendar month, reset competitively every month while every past
 * season's results stay stored (see LeaderboardService's "monthly" scope,
 * which never deletes prior months' collections).
 */
export function utcMonthKey(date: Date = new Date()): string {
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, "0");
  return `${y}-${m}`;
}

export function previousMonthKey(monthKey: string): string {
  const [year, month] = monthKey.split("-").map(Number) as [number, number];
  return utcMonthKey(new Date(Date.UTC(year, month - 2, 1)));
}

const SEASON_EPOCH_YEAR = 2025;
const SEASON_EPOCH_MONTH = 1; // January — Season 01.

/** Purely derived from the calendar, never a stored counter — Season 01 is January 2025, Season 02 is February 2025, etc. */
export function seasonNumberFor(monthKey: string): number {
  const [year, month] = monthKey.split("-").map(Number) as [number, number];
  return (year - SEASON_EPOCH_YEAR) * 12 + (month - SEASON_EPOCH_MONTH) + 1;
}

const LOCALE_BY_LANGUAGE: Record<Language, string> = {
  en: "en-US",
  ro: "ro-RO",
  es: "es-ES",
  pt: "pt-PT",
  hi: "hi-IN",
  id: "id-ID",
  ru: "ru-RU",
};

export function seasonMonthLabel(monthKey: string, language: Language): string {
  const [year, month] = monthKey.split("-").map(Number) as [number, number];
  const date = new Date(Date.UTC(year, month - 1, 1));
  return new Intl.DateTimeFormat(LOCALE_BY_LANGUAGE[language], { month: "long", timeZone: "UTC" }).format(date).toUpperCase();
}
