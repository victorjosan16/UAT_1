import type { Language } from "@/types";

/**
 * Every UI string in the app, keyed by dotted path then by language — see
 * docs/GAME_DESIGN.md §Localization. Question/category/country content
 * lives next to its own data instead (data/questions/*, data/categories.ts,
 * data/countries.ts) since each entry needs its own translation, not a
 * shared template.
 */
const STRINGS = {
  "nav.home": { en: "Home", ro: "Acasă" },
  "nav.discover": { en: "Discover", ro: "Descoperă" },
  "nav.play": { en: "Play", ro: "Joacă" },
  "nav.ranking": { en: "Ranking", ro: "Clasament" },
  "nav.profile": { en: "Profile", ro: "Profil" },

  "home.greeting": { en: "Hi, {name}", ro: "Salut, {name}" },
  "home.dailyStreakDays": { en: "{count} days", ro: "{count} zile" },
  "home.dailyStreakDay": { en: "{count} day", ro: "{count} zi" },
  "home.hero.eyebrow": { en: "DAILY CHALLENGE", ro: "PROVOCAREA ZILEI" },
  "home.hero.title": { en: "10 Questions, Once a Day", ro: "10 Întrebări, O Dată pe Zi" },
  "home.hero.meta": { en: "Same set for everyone today — see how you stack up.", ro: "Același set pentru toți azi — vezi cum te descurci." },
  "home.hero.cta": { en: "PLAY NOW", ro: "JOACĂ ACUM" },
  "home.quickPlay": { en: "Quick Play", ro: "Joc Rapid" },
  "home.levelJourney": { en: "Level Journey", ro: "Traseul Nivelurilor" },
  "home.categories": { en: "Categories", ro: "Categorii" },
  "home.seeAll": { en: "See all", ro: "Vezi tot" },
  "home.continue": { en: "Continue", ro: "Continuă" },
  "home.continueCta": { en: "CONTINUE", ro: "CONTINUĂ" },
  "home.yourBest": { en: "Your Best", ro: "Recordurile Tale" },
  "home.bestScore": { en: "Best Score", ro: "Scor Maxim" },
  "home.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim" },

  "discover.title": { en: "Discover", ro: "Descoperă" },
  "discover.subtitle": { en: "Pick a category and start a 10-question run.", ro: "Alege o categorie și începe un set de 10 întrebări." },
  "discover.play": { en: "PLAY", ro: "JOACĂ" },
  "discover.comingSoon": { en: "SOON", ro: "ÎN CURÂND" },
  "discover.comingSoonBadge": { en: "Coming soon", ro: "În curând" },

  "play.title": { en: "Play", ro: "Joacă" },
  "play.subtitle": { en: "Choose how you want to play.", ro: "Alege cum vrei să joci." },
  "play.quickPlay.title": { en: "Quick Play", ro: "Joc Rapid" },
  "play.quickPlay.desc": { en: "10 mixed questions, straight in.", ro: "10 întrebări variate, direct la joc." },
  "play.category.title": { en: "Category Quiz", ro: "Quiz pe Categorii" },
  "play.category.desc": { en: "Pick a topic and test it in depth.", ro: "Alege un subiect și testează-l în profunzime." },
  "play.daily.title": { en: "Daily Challenge", ro: "Provocarea Zilei" },
  "play.daily.desc": { en: "Same 10 questions for everyone today.", ro: "Aceleași 10 întrebări pentru toți azi." },
  "play.level.title": { en: "Level Journey", ro: "Traseul Nivelurilor" },
  "play.level.desc": { en: "20 levels, Rookie to Genius, then Endless.", ro: "20 de niveluri, de la Începător la Geniu, apoi Infinit." },
  "play.championship.title": { en: "5-Player Championship", ro: "Campionat pe 5 Jucători" },
  "play.championship.desc": { en: "Live multiplayer — coming soon.", ro: "Multiplayer live — în curând." },
  "play.friend.title": { en: "Challenge a Friend", ro: "Provoacă un Prieten" },
  "play.friend.desc": { en: "Async 1v1 — coming soon.", ro: "1v1 asincron — în curând." },

  "ranking.title": { en: "Ranking", ro: "Clasament" },
  "ranking.subtitle": { en: "Online leaderboards are coming soon.", ro: "Clasamentele online vin în curând." },
  "ranking.emptyText": { en: "Play a run to start tracking your best score.", ro: "Joacă un set ca să-ți urmărești scorul maxim." },
  "ranking.bestScore": { en: "Best Score", ro: "Scor Maxim" },
  "ranking.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim" },
  "ranking.bestStreak": { en: "Best Streak", ro: "Serie Maximă" },
  "ranking.runsPlayed": { en: "Runs Played", ro: "Jocuri Jucate" },

  "profile.guestPlayer": { en: "Guest player", ro: "Jucător invitat" },
  "profile.stats": { en: "Stats", ro: "Statistici" },
  "profile.badges": { en: "Badges", ro: "Insigne" },
  "profile.runsPlayed": { en: "Runs Played", ro: "Jocuri Jucate" },
  "profile.bestScore": { en: "Best Score", ro: "Scor Maxim" },
  "profile.bestStreak": { en: "Best Streak", ro: "Serie Maximă" },
  "profile.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim" },
  "profile.dailyStreak": { en: "Daily Streak", ro: "Serie Zilnică" },
  "profile.locked": { en: "Locked", ro: "Blocată" },
  "profile.language": { en: "Language", ro: "Limbă" },

  "quiz.questionOf": { en: "Question {index} of {total}", ro: "Întrebarea {index} din {total}" },
  "quiz.correct": { en: "CORRECT!", ro: "CORECT!" },
  "quiz.timesUp": { en: "TIME'S UP!", ro: "TIMPUL A EXPIRAT!" },
  "quiz.quit": { en: "Quit", ro: "Ieșire" },
  "quiz.correctAnswers": { en: "Correct answers", ro: "Răspunsuri corecte" },
  "quiz.streak": { en: "Streak", ro: "Serie" },
  "quiz.score": { en: "Score", ro: "Scor" },
  "quiz.level": { en: "LEVEL", ro: "NIVEL" },

  "results.perfectGame": { en: "PERFECT GAME", ro: "JOC PERFECT" },
  "results.newRecord": { en: "NEW RECORD!", ro: "RECORD NOU!" },
  "results.correct": { en: "CORRECT", ro: "CORECTE" },
  "results.points": { en: "POINTS", ro: "PUNCTE" },
  "results.bestStreak": { en: "BEST STREAK", ro: "SERIE MAXIMĂ" },
  "results.avgResponse": { en: "AVG RESPONSE", ro: "TIMP MEDIU" },
  "results.playAgain": { en: "PLAY AGAIN", ro: "JOACĂ DIN NOU" },
  "results.backToHome": { en: "BACK TO HOME", ro: "ÎNAPOI ACASĂ" },

  "badge.firstWin": { en: "FIRST WIN", ro: "PRIMA VICTORIE" },
  "badge.perfect10": { en: "PERFECT 10", ro: "10 PERFECT" },
  "badge.onFire": { en: "ON FIRE", ro: "ÎN FLĂCĂRI" },
  "badge.sevenDayStreak": { en: "7 DAY STREAK", ro: "SERIE DE 7 ZILE" },
  "badge.thousandQuestions": { en: "1000 QUESTIONS", ro: "1000 DE ÎNTREBĂRI" },
  "badge.champion": { en: "CHAMPION", ro: "CAMPION" },
} as const satisfies Record<string, Record<Language, string>>;

export type StringKey = keyof typeof STRINGS;

/** Resolves a UI string in the given language, substituting any `{placeholder}` values. */
export function translate(key: StringKey, language: Language, vars?: Record<string, string | number>): string {
  const template = STRINGS[key][language];
  if (!vars) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) => (name in vars ? String(vars[name]) : match));
}
