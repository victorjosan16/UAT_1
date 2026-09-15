import type { Club, League } from "@/types";

/**
 * The club database. Names/countries/leagues are public factual
 * information (not a licensing concern) — see docs/ASSETS_AND_RIGHTS.md.
 * Only the VISUAL crest is a rights concern, and that's handled entirely
 * separately by src/assets/crestPlaceholder.ts, which never touches this
 * file. Swapping in real, licensed crest images later means changing the
 * asset resolver only — nothing here.
 *
 * `difficulty` is the recognizability tier used to build levels (see
 * src/quiz/levels.ts): 1 = extremely famous, 5 = obscure or easily
 * confused with a similar club.
 */

interface ClubSeed {
  id: string;
  name: string;
  country: string;
  league: League;
  difficulty: Club["difficulty"];
}

const CLUB_SEEDS: ClubSeed[] = [
  // --- Premier League (England) ---
  { id: "arsenal", name: "Arsenal", country: "England", league: "PREMIER_LEAGUE", difficulty: 1 },
  { id: "liverpool", name: "Liverpool", country: "England", league: "PREMIER_LEAGUE", difficulty: 1 },
  { id: "man-utd", name: "Manchester United", country: "England", league: "PREMIER_LEAGUE", difficulty: 1 },
  { id: "man-city", name: "Manchester City", country: "England", league: "PREMIER_LEAGUE", difficulty: 1 },
  { id: "chelsea", name: "Chelsea", country: "England", league: "PREMIER_LEAGUE", difficulty: 1 },
  { id: "tottenham", name: "Tottenham Hotspur", country: "England", league: "PREMIER_LEAGUE", difficulty: 2 },
  { id: "newcastle", name: "Newcastle United", country: "England", league: "PREMIER_LEAGUE", difficulty: 3 },
  { id: "west-ham", name: "West Ham United", country: "England", league: "PREMIER_LEAGUE", difficulty: 3 },
  { id: "everton", name: "Everton", country: "England", league: "PREMIER_LEAGUE", difficulty: 3 },
  { id: "aston-villa", name: "Aston Villa", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "leicester", name: "Leicester City", country: "England", league: "PREMIER_LEAGUE", difficulty: 3 },
  { id: "wolves", name: "Wolverhampton Wanderers", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "forest", name: "Nottingham Forest", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "brighton", name: "Brighton & Hove Albion", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "crystal-palace", name: "Crystal Palace", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "fulham", name: "Fulham", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },
  { id: "brentford", name: "Brentford", country: "England", league: "PREMIER_LEAGUE", difficulty: 5 },
  { id: "southampton", name: "Southampton", country: "England", league: "PREMIER_LEAGUE", difficulty: 4 },

  // --- LaLiga (Spain) ---
  { id: "real-madrid", name: "Real Madrid", country: "Spain", league: "LALIGA", difficulty: 1 },
  { id: "barcelona", name: "Barcelona", country: "Spain", league: "LALIGA", difficulty: 1 },
  { id: "atletico-madrid", name: "Atletico Madrid", country: "Spain", league: "LALIGA", difficulty: 2 },
  { id: "sevilla", name: "Sevilla", country: "Spain", league: "LALIGA", difficulty: 3 },
  { id: "real-sociedad", name: "Real Sociedad", country: "Spain", league: "LALIGA", difficulty: 4 },
  { id: "real-betis", name: "Real Betis", country: "Spain", league: "LALIGA", difficulty: 4 },
  { id: "villarreal", name: "Villarreal", country: "Spain", league: "LALIGA", difficulty: 3 },
  { id: "athletic-bilbao", name: "Athletic Bilbao", country: "Spain", league: "LALIGA", difficulty: 4 },
  { id: "valencia", name: "Valencia", country: "Spain", league: "LALIGA", difficulty: 3 },
  { id: "celta-vigo", name: "Celta Vigo", country: "Spain", league: "LALIGA", difficulty: 5 },

  // --- Serie A (Italy) ---
  { id: "juventus", name: "Juventus", country: "Italy", league: "SERIE_A", difficulty: 1 },
  { id: "ac-milan", name: "AC Milan", country: "Italy", league: "SERIE_A", difficulty: 2 },
  { id: "inter-milan", name: "Inter Milan", country: "Italy", league: "SERIE_A", difficulty: 2 },
  { id: "napoli", name: "Napoli", country: "Italy", league: "SERIE_A", difficulty: 2 },
  { id: "as-roma", name: "AS Roma", country: "Italy", league: "SERIE_A", difficulty: 3 },
  { id: "lazio", name: "Lazio", country: "Italy", league: "SERIE_A", difficulty: 3 },
  { id: "atalanta", name: "Atalanta", country: "Italy", league: "SERIE_A", difficulty: 4 },
  { id: "fiorentina", name: "Fiorentina", country: "Italy", league: "SERIE_A", difficulty: 3 },
  { id: "torino", name: "Torino", country: "Italy", league: "SERIE_A", difficulty: 5 },

  // --- Bundesliga (Germany) ---
  { id: "bayern-munich", name: "Bayern Munich", country: "Germany", league: "BUNDESLIGA", difficulty: 1 },
  { id: "dortmund", name: "Borussia Dortmund", country: "Germany", league: "BUNDESLIGA", difficulty: 2 },
  { id: "rb-leipzig", name: "RB Leipzig", country: "Germany", league: "BUNDESLIGA", difficulty: 3 },
  { id: "bayer-leverkusen", name: "Bayer Leverkusen", country: "Germany", league: "BUNDESLIGA", difficulty: 4 },
  { id: "gladbach", name: "Borussia Monchengladbach", country: "Germany", league: "BUNDESLIGA", difficulty: 4 },
  { id: "frankfurt", name: "Eintracht Frankfurt", country: "Germany", league: "BUNDESLIGA", difficulty: 4 },
  { id: "stuttgart", name: "VfB Stuttgart", country: "Germany", league: "BUNDESLIGA", difficulty: 4 },
  { id: "schalke", name: "Schalke 04", country: "Germany", league: "BUNDESLIGA", difficulty: 3 },
  { id: "hamburger-sv", name: "Hamburger SV", country: "Germany", league: "BUNDESLIGA", difficulty: 5 },

  // --- Ligue 1 (France) ---
  { id: "psg", name: "Paris Saint-Germain", country: "France", league: "LIGUE_1", difficulty: 1 },
  { id: "marseille", name: "Marseille", country: "France", league: "LIGUE_1", difficulty: 2 },
  { id: "lyon", name: "Lyon", country: "France", league: "LIGUE_1", difficulty: 3 },
  { id: "monaco", name: "Monaco", country: "France", league: "LIGUE_1", difficulty: 3 },
  { id: "lille", name: "Lille", country: "France", league: "LIGUE_1", difficulty: 4 },
  { id: "nice", name: "Nice", country: "France", league: "LIGUE_1", difficulty: 5 },
  { id: "rennes", name: "Rennes", country: "France", league: "LIGUE_1", difficulty: 5 },

  // --- Other Europe (Portugal, Netherlands, Scotland, Belgium, Turkey) ---
  { id: "porto", name: "Porto", country: "Portugal", league: "OTHER_EUROPE", difficulty: 3 },
  { id: "benfica", name: "Benfica", country: "Portugal", league: "OTHER_EUROPE", difficulty: 3 },
  { id: "sporting-cp", name: "Sporting CP", country: "Portugal", league: "OTHER_EUROPE", difficulty: 4 },
  { id: "ajax", name: "Ajax", country: "Netherlands", league: "OTHER_EUROPE", difficulty: 2 },
  { id: "psv", name: "PSV Eindhoven", country: "Netherlands", league: "OTHER_EUROPE", difficulty: 4 },
  { id: "feyenoord", name: "Feyenoord", country: "Netherlands", league: "OTHER_EUROPE", difficulty: 4 },
  { id: "celtic", name: "Celtic", country: "Scotland", league: "OTHER_EUROPE", difficulty: 3 },
  { id: "rangers", name: "Rangers", country: "Scotland", league: "OTHER_EUROPE", difficulty: 3 },
  { id: "galatasaray", name: "Galatasaray", country: "Turkey", league: "OTHER_EUROPE", difficulty: 4 },
  { id: "fenerbahce", name: "Fenerbahce", country: "Turkey", league: "OTHER_EUROPE", difficulty: 4 },
  { id: "anderlecht", name: "Anderlecht", country: "Belgium", league: "OTHER_EUROPE", difficulty: 5 },

  // --- International (South America, North America, Middle East) ---
  { id: "flamengo", name: "Flamengo", country: "Brazil", league: "INTERNATIONAL", difficulty: 3 },
  { id: "boca-juniors", name: "Boca Juniors", country: "Argentina", league: "INTERNATIONAL", difficulty: 3 },
  { id: "river-plate", name: "River Plate", country: "Argentina", league: "INTERNATIONAL", difficulty: 3 },
  { id: "palmeiras", name: "Palmeiras", country: "Brazil", league: "INTERNATIONAL", difficulty: 4 },
  { id: "corinthians", name: "Corinthians", country: "Brazil", league: "INTERNATIONAL", difficulty: 4 },
  { id: "la-galaxy", name: "LA Galaxy", country: "USA", league: "INTERNATIONAL", difficulty: 5 },
  { id: "al-hilal", name: "Al Hilal", country: "Saudi Arabia", league: "INTERNATIONAL", difficulty: 5 },
  { id: "al-nassr", name: "Al Nassr", country: "Saudi Arabia", league: "INTERNATIONAL", difficulty: 5 },
];

export const CLUBS: readonly Club[] = CLUB_SEEDS.map((seed) => ({ ...seed, aliases: [] }));

export function getClubById(id: string): Club | undefined {
  return CLUBS.find((club) => club.id === id);
}

export const LEAGUE_LABEL: Record<League, string> = {
  PREMIER_LEAGUE: "Premier League",
  LALIGA: "LaLiga",
  SERIE_A: "Serie A",
  BUNDESLIGA: "Bundesliga",
  LIGUE_1: "Ligue 1",
  OTHER_EUROPE: "Europe",
  INTERNATIONAL: "World",
};
