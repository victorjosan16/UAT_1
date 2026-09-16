import type { Category, CategoryId } from "@/types";

/**
 * The full category list is shown in Discover so the game reads as scalable
 * even before every category has content — `available: false` categories
 * render as "Coming soon" and are excluded from Quick Play / Level Journey
 * question pools. See docs/GAME_DESIGN.md.
 */
export const CATEGORIES: readonly Category[] = [
  { id: "GENERAL_KNOWLEDGE", label: { en: "General Knowledge", ro: "Cultură Generală" }, emoji: "🧠", accent: "emerald", available: true },
  { id: "GEOGRAPHY", label: { en: "Geography", ro: "Geografie" }, emoji: "🌍", accent: "cyan", available: true },
  { id: "FLAGS", label: { en: "Flags", ro: "Steaguri" }, emoji: "🚩", accent: "coral", available: true },
  { id: "CAPITALS", label: { en: "Capitals", ro: "Capitale" }, emoji: "🏛️", accent: "amber", available: true },
  { id: "HISTORY", label: { en: "History", ro: "Istorie" }, emoji: "📜", accent: "violet", available: true },
  { id: "SCIENCE", label: { en: "Science", ro: "Știință" }, emoji: "🔬", accent: "cyan", available: true },
  { id: "SPACE", label: { en: "Space", ro: "Spațiu" }, emoji: "🚀", accent: "violet", available: true },
  { id: "ANIMALS", label: { en: "Animals", ro: "Animale" }, emoji: "🐾", accent: "amber", available: true },
  { id: "NATURE", label: { en: "Nature", ro: "Natură" }, emoji: "🌿", accent: "emerald", available: false },
  { id: "TECHNOLOGY", label: { en: "Technology", ro: "Tehnologie" }, emoji: "💻", accent: "cyan", available: false },
  { id: "INVENTIONS", label: { en: "Inventions", ro: "Invenții" }, emoji: "💡", accent: "amber", available: false },
  { id: "ART", label: { en: "Art", ro: "Artă" }, emoji: "🎨", accent: "coral", available: false },
  { id: "ARCHITECTURE", label: { en: "Architecture", ro: "Arhitectură" }, emoji: "🏗️", accent: "violet", available: false },
  { id: "HUMAN_BODY", label: { en: "Human Body", ro: "Corpul Uman" }, emoji: "🫀", accent: "coral", available: false },
  { id: "MATHEMATICS", label: { en: "Mathematics", ro: "Matematică" }, emoji: "➗", accent: "emerald", available: false },
  { id: "LOGIC", label: { en: "Logic", ro: "Logică" }, emoji: "🧩", accent: "violet", available: false },
  { id: "FOOD", label: { en: "World Cuisine", ro: "Bucătăria Lumii" }, emoji: "🍜", accent: "amber", available: false },
  { id: "LANDMARKS", label: { en: "Landmarks", ro: "Obiective Turistice" }, emoji: "🗽", accent: "cyan", available: false },
  { id: "CULTURES", label: { en: "World Cultures", ro: "Culturi ale Lumii" }, emoji: "🎎", accent: "coral", available: false },
  { id: "MYSTERY_IMAGE", label: { en: "Mystery Image", ro: "Imagine Misterioasă" }, emoji: "❓", accent: "violet", available: false },
];

export function getCategory(id: CategoryId): Category {
  const category = CATEGORIES.find((c) => c.id === id);
  if (!category) throw new Error(`Unknown category id "${id}"`);
  return category;
}

export const AVAILABLE_CATEGORIES: readonly Category[] = CATEGORIES.filter((c) => c.available);
