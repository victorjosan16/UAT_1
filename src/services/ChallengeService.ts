import { apiClient, type ApiClient } from "./ApiClient";

export interface ChallengeCreateResponse {
  challengeId: string;
  url: string;
  expiresAt: string;
}

export interface ChallengeDetails {
  challengeId: string;
  creatorNickname: string;
  creatorScore: number;
  creatorHeight: number;
  seed: string;
  gameVersion: string;
  expired: boolean;
}

export interface ChallengeAttemptResponse {
  accepted: boolean;
  yourScore: number;
  yourHeight: number;
  opponentScore: number;
  opponentHeight: number;
  outcome: "WIN" | "LOSS" | "SO_CLOSE";
}

export class ChallengeService {
  constructor(private readonly api: ApiClient = apiClient) {}

  createChallenge(sessionId: string): Promise<ChallengeCreateResponse> {
    return this.api.post<ChallengeCreateResponse>("/challenge", { sessionId });
  }

  fetchChallenge(challengeId: string): Promise<ChallengeDetails> {
    return this.api.get<ChallengeDetails>(`/challenge/${encodeURIComponent(challengeId)}`);
  }

  submitAttempt(challengeId: string, sessionId: string): Promise<ChallengeAttemptResponse> {
    return this.api.post<ChallengeAttemptResponse>(`/challenge/${encodeURIComponent(challengeId)}/attempt`, { sessionId });
  }

  buildShareUrl(challengeId: string): string {
    return `${window.location.origin}/challenge/${challengeId}`;
  }
}

export const challengeService = new ChallengeService();
