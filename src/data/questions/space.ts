import type { QuizQuestionSource } from "@/types";

export const SPACE_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "space-1", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "Which planet is known as the Red Planet?", correctAnswer: "Mars", distractors: ["Venus", "Jupiter", "Mercury"] },
  { id: "space-2", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "What is the closest star to Earth?", correctAnswer: "The Sun", distractors: ["Proxima Centauri", "Sirius", "Alpha Centauri"] },
  { id: "space-3", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "Which is the largest planet in our solar system?", correctAnswer: "Jupiter", distractors: ["Saturn", "Neptune", "Earth"] },
  { id: "space-4", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "What is the name of the galaxy that contains our solar system?", correctAnswer: "The Milky Way", distractors: ["Andromeda", "Triangulum", "Whirlpool Galaxy"] },
  { id: "space-5", categoryId: "SPACE", difficulty: 3, renderKind: "TEXT", prompt: "What was the first man-made satellite launched into space?", correctAnswer: "Sputnik 1", distractors: ["Apollo 11", "Voyager 1", "Hubble"] },
  { id: "space-6", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "Who was the first human to walk on the Moon?", correctAnswer: "Neil Armstrong", distractors: ["Buzz Aldrin", "Yuri Gagarin", "John Glenn"] },
  { id: "space-7", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "Which planet is famous for its prominent ring system?", correctAnswer: "Saturn", distractors: ["Jupiter", "Uranus", "Neptune"] },
  { id: "space-8", categoryId: "SPACE", difficulty: 2, renderKind: "TEXT", prompt: "What do we call a group of stars that forms a recognizable pattern?", correctAnswer: "Constellation", distractors: ["Galaxy", "Nebula", "Cluster"] },
  { id: "space-9", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "How many planets are in our solar system today?", correctAnswer: "8", distractors: ["7", "9", "10"] },
  { id: "space-10", categoryId: "SPACE", difficulty: 2, renderKind: "TEXT", prompt: "What do we call a star that explodes violently at the end of its life?", correctAnswer: "Supernova", distractors: ["Nova", "Pulsar", "Quasar"] },
  { id: "space-11", categoryId: "SPACE", difficulty: 2, renderKind: "TEXT", prompt: "Which space agency landed the first successful rovers on Mars?", correctAnswer: "NASA", distractors: ["ESA", "Roscosmos", "CNSA"] },
  { id: "space-12", categoryId: "SPACE", difficulty: 1, renderKind: "TEXT", prompt: "What is the name of Earth's only natural satellite?", correctAnswer: "The Moon", distractors: ["Europa", "Titan", "Phobos"] },
];
