import type { QuizQuestionSource } from "@/types";

export const GENERAL_KNOWLEDGE_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "gk-1", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "How many continents are there on Earth?", correctAnswer: "7", distractors: ["5", "6", "8"] },
  { id: "gk-2", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "What is the largest ocean on Earth?", correctAnswer: "Pacific Ocean", distractors: ["Atlantic Ocean", "Indian Ocean", "Arctic Ocean"] },
  { id: "gk-3", categoryId: "GENERAL_KNOWLEDGE", difficulty: 3, renderKind: "TEXT", prompt: "Which language has the most native speakers worldwide?", correctAnswer: "Mandarin Chinese", distractors: ["English", "Spanish", "Hindi"] },
  { id: "gk-4", categoryId: "GENERAL_KNOWLEDGE", difficulty: 2, renderKind: "TEXT", prompt: "What is the official currency of Japan?", correctAnswer: "Yen", distractors: ["Won", "Yuan", "Ringgit"] },
  { id: "gk-5", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "How many players from each team are on the field at once in football (soccer)?", correctAnswer: "11", distractors: ["9", "10", "12"] },
  { id: "gk-6", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "How many colors are there in a rainbow?", correctAnswer: "7", distractors: ["5", "6", "8"] },
  { id: "gk-7", categoryId: "GENERAL_KNOWLEDGE", difficulty: 2, renderKind: "TEXT", prompt: "What is the smallest prime number?", correctAnswer: "2", distractors: ["0", "1", "3"] },
  { id: "gk-8", categoryId: "GENERAL_KNOWLEDGE", difficulty: 3, renderKind: "TEXT", prompt: "What is widely considered the national sport of Japan?", correctAnswer: "Sumo wrestling", distractors: ["Judo", "Baseball", "Karate"] },
  { id: "gk-9", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "How many strings does a standard guitar have?", correctAnswer: "6", distractors: ["4", "5", "7"] },
  { id: "gk-10", categoryId: "GENERAL_KNOWLEDGE", difficulty: 2, renderKind: "TEXT", prompt: "What is the most widely spoken official language in Brazil?", correctAnswer: "Portuguese", distractors: ["Spanish", "English", "French"] },
  { id: "gk-11", categoryId: "GENERAL_KNOWLEDGE", difficulty: 3, renderKind: "TEXT", prompt: "Which is the largest desert in the world, including cold deserts?", correctAnswer: "Antarctica", distractors: ["Sahara", "Arabian Desert", "Gobi Desert"] },
  { id: "gk-12", categoryId: "GENERAL_KNOWLEDGE", difficulty: 1, renderKind: "TEXT", prompt: "What is the boiling point of water at sea level, in Celsius?", correctAnswer: "100°C", distractors: ["90°C", "110°C", "120°C"] },
];
