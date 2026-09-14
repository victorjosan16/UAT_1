import { ensureSignedIn } from "./firebase";
import { withTimeout } from "@/utils/async";

const AUTH_TOKEN_TIMEOUT_MS = 4000;

/**
 * Thin fetch wrapper — the single network boundary. Every other service
 * goes through this so swapping backends (Firebase could later be swapped
 * for something else) only means rewriting this file and the service
 * methods that call it.
 */

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface ApiClientOptions {
  baseUrl?: string;
  getAuthToken?: () => Promise<string | null> | string | null;
  timeoutMs?: number;
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly getAuthToken?: () => Promise<string | null> | string | null;
  private readonly timeoutMs: number;

  constructor(options: ApiClientOptions = {}) {
    this.baseUrl = options.baseUrl ?? "/api";
    this.getAuthToken = options.getAuthToken;
    this.timeoutMs = options.timeoutMs ?? 8000;
  }

  async request<TResponse>(path: string, init: RequestInit = {}): Promise<TResponse> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), this.timeoutMs);

    const headers = new Headers(init.headers);
    headers.set("Content-Type", "application/json");
    const token = await this.getAuthToken?.();
    if (token) headers.set("Authorization", `Bearer ${token}`);

    try {
      const response = await fetch(`${this.baseUrl}${path}`, { ...init, headers, signal: controller.signal });
      const text = await response.text();
      const data = text ? (JSON.parse(text) as unknown) : null;

      if (!response.ok) {
        const errorBody = data as { error?: string; code?: string } | null;
        throw new ApiError(errorBody?.error ?? `Request failed with ${response.status}`, response.status, errorBody?.code);
      }

      return data as TResponse;
    } catch (error) {
      if (error instanceof ApiError) throw error;
      if (error instanceof DOMException && error.name === "AbortError") {
        throw new ApiError("Request timed out", 0, "TIMEOUT");
      }
      throw new ApiError("Network unavailable", 0, "NETWORK");
    } finally {
      clearTimeout(timeout);
    }
  }

  get<T>(path: string): Promise<T> {
    return this.request<T>(path, { method: "GET" });
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined });
  }
}

export const apiClient = new ApiClient({
  getAuthToken: async () => {
    try {
      const user = await withTimeout(ensureSignedIn(), AUTH_TOKEN_TIMEOUT_MS);
      return await user.getIdToken();
    } catch {
      return null;
    }
  },
});
