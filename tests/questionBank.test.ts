import { describe, expect, it } from "vitest";
import { ALL_QUESTIONS, questionsForCategory } from "@/quiz/QuestionBank";
import { AVAILABLE_CATEGORIES } from "@/data/categories";

const LANGUAGES = ["en", "ro", "es", "pt", "hi", "id", "ru"] as const;

describe("QuestionBank", () => {
  it("has no duplicate question ids across the whole bank", () => {
    const ids = ALL_QUESTIONS.map((q) => q.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("every question has both prompt/correctAnswer languages non-empty", () => {
    for (const q of ALL_QUESTIONS) {
      for (const lang of LANGUAGES) {
        expect(q.prompt[lang].length).toBeGreaterThan(0);
        expect(q.correctAnswer[lang].length).toBeGreaterThan(0);
      }
    }
  });

  it("every question has enough distractors per language, none equal to the correct answer", () => {
    // TRUE_FALSE has exactly one meaningful distractor (the opposite boolean) — every other
    // renderKind still needs a real multiple-choice spread of at least 3.
    for (const q of ALL_QUESTIONS) {
      const minDistractors = q.renderKind === "TRUE_FALSE" ? 1 : 3;
      for (const lang of LANGUAGES) {
        const distractors = q.distractors[lang];
        expect(distractors.length).toBeGreaterThanOrEqual(minDistractors);
        expect(distractors).not.toContain(q.correctAnswer[lang]);
        expect(new Set(distractors).size).toBe(distractors.length);
      }
    }
  });

  it("has the same distractor count in every language for a given question", () => {
    for (const q of ALL_QUESTIONS) {
      for (const lang of LANGUAGES) {
        expect(q.distractors[lang].length).toBe(q.distractors.en.length);
      }
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

  it("TRUE_FALSE questions always have exactly one distractor (the opposite boolean)", () => {
    for (const q of ALL_QUESTIONS) {
      if (q.renderKind !== "TRUE_FALSE") continue;
      for (const lang of LANGUAGES) {
        expect(q.distractors[lang]).toHaveLength(1);
      }
    }
  });
});
