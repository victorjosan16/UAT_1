import type { QuizQuestionSource } from "@/types";

export const SCIENCE_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "sci-1", categoryId: "SCIENCE", difficulty: 2, renderKind: "TEXT", prompt: "What is the chemical symbol for gold?", correctAnswer: "Au", distractors: ["Ag", "Gd", "Go"] },
  { id: "sci-2", categoryId: "SCIENCE", difficulty: 3, renderKind: "TEXT", prompt: "How many bones are there in the adult human body?", correctAnswer: "206", distractors: ["186", "226", "300"] },
  { id: "sci-3", categoryId: "SCIENCE", difficulty: 1, renderKind: "TEXT", prompt: "What gas do plants absorb from the atmosphere for photosynthesis?", correctAnswer: "Carbon dioxide", distractors: ["Oxygen", "Nitrogen", "Hydrogen"] },
  { id: "sci-4", categoryId: "SCIENCE", difficulty: 2, renderKind: "TEXT", prompt: "Which part of the cell is known as its \"powerhouse\"?", correctAnswer: "Mitochondria", distractors: ["Nucleus", "Ribosome", "Golgi apparatus"] },
  { id: "sci-5", categoryId: "SCIENCE", difficulty: 1, renderKind: "TEXT", prompt: "What is the hardest naturally occurring substance on Earth?", correctAnswer: "Diamond", distractors: ["Quartz", "Titanium", "Graphite"] },
  { id: "sci-6", categoryId: "SCIENCE", difficulty: 2, renderKind: "TEXT", prompt: "Which element has the atomic number 1?", correctAnswer: "Hydrogen", distractors: ["Helium", "Oxygen", "Carbon"] },
  { id: "sci-7", categoryId: "SCIENCE", difficulty: 1, renderKind: "TEXT", prompt: "What force keeps the planets in orbit around the Sun?", correctAnswer: "Gravity", distractors: ["Magnetism", "Friction", "Inertia"] },
  { id: "sci-8", categoryId: "SCIENCE", difficulty: 2, renderKind: "TEXT", prompt: "What is the freezing point of water in Fahrenheit?", correctAnswer: "32°F", distractors: ["0°F", "100°F", "212°F"] },
  { id: "sci-9", categoryId: "SCIENCE", difficulty: 3, renderKind: "TEXT", prompt: "Which blood type is known as the universal donor?", correctAnswer: "O negative", distractors: ["AB positive", "A positive", "B negative"] },
  { id: "sci-10", categoryId: "SCIENCE", difficulty: 3, renderKind: "TEXT", prompt: "What is the scientific study of earthquakes called?", correctAnswer: "Seismology", distractors: ["Geology", "Meteorology", "Volcanology"] },
  { id: "sci-11", categoryId: "SCIENCE", difficulty: 1, renderKind: "TEXT", prompt: "How many chambers does the human heart have?", correctAnswer: "4", distractors: ["2", "3", "6"] },
  { id: "sci-12", categoryId: "SCIENCE", difficulty: 2, renderKind: "TEXT", prompt: "What is the most abundant gas in Earth's atmosphere?", correctAnswer: "Nitrogen", distractors: ["Oxygen", "Carbon dioxide", "Argon"] },
];
