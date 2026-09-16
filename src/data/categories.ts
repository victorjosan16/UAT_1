import type { Category, CategoryId } from "@/types";

/**
 * The full category list is shown in Discover so the game reads as scalable
 * even before every category has content — `available: false` categories
 * render as "Coming soon" and are excluded from Quick Play / Level Journey
 * question pools. See docs/GAME_DESIGN.md.
 */
export const CATEGORIES: readonly Category[] = [
  {
    id: "GENERAL_KNOWLEDGE",
    label: { en: "General Knowledge", ro: "Cultură Generală", es: "Cultura General", pt: "Cultura Geral", hi: "सामान्य ज्ञान", id: "Pengetahuan Umum", ru: "Общие знания" },
    emoji: "🧠",
    accent: "emerald",
    available: true,
  },
  {
    id: "GEOGRAPHY",
    label: { en: "Geography", ro: "Geografie", es: "Geografía", pt: "Geografia", hi: "भूगोल", id: "Geografi", ru: "География" },
    emoji: "🌍",
    accent: "cyan",
    available: true,
  },
  {
    id: "FLAGS",
    label: { en: "Flags", ro: "Steaguri", es: "Banderas", pt: "Bandeiras", hi: "झंडे", id: "Bendera", ru: "Флаги" },
    emoji: "🚩",
    accent: "coral",
    available: true,
  },
  {
    id: "CAPITALS",
    label: { en: "Capitals", ro: "Capitale", es: "Capitales", pt: "Capitais", hi: "राजधानियाँ", id: "Ibu Kota", ru: "Столицы" },
    emoji: "🏛️",
    accent: "amber",
    available: true,
  },
  {
    id: "HISTORY",
    label: { en: "History", ro: "Istorie", es: "Historia", pt: "História", hi: "इतिहास", id: "Sejarah", ru: "История" },
    emoji: "📜",
    accent: "violet",
    available: true,
  },
  {
    id: "SCIENCE",
    label: { en: "Science", ro: "Știință", es: "Ciencia", pt: "Ciência", hi: "विज्ञान", id: "Sains", ru: "Наука" },
    emoji: "🔬",
    accent: "cyan",
    available: true,
  },
  {
    id: "SPACE",
    label: { en: "Space", ro: "Spațiu", es: "Espacio", pt: "Espaço", hi: "अंतरिक्ष", id: "Luar Angkasa", ru: "Космос" },
    emoji: "🚀",
    accent: "violet",
    available: true,
  },
  {
    id: "ANIMALS",
    label: { en: "Animals", ro: "Animale", es: "Animales", pt: "Animais", hi: "जानवर", id: "Hewan", ru: "Животные" },
    emoji: "🐾",
    accent: "amber",
    available: true,
  },
  {
    id: "NATURE",
    label: { en: "Nature", ro: "Natură", es: "Naturaleza", pt: "Natureza", hi: "प्रकृति", id: "Alam", ru: "Природа" },
    emoji: "🌿",
    accent: "emerald",
    available: false,
  },
  {
    id: "TECHNOLOGY",
    label: { en: "Technology", ro: "Tehnologie", es: "Tecnología", pt: "Tecnologia", hi: "प्रौद्योगिकी", id: "Teknologi", ru: "Технологии" },
    emoji: "💻",
    accent: "cyan",
    available: true,
  },
  {
    id: "INVENTIONS",
    label: { en: "Inventions", ro: "Invenții", es: "Inventos", pt: "Invenções", hi: "आविष्कार", id: "Penemuan", ru: "Изобретения" },
    emoji: "💡",
    accent: "amber",
    available: false,
  },
  {
    id: "ART",
    label: { en: "Art", ro: "Artă", es: "Arte", pt: "Arte", hi: "कला", id: "Seni", ru: "Искусство" },
    emoji: "🎨",
    accent: "coral",
    available: true,
  },
  {
    id: "ARCHITECTURE",
    label: { en: "Architecture", ro: "Arhitectură", es: "Arquitectura", pt: "Arquitetura", hi: "वास्तुकला", id: "Arsitektur", ru: "Архитектура" },
    emoji: "🏗️",
    accent: "violet",
    available: false,
  },
  {
    id: "HUMAN_BODY",
    label: { en: "Human Body", ro: "Corpul Uman", es: "Cuerpo Humano", pt: "Corpo Humano", hi: "मानव शरीर", id: "Tubuh Manusia", ru: "Тело человека" },
    emoji: "🫀",
    accent: "coral",
    available: true,
  },
  {
    id: "MATHEMATICS",
    label: { en: "Mathematics", ro: "Matematică", es: "Matemáticas", pt: "Matemática", hi: "गणित", id: "Matematika", ru: "Математика" },
    emoji: "➗",
    accent: "emerald",
    available: false,
  },
  {
    id: "LOGIC",
    label: { en: "Logic", ro: "Logică", es: "Lógica", pt: "Lógica", hi: "तर्कशास्त्र", id: "Logika", ru: "Логика" },
    emoji: "🧩",
    accent: "violet",
    available: false,
  },
  {
    id: "FOOD",
    label: { en: "World Cuisine", ro: "Bucătăria Lumii", es: "Cocina del Mundo", pt: "Culinária Mundial", hi: "विश्व व्यंजन", id: "Kuliner Dunia", ru: "Кухни мира" },
    emoji: "🍜",
    accent: "amber",
    available: false,
  },
  {
    id: "LANDMARKS",
    label: { en: "Landmarks", ro: "Obiective Turistice", es: "Monumentos", pt: "Pontos Turísticos", hi: "प्रसिद्ध स्थल", id: "Landmark", ru: "Достопримечательности" },
    emoji: "🗽",
    accent: "cyan",
    available: false,
  },
  {
    id: "CULTURES",
    label: { en: "World Cultures", ro: "Culturi ale Lumii", es: "Culturas del Mundo", pt: "Culturas do Mundo", hi: "विश्व की संस्कृतियाँ", id: "Budaya Dunia", ru: "Культуры мира" },
    emoji: "🎎",
    accent: "coral",
    available: false,
  },
  {
    id: "MYSTERY_IMAGE",
    label: { en: "Mystery Image", ro: "Imagine Misterioasă", es: "Imagen Misteriosa", pt: "Imagem Misteriosa", hi: "रहस्यमय छवि", id: "Gambar Misteri", ru: "Загадочное изображение" },
    emoji: "❓",
    accent: "violet",
    available: false,
  },
];

export function getCategory(id: CategoryId): Category {
  const category = CATEGORIES.find((c) => c.id === id);
  if (!category) throw new Error(`Unknown category id "${id}"`);
  return category;
}

export const AVAILABLE_CATEGORIES: readonly Category[] = CATEGORIES.filter((c) => c.available);
