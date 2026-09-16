import { describe, expect, it } from "vitest";
import { evaluateMilestones, milestoneKey } from "@/services/MilestoneService";

describe("evaluateMilestones", () => {
  it("celebrates Top 100 the first time a rank of 100 or better is reached", () => {
    const result = evaluateMilestones("allTime", 97, new Set());
    expect(result.celebrate).toBe(100);
    expect(result.toMark).toEqual([milestoneKey("allTime", 100)]);
  });

  it("does not celebrate a rank outside every threshold", () => {
    const result = evaluateMilestones("allTime", 15000, new Set());
    expect(result.celebrate).toBeNull();
    expect(result.toMark).toEqual([]);
  });

  it("never re-celebrates a threshold already marked reached", () => {
    const already = new Set([milestoneKey("allTime", 100)]);
    const result = evaluateMilestones("allTime", 80, already);
    expect(result.celebrate).toBeNull();
    expect(result.toMark).toEqual([]);
  });

  it("celebrates the more exclusive Top 50 even if Top 100 was already claimed", () => {
    const already = new Set([milestoneKey("allTime", 100)]);
    const result = evaluateMilestones("allTime", 42, already);
    expect(result.celebrate).toBe(50);
    expect(result.toMark).toEqual([milestoneKey("allTime", 50)]);
  });

  it("a single huge jump straight to #1 marks every threshold but celebrates only #1", () => {
    const result = evaluateMilestones("allTime", 1, new Set());
    expect(result.celebrate).toBe(1);
    expect(result.toMark).toEqual([milestoneKey("allTime", 100), milestoneKey("allTime", 50), milestoneKey("allTime", 10), milestoneKey("allTime", 1)]);
  });

  it("keeps scopes independent — reaching Top 100 in allTime doesn't affect weekly", () => {
    const already = new Set([milestoneKey("allTime", 100)]);
    const result = evaluateMilestones("weekly", 90, already);
    expect(result.celebrate).toBe(100);
  });

  it("dropping back out of a tier and returning doesn't re-celebrate it", () => {
    const already = new Set([milestoneKey("allTime", 100), milestoneKey("allTime", 50)]);
    // Player fell to rank 80 (still inside Top 100, which is already claimed) then climbs back to 45.
    const first = evaluateMilestones("allTime", 80, already);
    expect(first.celebrate).toBeNull();
  });
});
