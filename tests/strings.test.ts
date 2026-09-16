import { describe, expect, it } from "vitest";
import { STRING_KEYS, translate } from "@/i18n/strings";
import type { Language } from "@/types";

const LANGUAGES: readonly Language[] = ["en", "ro", "es", "pt", "hi", "id", "ru"];

describe("i18n strings", () => {
  it("has non-empty text for every key in every supported language", () => {
    for (const key of STRING_KEYS) {
      for (const lang of LANGUAGES) {
        expect(translate(key, lang).length).toBeGreaterThan(0);
      }
    }
  });

  it("substitutes placeholder variables", () => {
    expect(translate("home.greeting", "en", { name: "Alex" })).toBe("Hi, Alex");
    expect(translate("home.greeting", "ro", { name: "Alex" })).toBe("Salut, Alex");
  });

  it("leaves unmatched placeholders untouched", () => {
    expect(translate("home.greeting", "en")).toBe("Hi, {name}");
  });
});
