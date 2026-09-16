import { describe, expect, it } from "vitest";
import { ALL_QUESTIONS, questionsForCategory } from "@/quiz/QuestionBank";
import { AVAILABLE_CATEGORIES } from "@/data/categories";

describe("QuestionBank", () => {
  it("has no duplicate question ids across the whole bank", () => {
    const ids = ALL_QUESTIONS.map((q) => q.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("every question has at least 3 distractors, none equal to the correct answer", () => {
    for (const q of ALL_QUESTIONS) {
      expect(q.distractors.length).toBeGreaterThanOrEqual(3);
      expect(q.distractors).not.toContain(q.correctAnswer);
      expect(new Set(q.distractors).size).toBe(q.distractors.length);
    }
  });

  it("every available category has at least 10 questions", () => {
    for (const category of AVAILABLE_CATEGORIES) {
      expect(questionsForCategory(category.id).length).toBeGreaterThanOrEqual(10);
    }
  });

  it("every question belongs to the category its bank is keyed under", () => {
    for (const category of AVAILABLE_CATEGORIES) {
      for (const q of questionsForCategory(category.id)) {
        expect(q.categoryId).toBe(category.id);
      }
    }
  });

  it("FLAG questions always carry a flagCountryId", () => {
    for (const q of ALL_QUESTIONS) {
      if (q.renderKind === "FLAG") expect(q.flagCountryId).toBeTruthy();
    }
  });
});
