import type { Difficulty } from "@/types";

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
  name: string;
  capital: string;
  continent: Continent;
  /** 1 = near-universally known, 5 = an easy mix-up even for a strong player (e.g. the Romania/Chad/Andorra/Moldova tricolor family). */
  difficulty: Difficulty;
  flag: FlagPattern;
}

export const COUNTRIES: readonly Country[] = [
  { id: "romania", name: "Romania", capital: "Bucharest", continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#002B7F", "#FCD116", "#CE1126"] } },
  { id: "chad", name: "Chad", capital: "N'Djamena", continent: "Africa", difficulty: 5, flag: { kind: "vertical", colors: ["#002664", "#FECB00", "#C60C30"] } },
  { id: "andorra", name: "Andorra", capital: "Andorra la Vella", continent: "Europe", difficulty: 5, flag: { kind: "vertical", colors: ["#0018A8", "#FEDD00", "#D50032"] } },
  { id: "moldova", name: "Moldova", capital: "Chișinău", continent: "Europe", difficulty: 4, flag: { kind: "vertical", colors: ["#003DA5", "#FFD200", "#CC092F"] } },
  { id: "france", name: "France", capital: "Paris", continent: "Europe", difficulty: 1, flag: { kind: "vertical", colors: ["#0055A4", "#FFFFFF", "#EF4135"] } },
  { id: "italy", name: "Italy", capital: "Rome", continent: "Europe", difficulty: 1, flag: { kind: "vertical", colors: ["#008C45", "#F4F5F0", "#CD212A"] } },
  { id: "ireland", name: "Ireland", capital: "Dublin", continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#169B62", "#FFFFFF", "#FF883E"] } },
  { id: "belgium", name: "Belgium", capital: "Brussels", continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#000000", "#FFD90C", "#EF3340"] } },
  { id: "germany", name: "Germany", capital: "Berlin", continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#000000", "#DD0000", "#FFCE00"] } },
  { id: "netherlands", name: "Netherlands", capital: "Amsterdam", continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#AE1C28", "#FFFFFF", "#21468B"] } },
  { id: "poland", name: "Poland", capital: "Warsaw", continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#FFFFFF", "#DC143C"] } },
  { id: "monaco", name: "Monaco", capital: "Monaco", continent: "Europe", difficulty: 4, flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF"] } },
  { id: "indonesia", name: "Indonesia", capital: "Jakarta", continent: "Asia", difficulty: 3, flag: { kind: "horizontal", colors: ["#FF0000", "#FFFFFF"] } },
  { id: "austria", name: "Austria", capital: "Vienna", continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#ED2939", "#FFFFFF", "#ED2939"] } },
  { id: "sweden", name: "Sweden", capital: "Stockholm", continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#006AA7", cross: "#FECC02" } },
  { id: "norway", name: "Norway", capital: "Oslo", continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#EF2B2D", cross: "#002868" } },
  { id: "finland", name: "Finland", capital: "Helsinki", continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#FFFFFF", cross: "#003580" } },
  { id: "denmark", name: "Denmark", capital: "Copenhagen", continent: "Europe", difficulty: 2, flag: { kind: "nordic-cross", field: "#C60C30", cross: "#FFFFFF" } },
  { id: "switzerland", name: "Switzerland", capital: "Bern", continent: "Europe", difficulty: 2, flag: { kind: "swiss-cross", field: "#D52B1E", cross: "#FFFFFF" } },
  { id: "japan", name: "Japan", capital: "Tokyo", continent: "Asia", difficulty: 1, flag: { kind: "circle", field: "#FFFFFF", circle: "#BC002D" } },
  { id: "bangladesh", name: "Bangladesh", capital: "Dhaka", continent: "Asia", difficulty: 4, flag: { kind: "circle", field: "#006A4E", circle: "#F42A41" } },
  { id: "turkey", name: "Turkey", capital: "Ankara", continent: "Asia", difficulty: 2, flag: { kind: "single", color: "#E30A17", overlay: "star-crescent" } },
  { id: "ukraine", name: "Ukraine", capital: "Kyiv", continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#0057B7", "#FFD700"] } },
  { id: "usa", name: "United States", capital: "Washington, D.C.", continent: "North America", difficulty: 1, flag: { kind: "canton-stripes", stripeColors: ["#B31942", "#FFFFFF"], cantonColor: "#0A3161" } },
  { id: "uk", name: "United Kingdom", capital: "London", continent: "Europe", difficulty: 1, flag: { kind: "union-jack" } },
  { id: "spain", name: "Spain", capital: "Madrid", continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#AA151B", "#F1BF00", "#AA151B"] } },
  { id: "portugal", name: "Portugal", capital: "Lisbon", continent: "Europe", difficulty: 2, flag: { kind: "vertical", colors: ["#006600", "#FF0000"] } },
  { id: "greece", name: "Greece", capital: "Athens", continent: "Europe", difficulty: 2, flag: { kind: "horizontal", colors: ["#0D5EAF", "#FFFFFF", "#0D5EAF"] } },
  { id: "egypt", name: "Egypt", capital: "Cairo", continent: "Africa", difficulty: 2, flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF", "#000000"] } },
  { id: "china", name: "China", capital: "Beijing", continent: "Asia", difficulty: 1, flag: { kind: "single", color: "#DE2910" } },
  { id: "brazil", name: "Brazil", capital: "Brasília", continent: "South America", difficulty: 1, flag: { kind: "single", color: "#009739" } },
  { id: "canada", name: "Canada", capital: "Ottawa", continent: "North America", difficulty: 1, flag: { kind: "vertical", colors: ["#FF0000", "#FFFFFF", "#FF0000"] } },
  { id: "australia", name: "Australia", capital: "Canberra", continent: "Oceania", difficulty: 2, flag: { kind: "single", color: "#00008B" } },
  { id: "india", name: "India", capital: "New Delhi", continent: "Asia", difficulty: 1, flag: { kind: "horizontal", colors: ["#FF9933", "#FFFFFF", "#138808"] } },
  { id: "mexico", name: "Mexico", capital: "Mexico City", continent: "North America", difficulty: 2, flag: { kind: "vertical", colors: ["#006341", "#FFFFFF", "#CE1126"] } },
  { id: "russia", name: "Russia", capital: "Moscow", continent: "Europe", difficulty: 1, flag: { kind: "horizontal", colors: ["#FFFFFF", "#0039A6", "#D52B1E"] } },
  { id: "south-korea", name: "South Korea", capital: "Seoul", continent: "Asia", difficulty: 3, flag: { kind: "single", color: "#FFFFFF" } },
  { id: "argentina", name: "Argentina", capital: "Buenos Aires", continent: "South America", difficulty: 2, flag: { kind: "horizontal", colors: ["#74ACDF", "#FFFFFF", "#74ACDF"] } },
];

export function getCountryById(id: string): Country | undefined {
  return COUNTRIES.find((c) => c.id === id);
}
