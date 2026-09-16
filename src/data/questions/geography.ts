import type { QuizQuestionSource } from "@/types";

export const GEOGRAPHY_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "geo-1", categoryId: "GEOGRAPHY", difficulty: 2, renderKind: "TEXT", prompt: "Which is traditionally cited as the longest river in the world?", correctAnswer: "The Nile", distractors: ["The Amazon", "The Yangtze", "The Mississippi"] },
  { id: "geo-2", categoryId: "GEOGRAPHY", difficulty: 1, renderKind: "TEXT", prompt: "Which is the largest country in the world by area?", correctAnswer: "Russia", distractors: ["Canada", "China", "United States"] },
  { id: "geo-3", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which is the smallest country in the world by area?", correctAnswer: "Vatican City", distractors: ["Monaco", "San Marino", "Liechtenstein"] },
  { id: "geo-4", categoryId: "GEOGRAPHY", difficulty: 2, renderKind: "TEXT", prompt: "Mount Everest sits on the border of Nepal and which other country?", correctAnswer: "China", distractors: ["India", "Bhutan", "Pakistan"] },
  { id: "geo-5", categoryId: "GEOGRAPHY", difficulty: 2, renderKind: "TEXT", prompt: "Which is the largest hot desert in the world?", correctAnswer: "The Sahara", distractors: ["The Gobi", "The Kalahari", "The Arabian Desert"] },
  { id: "geo-6", categoryId: "GEOGRAPHY", difficulty: 1, renderKind: "TEXT", prompt: "The Amazon Rainforest is primarily located on which continent?", correctAnswer: "South America", distractors: ["Africa", "Asia", "Central America"] },
  { id: "geo-7", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which is the largest lake in the world by surface area?", correctAnswer: "The Caspian Sea", distractors: ["Lake Superior", "Lake Victoria", "Lake Baikal"] },
  { id: "geo-8", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which mountain range is often considered the natural border between Europe and Asia?", correctAnswer: "The Ural Mountains", distractors: ["The Alps", "The Andes", "The Himalayas"] },
  { id: "geo-9", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which country is said to have more lakes than the rest of the world combined?", correctAnswer: "Canada", distractors: ["Russia", "United States", "Finland"] },
  { id: "geo-10", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which is the driest permanently inhabited continent?", correctAnswer: "Australia", distractors: ["Africa", "Asia", "South America"] },
  { id: "geo-11", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "Which strait separates Europe and Africa at its narrowest point?", correctAnswer: "The Strait of Gibraltar", distractors: ["The Bosphorus", "The Strait of Hormuz", "The English Channel"] },
  { id: "geo-12", categoryId: "GEOGRAPHY", difficulty: 3, renderKind: "TEXT", prompt: "How many time zones does Russia officially span?", correctAnswer: "11", distractors: ["7", "9", "13"] },
];
