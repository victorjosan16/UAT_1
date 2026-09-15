import { describe, expect, it } from "vitest";
import { PLAYERS, getPlayerById } from "@/data/players";
import { getClubById } from "@/data/clubs";

describe("PLAYERS dataset", () => {
  it("has no duplicate ids", () => {
    const ids = PLAYERS.map((p) => p.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("has no duplicate names", () => {
    const names = PLAYERS.map((p) => p.name);
    expect(new Set(names).size).toBe(names.length);
  });

  it("every career club id resolves to a real club in the dataset", () => {
    for (const player of PLAYERS) {
      for (const clubId of player.careerClubIds) {
        expect(getClubById(clubId), `${player.name} lists unknown club id "${clubId}"`).toBeDefined();
      }
    }
  });

  it("every player has at least one career stop", () => {
    for (const player of PLAYERS) {
      expect(player.careerClubIds.length).toBeGreaterThan(0);
    }
  });

  it("has enough players to build 4-option questions", () => {
    expect(PLAYERS.length).toBeGreaterThanOrEqual(20);
  });

  it("getPlayerById finds a known player and returns undefined for an unknown one", () => {
    expect(getPlayerById("lionel-messi")?.name).toBe("Lionel Messi");
    expect(getPlayerById("does-not-exist")).toBeUndefined();
  });
});
