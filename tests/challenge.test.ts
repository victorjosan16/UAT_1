import { describe, expect, it } from "vitest";
import { evaluateChallengeOutcome } from "@/modes/ChallengeMode";
import { createDailyRun } from "@/modes/DailyMode";
import { createChallengeRun, type ChallengeSummary } from "@/modes/ChallengeMode";

describe("evaluateChallengeOutcome", () => {
  it("is a WIN when you score strictly higher", () => {
    expect(evaluateChallengeOutcome(500, 400)).toBe("WIN");
  });

  it("is SO_CLOSE within the close-margin band below the opponent", () => {
    expect(evaluateChallengeOutcome(390, 400)).toBe("SO_CLOSE");
  });

  it("is a LOSS well below the opponent", () => {
    expect(evaluateChallengeOutcome(100, 400)).toBe("LOSS");
  });
});

describe("createChallengeRun", () => {
  it("replays the exact seed from the challenge so both players face identical conditions", () => {
    const challenge: ChallengeSummary = {
      challengeId: "ABC123",
      creatorNickname: "VICTOR",
      creatorScore: 42850,
      creatorHeight: 87,
      seed: "classic:XYZ789",
      gameVersion: "1.0.0",
    };
    const run = createChallengeRun(challenge);
    expect(run.seed).toBe(challenge.seed);
    expect(run.targetScoreToBeat).toBe(challenge.creatorScore);
  });
});

describe("createDailyRun", () => {
  it("produces the same seed for two calls on the same UTC day", () => {
    const day = new Date(Date.UTC(2026, 2, 3, 10, 0, 0));
    const a = createDailyRun(day);
    const b = createDailyRun(new Date(Date.UTC(2026, 2, 3, 22, 0, 0)));
    expect(a.seed).toBe(b.seed);
    expect(a.dateKey).toBe("2026-03-03");
  });
});
