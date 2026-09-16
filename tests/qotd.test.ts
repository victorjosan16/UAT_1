import { describe, expect, it } from "vitest";
import { pickQotdQuestion } from "@/utils/qotd";

describe("pickQotdQuestion", () => {
  it("is deterministic — the same date key always picks the same question and option order", () => {
    const a = pickQotdQuestion("2026-09-16", "en");
    const b = pickQotdQuestion("2026-09-16", "en");
    expect(a.source.id).toBe(b.source.id);
    expect(a.options).toEqual(b.options);
    expect(a.correctIndex).toBe(b.correctIndex);
  });

  it("different days can pick different questions", () => {
    const seenIds = new Set<string>();
    for (let day = 1; day <= 15; day++) {
      seenIds.add(pickQotdQuestion(`2026-09-${String(day).padStart(2, "0")}`, "en").source.id);
    }
    expect(seenIds.size).toBeGreaterThan(1);
  });

  it("resolves prompt/options into the requested language", () => {
    const en = pickQotdQuestion("2026-09-16", "en");
    const ro = pickQotdQuestion("2026-09-16", "ro");
    expect(en.source.id).toBe(ro.source.id);
    expect(en.prompt).toBe(en.source.prompt.en);
    expect(ro.prompt).toBe(ro.source.prompt.ro);
  });

  it("always includes the correct answer among the options", () => {
    const q = pickQotdQuestion("2026-09-16", "en");
    expect(q.options[q.correctIndex]).toBe(q.source.correctAnswer.en);
  });
});
