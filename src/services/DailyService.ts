import { apiClient, type ApiClient } from "./ApiClient";

export interface DailyStatusResponse {
  dateKey: string;
  seed: string;
  todaysBestFloor: number;
  yourBestFloor: number;
  yourBestScore: number;
  yourRank: number | null;
  attemptCount: number;
  streak: number;
}

export interface DailySubmitResponse {
  accepted: boolean;
  rank: number | null;
  streak: number;
}

export class DailyService {
  constructor(private readonly api: ApiClient = apiClient) {}

  fetchStatus(): Promise<DailyStatusResponse> {
    return this.api.get<DailyStatusResponse>("/daily");
  }

  submitScore(sessionId: string): Promise<DailySubmitResponse> {
    return this.api.post<DailySubmitResponse>("/daily/score", { sessionId });
  }
}

export const dailyService = new DailyService();
