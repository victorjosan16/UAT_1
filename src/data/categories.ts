import type { Category, CategoryId } from "@/types";

/**
 * The full category list is shown in Discover so the game reads as scalable
 * even before every category has content — `available: false` categories
 * render as "Coming soon" and are excluded from Quick Play / Level Journey
 * question pools. See docs/GAME_DESIGN.md.
 */
export const CATEGORIES: readonly Category[] = [
  { id: "GENERAL_KNOWLEDGE", label: "General Knowledge", emoji: "🧠", accent: "emerald", available: true },
  { id: "GEOGRAPHY", label: "Geography", emoji: "🌍", accent: "cyan", available: true },
  { id: "FLAGS", label: "Flags", emoji: "🚩", accent: "coral", available: true },
  { id: "CAPITALS", label: "Capitals", emoji: "🏛️", accent: "amber", available: true },
  { id: "HISTORY", label: "History", emoji: "📜", accent: "violet", available: true },
  { id: "SCIENCE", label: "Science", emoji: "🔬", accent: "cyan", available: true },
  { id: "SPACE", label: "Space", emoji: "🚀", accent: "violet", available: true },
  { id: "ANIMALS", label: "Animals", emoji: "🐾", accent: "amber", available: true },
  { id: "NATURE", label: "Nature", emoji: "🌿", accent: "emerald", available: false },
  { id: "TECHNOLOGY", label: "Technology", emoji: "💻", accent: "cyan", available: false },
  { id: "INVENTIONS", label: "Inventions", emoji: "💡", accent: "amber", available: false },
  { id: "ART", label: "Art", emoji: "🎨", accent: "coral", available: false },
  { id: "ARCHITECTURE", label: "Architecture", emoji: "🏗️", accent: "violet", available: false },
  { id: "HUMAN_BODY", label: "Human Body", emoji: "🫀", accent: "coral", available: false },
  { id: "MATHEMATICS", label: "Mathematics", emoji: "➗", accent: "emerald", available: false },
  { id: "LOGIC", label: "Logic", emoji: "🧩", accent: "violet", available: false },
  { id: "FOOD", label: "World Cuisine", emoji: "🍜", accent: "amber", available: false },
  { id: "LANDMARKS", label: "Landmarks", emoji: "🗽", accent: "cyan", available: false },
  { id: "CULTURES", label: "World Cultures", emoji: "🎎", accent: "coral", available: false },
  { id: "MYSTERY_IMAGE", label: "Mystery Image", emoji: "❓", accent: "violet", available: false },
];

export function getCategory(id: CategoryId): Category {
  const category = CATEGORIES.find((c) => c.id === id);
  if (!category) throw new Error(`Unknown category id "${id}"`);
  return category;
}

export const AVAILABLE_CATEGORIES: readonly Category[] = CATEGORIES.filter((c) => c.available);
