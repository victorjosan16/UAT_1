import type { QuizQuestionSource } from "@/types";

export const ANIMALS_QUESTIONS: readonly QuizQuestionSource[] = [
  { id: "ani-1", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "What is the tallest land animal in the world?", correctAnswer: "Giraffe", distractors: ["Elephant", "Ostrich", "Camel"] },
  { id: "ani-2", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "Which is the largest animal to have ever existed?", correctAnswer: "Blue whale", distractors: ["African elephant", "Sperm whale", "Giant squid"] },
  { id: "ani-3", categoryId: "ANIMALS", difficulty: 2, renderKind: "TEXT", prompt: "Which flightless bird is the fastest runner among birds?", correctAnswer: "Ostrich", distractors: ["Penguin", "Emu", "Kiwi"] },
  { id: "ani-4", categoryId: "ANIMALS", difficulty: 2, renderKind: "TEXT", prompt: "What is a baby kangaroo called?", correctAnswer: "Joey", distractors: ["Cub", "Calf", "Pup"] },
  { id: "ani-5", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "Which big cat is known for having a mane?", correctAnswer: "Lion", distractors: ["Tiger", "Leopard", "Cheetah"] },
  { id: "ani-6", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "How many legs does a spider have?", correctAnswer: "8", distractors: ["6", "10", "12"] },
  { id: "ani-7", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "Which animal is often nicknamed the \"King of the Jungle\"?", correctAnswer: "Lion", distractors: ["Tiger", "Elephant", "Gorilla"] },
  { id: "ani-8", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "What is the fastest land animal in the world?", correctAnswer: "Cheetah", distractors: ["Lion", "Pronghorn", "Greyhound"] },
  { id: "ani-9", categoryId: "ANIMALS", difficulty: 3, renderKind: "TEXT", prompt: "Which mammal is famous for laying eggs instead of giving live birth?", correctAnswer: "Platypus", distractors: ["Kangaroo", "Koala", "Armadillo"] },
  { id: "ani-10", categoryId: "ANIMALS", difficulty: 2, renderKind: "TEXT", prompt: "What do you call a group of lions?", correctAnswer: "Pride", distractors: ["Pack", "Herd", "Flock"] },
  { id: "ani-11", categoryId: "ANIMALS", difficulty: 2, renderKind: "TEXT", prompt: "Which land mammal is well known for living up to 70 years?", correctAnswer: "Elephant", distractors: ["Lion", "Giraffe", "Horse"] },
  { id: "ani-12", categoryId: "ANIMALS", difficulty: 1, renderKind: "TEXT", prompt: "Which insect is well known for producing honey?", correctAnswer: "Bee", distractors: ["Wasp", "Ant", "Butterfly"] },
];
