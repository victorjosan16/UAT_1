import type { Difficulty, LocalizedText } from "@/types";

/**
 * Structured, factual country data — capital, continent, and a simplified
 * but color-accurate flag layout described declaratively (bands/crosses/
 * emblems), never an image scraped or AI-generated. National flags are
 * official state symbols, not private trademarks, so rendering their real
 * layout/colors precisely (unlike a football club crest) carries no
 * licensing risk — see docs/GAME_DESIGN.md. The correct answer for every
 * FLAG/CAPITALS question always comes from this data, never from the
 * rendering.
 */

export type FlagPattern =
  | { kind: "horizontal"; colors: readonly [string, string] | readonly [string, string, string] }
  | { kind: "vertical"; colors: readonly [string, string] | readonly [string, string, string] }
  | { kind: "nordic-cross"; field: string; cross: string }
  | { kind: "swiss-cross"; field: string; cross: string }
  | { kind: "circle"; field: string; circle: string }
  | { kind: "canton-stripes"; stripeColors: readonly [string, string]; cantonColor: string }
  | { kind: "single"; color: string; overlay?: "star-crescent" }
  | { kind: "union-jack" };

export type Continent = "Europe" | "Asia" | "Africa" | "North America" | "South America" | "Oceania";

export interface Country {
  id: string;
  name: LocalizedText;
  capital: LocalizedText;
  continent: Continent;
  /** 1 = near-universally known, 5 = an easy mix-up even for a strong player (e.g. the Romania/Chad/Andorra/Moldova tricolor family). */
  difficulty: Difficulty;
  flag: FlagPattern;
}

export const COUNTRIES: readonly Country[] = [
  { id: "romania", name: { en: "Romania", ro: "România" }, capital: { en: "Bucharest", ro: "București" }, continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#002B7F", "#FCD116", "#CE1126"] } },
  { id: "chad", name: { en: "Chad", ro: "Ciad" }, capital: { en: "N'Djamena", ro: "N'Djamena" }, continent: "Africa", difficulty: 5, flag: { kind: "vertical", colors: ["#002664", "#FECB00", "#C60C30"] } },
  { id: "andorra", name: { en: "Andorra", ro: "Andorra" }, capital: { en: "Andorra la Vella", ro: "Andorra la Vella" }, continent: "Europe", difficulty: 5, flag: { kind: "vertical", colors: ["#0018A8", "#FEDD00", "#D50032"] } },
  { id: "moldova", name: { en: "Moldova", ro: "Moldova" }, capital: { en: "Chisinau", ro: "Chișinău" }, continent: "Europe", difficulty: 4, flag: { kind: "vertical", colors: ["#003DA5", "#FFD200", "#CC092F"] } },
  { id: "france", name: { en: "France", ro: "Franța" }, capital: { en: "Paris", ro: "Paris" }, continent: "Europe", difficulty: 1, flag: { kind: "vertical", colors: ["#0055A4", "#FFFFFF", "#EF4135"] } },
  { id: "italy", name: { en: "Italy", ro: "Italia" }, capital: { en: "Rome", ro: "Roma" }, continent: "Europe", difficulty: 1, flag: { kind: "vertical", colors: ["#008C45", "#F4F5F0", "#CD212A"] } },
  { id: "ireland", name: { en: "Ireland", ro: "Irlanda" }, capital: { en: "Dublin", ro: "Dublin" }, continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#169B62", "#FFFFFF", "#FF883E"] } },
  { id: "belgium", name: { en: "Belgium", ro: "Belgia" }, capital: { en: "Brussels", ro: "Bruxelles" }, continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#000000", "#FFD90C", "#EF3340"] } },
  { id: "germany", name: { en: "Germany", ro: "Germania" }, capital: { en: "Berlin", ro: "Berlin" }, continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#000000", "#DD0000", "#FFCE00"] } },
  { id: "netherlands", name: { en: "Netherlands", ro: "Olanda" }, capital: { en: "Amsterdam", ro: "Amsterdam" }, continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#AE1C28", "#FFFFFF", "#21468B"] } },
  { id: "poland", name: { en: "Poland", ro: "Polonia" }, capital: { en: "Warsaw", ro: "Varșovia" }, continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#FFFFFF", "#DC143C"] } },
  { id: "monaco", name: { en: "Monaco", ro: "Monaco" }, capital: { en: "Monaco", ro: "Monaco" }, continent: "Europe", difficulty: 4, flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF"] } },
  { id: "indonesia", name: { en: "Indonesia", ro: "Indonezia" }, capital: { en: "Jakarta", ro: "Jakarta" }, continent: "Asia", difficulty: 3, flag: { kind: "horizontal", colors: ["#FF0000", "#FFFFFF"] } },
  { id: "austria", name: { en: "Austria", ro: "Austria" }, capital: { en: "Vienna", ro: "Viena" }, continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#ED2939", "#FFFFFF", "#ED2939"] } },
  { id: "sweden", name: { en: "Sweden", ro: "Suedia" }, capital: { en: "Stockholm", ro: "Stockholm" }, continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#006AA7", cross: "#FECC02" } },
  { id: "norway", name: { en: "Norway", ro: "Norvegia" }, capital: { en: "Oslo", ro: "Oslo" }, continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#EF2B2D", cross: "#002868" } },
  { id: "finland", name: { en: "Finland", ro: "Finlanda" }, capital: { en: "Helsinki", ro: "Helsinki" }, continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#FFFFFF", cross: "#003580" } },
  { id: "denmark", name: { en: "Denmark", ro: "Danemarca" }, capital: { en: "Copenhagen", ro: "Copenhaga" }, continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#C60C30", cross: "#FFFFFF" } },
  { id: "switzerland", name: { en: "Switzerland", ro: "Elveția" }, capital: { en: "Bern", ro: "Berna" }, continent: "Europe", difficulty: 2, flag: { kind: "swiss-cross", field: "#D52B1E", cross: "#FFFFFF" } },
  { id: "japan", name: { en: "Japan", ro: "Japonia" }, capital: { en: "Tokyo", ro: "Tokyo" }, continent: "Asia", difficulty: 1, flag: { kind: "circle", field: "#FFFFFF", circle: "#BC002D" } },
  { id: "bangladesh", name: { en: "Bangladesh", ro: "Bangladesh" }, capital: { en: "Dhaka", ro: "Dhaka" }, continent: "Asia", difficulty: 4, flag: { kind: "circle", field: "#006A4E", circle: "#F42A41" } },
  { id: "turkey", name: { en: "Turkey", ro: "Turcia" }, capital: { en: "Ankara", ro: "Ankara" }, continent: "Asia", difficulty: 2, flag: { kind: "single", color: "#E30A17", overlay: "star-crescent" } },
  { id: "ukraine", name: { en: "Ukraine", ro: "Ucraina" }, capital: { en: "Kyiv", ro: "Kiev" }, continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#0057B7", "#FFD700"] } },
  { id: "usa", name: { en: "United States", ro: "Statele Unite" }, capital: { en: "Washington, D.C.", ro: "Washington, D.C." }, continent: "North America", difficulty: 1, flag: { kind: "canton-stripes", stripeColors: ["#B31942", "#FFFFFF"], cantonColor: "#0A3161" } },
  { id: "uk", name: { en: "United Kingdom", ro: "Marea Britanie" }, capital: { en: "London", ro: "Londra" }, continent: "Europe", difficulty: 1, flag: { kind: "union-jack" } },
  { id: "spain", name: { en: "Spain", ro: "Spania" }, capital: { en: "Madrid", ro: "Madrid" }, continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#AA151B", "#F1BF00", "#AA151B"] } },
  { id: "portugal", name: { en: "Portugal", ro: "Portugalia" }, capital: { en: "Lisbon", ro: "Lisabona" }, continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#006600", "#FF0000"] } },
  { id: "greece", name: { en: "Greece", ro: "Grecia" }, capital: { en: "Athens", ro: "Atena" }, continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#0D5EAF", "#FFFFFF", "#0D5EAF"] } },
  { id: "egypt", name: { en: "Egypt", ro: "Egipt" }, capital: { en: "Cairo", ro: "Cairo" }, continent: "Africa", difficulty: 2, flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF", "#000000"] } },
  { id: "china", name: { en: "China", ro: "China" }, capital: { en: "Beijing", ro: "Beijing" }, continent: "Asia", difficulty: 1, flag: { kind: "single", color: "#DE2910" } },
  { id: "brazil", name: { en: "Brazil", ro: "Brazilia" }, capital: { en: "Brasília", ro: "Brasília" }, continent: "South America", difficulty: 1, flag: { kind: "single", color: "#009739" } },
  { id: "canada", name: { en: "Canada", ro: "Canada" }, capital: { en: "Ottawa", ro: "Ottawa" }, continent: "North America", difficulty: 1, flag: { kind: "vertical", colors: ["#FF0000", "#FFFFFF", "#FF0000"] } },
  { id: "australia", name: { en: "Australia", ro: "Australia" }, capital: { en: "Canberra", ro: "Canberra" }, continent: "Oceania", difficulty: 2, flag: { kind: "single", color: "#00008B" } },
  { id: "india", name: { en: "India", ro: "India" }, capital: { en: "New Delhi", ro: "New Delhi" }, continent: "Asia", difficulty: 1, flag: { kind: "horizontal", colors: ["#FF9933", "#FFFFFF", "#138808"] } },
  { id: "mexico", name: { en: "Mexico", ro: "Mexic" }, capital: { en: "Mexico City", ro: "Ciudad de México" }, continent: "North America", difficulty: 2, flag: { kind: "vertical", colors: ["#006341", "#FFFFFF", "#CE1126"] } },
  { id: "russia", name: { en: "Russia", ro: "Rusia" }, capital: { en: "Moscow", ro: "Moscova" }, continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#FFFFFF", "#0039A6", "#D52B1E"] } },
  { id: "south-korea", name: { en: "South Korea", ro: "Coreea de Sud" }, capital: { en: "Seoul", ro: "Seul" }, continent: "Asia", difficulty: 3, flag: { kind: "single", color: "#FFFFFF" } },
  { id: "argentina", name: { en: "Argentina", ro: "Argentina" }, capital: { en: "Buenos Aires", ro: "Buenos Aires" }, continent: "South America", difficulty: 2, flag: { kind: "horizontal", colors: ["#74ACDF", "#FFFFFF", "#74ACDF"] } },
];

export function getCountryById(id: string): Country | undefined {
  return COUNTRIES.find((c) => c.id === id);
}
