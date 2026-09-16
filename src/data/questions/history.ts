import type { QuizQuestionSource } from "@/types";

export const HISTORY_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "hist-1", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "In which year did World War II end?", correctAnswer: "1945", distractors: ["1943", "1944", "1946"] },
  { id: "hist-2", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "Who was the first President of the United States?", correctAnswer: "George Washington", distractors: ["Thomas Jefferson", "John Adams", "Abraham Lincoln"] },
  { id: "hist-3", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "The ancient pyramids of Giza are located in which country?", correctAnswer: "Egypt", distractors: ["Sudan", "Mexico", "Iraq"] },
  { id: "hist-4", categoryId: "HISTORY", difficulty: 2, renderKind: "TEXT", prompt: "Julius Caesar was a leader of which ancient civilization?", correctAnswer: "Rome", distractors: ["Greece", "Egypt", "Persia"] },
  { id: "hist-5", categoryId: "HISTORY", difficulty: 2, renderKind: "TEXT", prompt: "The Berlin Wall fell in which year?", correctAnswer: "1989", distractors: ["1985", "1991", "1993"] },
  { id: "hist-6", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "Which famous ship sank in 1912 after hitting an iceberg?", correctAnswer: "Titanic", distractors: ["Lusitania", "Britannic", "Bismarck"] },
  { id: "hist-7", categoryId: "HISTORY", difficulty: 2, renderKind: "TEXT", prompt: "Who painted the Mona Lisa?", correctAnswer: "Leonardo da Vinci", distractors: ["Michelangelo", "Raphael", "Donatello"] },
  { id: "hist-8", categoryId: "HISTORY", difficulty: 2, renderKind: "TEXT", prompt: "Which country gifted the Statue of Liberty to the United States?", correctAnswer: "France", distractors: ["United Kingdom", "Spain", "Netherlands"] },
  { id: "hist-9", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "In which country is the Great Wall located?", correctAnswer: "China", distractors: ["Japan", "Mongolia", "India"] },
  { id: "hist-10", categoryId: "HISTORY", difficulty: 3, renderKind: "TEXT", prompt: "Who was known as the \"Maid of Orléans\" during the Hundred Years' War?", correctAnswer: "Joan of Arc", distractors: ["Marie Antoinette", "Catherine de Medici", "Eleanor of Aquitaine"] },
  { id: "hist-11", categoryId: "HISTORY", difficulty: 1, renderKind: "TEXT", prompt: "The Cold War was primarily a rivalry between the United States and which country?", correctAnswer: "Soviet Union", distractors: ["China", "Cuba", "North Korea"] },
  { id: "hist-12", categoryId: "HISTORY", difficulty: 3, renderKind: "TEXT", prompt: "In which century did the Renaissance begin?", correctAnswer: "14th century", distractors: ["12th century", "16th century", "18th century"] },
];
