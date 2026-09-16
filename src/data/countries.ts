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
  {
    id: "romania",
    name: { en: "Romania", ro: "România", es: "Rumania", pt: "Romênia", hi: "रोमानिया", id: "Rumania", ru: "Румыния" },
    capital: { en: "Bucharest", ro: "București", es: "Bucarest", pt: "Bucareste", hi: "बुखारेस्ट", id: "Bukares", ru: "Бухарест" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "vertical", colors: ["#002B7F", "#FCD116", "#CE1126"] },
  },
  {
    id: "chad",
    name: { en: "Chad", ro: "Ciad", es: "Chad", pt: "Chade", hi: "चाड", id: "Chad", ru: "Чад" },
    capital: { en: "N'Djamena", ro: "N'Djamena", es: "Yamena", pt: "Ndjamena", hi: "नजामेना", id: "N'Djamena", ru: "Нджамена" },
    continent: "Africa",
    difficulty: 5,
    flag: { kind: "vertical", colors: ["#002664", "#FECB00", "#C60C30"] },
  },
  {
    id: "andorra",
    name: { en: "Andorra", ro: "Andorra", es: "Andorra", pt: "Andorra", hi: "अंडोरा", id: "Andorra", ru: "Андорра" },
    capital: { en: "Andorra la Vella", ro: "Andorra la Vella", es: "Andorra la Vella", pt: "Andorra-a-Velha", hi: "अंडोरा ला वेला", id: "Andorra la Vella", ru: "Андорра-ла-Велья" },
    continent: "Europe",
    difficulty: 5,
    flag: { kind: "vertical", colors: ["#0018A8", "#FEDD00", "#D50032"] },
  },
  {
    id: "moldova",
    name: { en: "Moldova", ro: "Moldova", es: "Moldavia", pt: "Moldávia", hi: "मोल्दोवा", id: "Moldova", ru: "Молдова" },
    capital: { en: "Chisinau", ro: "Chișinău", es: "Chisináu", pt: "Chisinau", hi: "चिसीनाउ", id: "Chisinau", ru: "Кишинёв" },
    continent: "Europe",
    difficulty: 4,
    flag: { kind: "vertical", colors: ["#003DA5", "#FFD200", "#CC092F"] },
  },
  {
    id: "france",
    name: { en: "France", ro: "Franța", es: "Francia", pt: "França", hi: "फ़्रांस", id: "Prancis", ru: "Франция" },
    capital: { en: "Paris", ro: "Paris", es: "París", pt: "Paris", hi: "पेरिस", id: "Paris", ru: "Париж" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "vertical", colors: ["#0055A4", "#FFFFFF", "#EF4135"] },
  },
  {
    id: "italy",
    name: { en: "Italy", ro: "Italia", es: "Italia", pt: "Itália", hi: "इटली", id: "Italia", ru: "Италия" },
    capital: { en: "Rome", ro: "Roma", es: "Roma", pt: "Roma", hi: "रोम", id: "Roma", ru: "Рим" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "vertical", colors: ["#008C45", "#F4F5F0", "#CD212A"] },
  },
  {
    id: "ireland",
    name: { en: "Ireland", ro: "Irlanda", es: "Irlanda", pt: "Irlanda", hi: "आयरलैंड", id: "Irlandia", ru: "Ирландия" },
    capital: { en: "Dublin", ro: "Dublin", es: "Dublín", pt: "Dublin", hi: "डबलिन", id: "Dublin", ru: "Дублин" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "vertical", colors: ["#169B62", "#FFFFFF", "#FF883E"] },
  },
  {
    id: "belgium",
    name: { en: "Belgium", ro: "Belgia", es: "Bélgica", pt: "Bélgica", hi: "बेल्जियम", id: "Belgia", ru: "Бельгия" },
    capital: { en: "Brussels", ro: "Bruxelles", es: "Bruselas", pt: "Bruxelas", hi: "ब्रुसेल्स", id: "Brussels", ru: "Брюссель" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "vertical", colors: ["#000000", "#FFD90C", "#EF3340"] },
  },
  {
    id: "germany",
    name: { en: "Germany", ro: "Germania", es: "Alemania", pt: "Alemanha", hi: "जर्मनी", id: "Jerman", ru: "Германия" },
    capital: { en: "Berlin", ro: "Berlin", es: "Berlín", pt: "Berlim", hi: "बर्लिन", id: "Berlin", ru: "Берлин" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "horizontal", colors: ["#000000", "#DD0000", "#FFCE00"] },
  },
  {
    id: "netherlands",
    name: { en: "Netherlands", ro: "Olanda", es: "Países Bajos", pt: "Países Baixos", hi: "नीदरलैंड", id: "Belanda", ru: "Нидерланды" },
    capital: { en: "Amsterdam", ro: "Amsterdam", es: "Ámsterdam", pt: "Amsterdã", hi: "एम्स्टर्डम", id: "Amsterdam", ru: "Амстердам" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#AE1C28", "#FFFFFF", "#21468B"] },
  },
  {
    id: "poland",
    name: { en: "Poland", ro: "Polonia", es: "Polonia", pt: "Polônia", hi: "पोलैंड", id: "Polandia", ru: "Польша" },
    capital: { en: "Warsaw", ro: "Varșovia", es: "Varsovia", pt: "Varsóvia", hi: "वारसॉ", id: "Warsawa", ru: "Варшава" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#FFFFFF", "#DC143C"] },
  },
  {
    id: "monaco",
    name: { en: "Monaco", ro: "Monaco", es: "Mónaco", pt: "Mônaco", hi: "मोनाको", id: "Monako", ru: "Монако" },
    capital: { en: "Monaco", ro: "Monaco", es: "Mónaco", pt: "Mônaco", hi: "मोनाको", id: "Monako", ru: "Монако" },
    continent: "Europe",
    difficulty: 4,
    flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF"] },
  },
  {
    id: "indonesia",
    name: { en: "Indonesia", ro: "Indonezia", es: "Indonesia", pt: "Indonésia", hi: "इंडोनेशिया", id: "Indonesia", ru: "Индонезия" },
    capital: { en: "Jakarta", ro: "Jakarta", es: "Yakarta", pt: "Jacarta", hi: "जकार्ता", id: "Jakarta", ru: "Джакарта" },
    continent: "Asia",
    difficulty: 3,
    flag: { kind: "horizontal", colors: ["#FF0000", "#FFFFFF"] },
  },
  {
    id: "austria",
    name: { en: "Austria", ro: "Austria", es: "Austria", pt: "Áustria", hi: "ऑस्ट्रिया", id: "Austria", ru: "Австрия" },
    capital: { en: "Vienna", ro: "Viena", es: "Viena", pt: "Viena", hi: "विएना", id: "Wina", ru: "Вена" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#ED2939", "#FFFFFF", "#ED2939"] },
  },
  {
    id: "sweden",
    name: { en: "Sweden", ro: "Suedia", es: "Suecia", pt: "Suécia", hi: "स्वीडन", id: "Swedia", ru: "Швеция" },
    capital: { en: "Stockholm", ro: "Stockholm", es: "Estocolmo", pt: "Estocolmo", hi: "स्टॉकहोम", id: "Stockholm", ru: "Стокгольм" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "nordic-cross", field: "#006AA7", cross: "#FECC02" },
  },
  {
    id: "norway",
    name: { en: "Norway", ro: "Norvegia", es: "Noruega", pt: "Noruega", hi: "नॉर्वे", id: "Norwegia", ru: "Норвегия" },
    capital: { en: "Oslo", ro: "Oslo", es: "Oslo", pt: "Oslo", hi: "ओस्लो", id: "Oslo", ru: "Осло" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "nordic-cross", field: "#EF2B2D", cross: "#002868" },
  },
  {
    id: "finland",
    name: { en: "Finland", ro: "Finlanda", es: "Finlandia", pt: "Finlândia", hi: "फ़िनलैंड", id: "Finlandia", ru: "Финляндия" },
    capital: { en: "Helsinki", ro: "Helsinki", es: "Helsinki", pt: "Helsinque", hi: "हेलसिंकी", id: "Helsinki", ru: "Хельсинки" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "nordic-cross", field: "#FFFFFF", cross: "#003580" },
  },
  {
    id: "denmark",
    name: { en: "Denmark", ro: "Danemarca", es: "Dinamarca", pt: "Dinamarca", hi: "डेनमार्क", id: "Denmark", ru: "Дания" },
    capital: { en: "Copenhagen", ro: "Copenhaga", es: "Copenhague", pt: "Copenhaga", hi: "कोपेनहेगन", id: "Kopenhagen", ru: "Копенгаген" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "nordic-cross", field: "#C60C30", cross: "#FFFFFF" },
  },
  {
    id: "switzerland",
    name: { en: "Switzerland", ro: "Elveția", es: "Suiza", pt: "Suíça", hi: "स्विट्ज़रलैंड", id: "Swiss", ru: "Швейцария" },
    capital: { en: "Bern", ro: "Berna", es: "Berna", pt: "Berna", hi: "बर्न", id: "Bern", ru: "Берн" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "swiss-cross", field: "#D52B1E", cross: "#FFFFFF" },
  },
  {
    id: "japan",
    name: { en: "Japan", ro: "Japonia", es: "Japón", pt: "Japão", hi: "जापान", id: "Jepang", ru: "Япония" },
    capital: { en: "Tokyo", ro: "Tokyo", es: "Tokio", pt: "Tóquio", hi: "टोक्यो", id: "Tokyo", ru: "Токио" },
    continent: "Asia",
    difficulty: 1,
    flag: { kind: "circle", field: "#FFFFFF", circle: "#BC002D" },
  },
  {
    id: "bangladesh",
    name: { en: "Bangladesh", ro: "Bangladesh", es: "Bangladés", pt: "Bangladesh", hi: "बांग्लादेश", id: "Bangladesh", ru: "Бангладеш" },
    capital: { en: "Dhaka", ro: "Dhaka", es: "Daca", pt: "Daca", hi: "ढाका", id: "Dhaka", ru: "Дакка" },
    continent: "Asia",
    difficulty: 4,
    flag: { kind: "circle", field: "#006A4E", circle: "#F42A41" },
  },
  {
    id: "turkey",
    name: { en: "Turkey", ro: "Turcia", es: "Turquía", pt: "Turquia", hi: "तुर्की", id: "Turki", ru: "Турция" },
    capital: { en: "Ankara", ro: "Ankara", es: "Ankara", pt: "Ancara", hi: "अंकारा", id: "Ankara", ru: "Анкара" },
    continent: "Asia",
    difficulty: 2,
    flag: { kind: "single", color: "#E30A17", overlay: "star-crescent" },
  },
  {
    id: "ukraine",
    name: { en: "Ukraine", ro: "Ucraina", es: "Ucrania", pt: "Ucrânia", hi: "यूक्रेन", id: "Ukraina", ru: "Украина" },
    capital: { en: "Kyiv", ro: "Kiev", es: "Kiev", pt: "Kiev", hi: "कीव", id: "Kyiv", ru: "Киев" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#0057B7", "#FFD700"] },
  },
  {
    id: "usa",
    name: {
      en: "United States",
      ro: "Statele Unite",
      es: "Estados Unidos",
      pt: "Estados Unidos",
      hi: "संयुक्त राज्य अमेरिका",
      id: "Amerika Serikat",
      ru: "США",
    },
    capital: {
      en: "Washington, D.C.",
      ro: "Washington, D.C.",
      es: "Washington D. C.",
      pt: "Washington, D.C.",
      hi: "वाशिंगटन डी.सी.",
      id: "Washington, D.C.",
      ru: "Вашингтон",
    },
    continent: "North America",
    difficulty: 1,
    flag: { kind: "canton-stripes", stripeColors: ["#B31942", "#FFFFFF"], cantonColor: "#0A3161" },
  },
  {
    id: "uk",
    name: { en: "United Kingdom", ro: "Marea Britanie", es: "Reino Unido", pt: "Reino Unido", hi: "यूनाइटेड किंगडम", id: "Britania Raya", ru: "Великобритания" },
    capital: { en: "London", ro: "Londra", es: "Londres", pt: "Londres", hi: "लंदन", id: "London", ru: "Лондон" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "union-jack" },
  },
  {
    id: "spain",
    name: { en: "Spain", ro: "Spania", es: "España", pt: "Espanha", hi: "स्पेन", id: "Spanyol", ru: "Испания" },
    capital: { en: "Madrid", ro: "Madrid", es: "Madrid", pt: "Madri", hi: "मैड्रिड", id: "Madrid", ru: "Мадрид" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "horizontal", colors: ["#AA151B", "#F1BF00", "#AA151B"] },
  },
  {
    id: "portugal",
    name: { en: "Portugal", ro: "Portugalia", es: "Portugal", pt: "Portugal", hi: "पुर्तगाल", id: "Portugal", ru: "Португалия" },
    capital: { en: "Lisbon", ro: "Lisabona", es: "Lisboa", pt: "Lisboa", hi: "लिस्बन", id: "Lisbon", ru: "Лиссабон" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "vertical", colors: ["#006600", "#FF0000"] },
  },
  {
    id: "greece",
    name: { en: "Greece", ro: "Grecia", es: "Grecia", pt: "Grécia", hi: "ग्रीस", id: "Yunani", ru: "Греция" },
    capital: { en: "Athens", ro: "Atena", es: "Atenas", pt: "Atenas", hi: "एथेंस", id: "Athena", ru: "Афины" },
    continent: "Europe",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#0D5EAF", "#FFFFFF", "#0D5EAF"] },
  },
  {
    id: "egypt",
    name: { en: "Egypt", ro: "Egipt", es: "Egipto", pt: "Egito", hi: "मिस्र", id: "Mesir", ru: "Египет" },
    capital: { en: "Cairo", ro: "Cairo", es: "El Cairo", pt: "Cairo", hi: "काहिरा", id: "Kairo", ru: "Каир" },
    continent: "Africa",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#CE1126", "#FFFFFF", "#000000"] },
  },
  {
    id: "china",
    name: { en: "China", ro: "China", es: "China", pt: "China", hi: "चीन", id: "Tiongkok", ru: "Китай" },
    capital: { en: "Beijing", ro: "Beijing", es: "Pekín", pt: "Pequim", hi: "बीजिंग", id: "Beijing", ru: "Пекин" },
    continent: "Asia",
    difficulty: 1,
    flag: { kind: "single", color: "#DE2910" },
  },
  {
    id: "brazil",
    name: { en: "Brazil", ro: "Brazilia", es: "Brasil", pt: "Brasil", hi: "ब्राज़ील", id: "Brasil", ru: "Бразилия" },
    capital: { en: "Brasília", ro: "Brasília", es: "Brasilia", pt: "Brasília", hi: "ब्रासीलिया", id: "Brasília", ru: "Бразилиа" },
    continent: "South America",
    difficulty: 1,
    flag: { kind: "single", color: "#009739" },
  },
  {
    id: "canada",
    name: { en: "Canada", ro: "Canada", es: "Canadá", pt: "Canadá", hi: "कनाडा", id: "Kanada", ru: "Канада" },
    capital: { en: "Ottawa", ro: "Ottawa", es: "Ottawa", pt: "Ottawa", hi: "ओटावा", id: "Ottawa", ru: "Оттава" },
    continent: "North America",
    difficulty: 1,
    flag: { kind: "vertical", colors: ["#FF0000", "#FFFFFF", "#FF0000"] },
  },
  {
    id: "australia",
    name: { en: "Australia", ro: "Australia", es: "Australia", pt: "Austrália", hi: "ऑस्ट्रेलिया", id: "Australia", ru: "Австралия" },
    capital: { en: "Canberra", ro: "Canberra", es: "Canberra", pt: "Camberra", hi: "कैनबरा", id: "Canberra", ru: "Канберра" },
    continent: "Oceania",
    difficulty: 2,
    flag: { kind: "single", color: "#00008B" },
  },
  {
    id: "india",
    name: { en: "India", ro: "India", es: "India", pt: "Índia", hi: "भारत", id: "India", ru: "Индия" },
    capital: { en: "New Delhi", ro: "New Delhi", es: "Nueva Delhi", pt: "Nova Deli", hi: "नई दिल्ली", id: "New Delhi", ru: "Нью-Дели" },
    continent: "Asia",
    difficulty: 1,
    flag: { kind: "horizontal", colors: ["#FF9933", "#FFFFFF", "#138808"] },
  },
  {
    id: "mexico",
    name: { en: "Mexico", ro: "Mexic", es: "México", pt: "México", hi: "मेक्सिको", id: "Meksiko", ru: "Мексика" },
    capital: { en: "Mexico City", ro: "Ciudad de México", es: "Ciudad de México", pt: "Cidade do México", hi: "मेक्सिको सिटी", id: "Kota Meksiko", ru: "Мехико" },
    continent: "North America",
    difficulty: 2,
    flag: { kind: "vertical", colors: ["#006341", "#FFFFFF", "#CE1126"] },
  },
  {
    id: "russia",
    name: { en: "Russia", ro: "Rusia", es: "Rusia", pt: "Rússia", hi: "रूस", id: "Rusia", ru: "Россия" },
    capital: { en: "Moscow", ro: "Moscova", es: "Moscú", pt: "Moscou", hi: "मॉस्को", id: "Moskow", ru: "Москва" },
    continent: "Europe",
    difficulty: 1,
    flag: { kind: "horizontal", colors: ["#FFFFFF", "#0039A6", "#D52B1E"] },
  },
  {
    id: "south-korea",
    name: { en: "South Korea", ro: "Coreea de Sud", es: "Corea del Sur", pt: "Coreia do Sul", hi: "दक्षिण कोरिया", id: "Korea Selatan", ru: "Южная Корея" },
    capital: { en: "Seoul", ro: "Seul", es: "Seúl", pt: "Seul", hi: "सियोल", id: "Seoul", ru: "Сеул" },
    continent: "Asia",
    difficulty: 3,
    flag: { kind: "single", color: "#FFFFFF" },
  },
  {
    id: "argentina",
    name: { en: "Argentina", ro: "Argentina", es: "Argentina", pt: "Argentina", hi: "अर्जेंटीना", id: "Argentina", ru: "Аргентина" },
    capital: { en: "Buenos Aires", ro: "Buenos Aires", es: "Buenos Aires", pt: "Buenos Aires", hi: "ब्यूनस आयर्स", id: "Buenos Aires", ru: "Буэнос-Айрес" },
    continent: "South America",
    difficulty: 2,
    flag: { kind: "horizontal", colors: ["#74ACDF", "#FFFFFF", "#74ACDF"] },
  },
];

export function getCountryById(id: string): Country | undefined {
  return COUNTRIES.find((c) => c.id === id);
}
