import type { Response } from "express";
import type { ZodSchema } from "zod";

export function badRequest(res: Response, message: string, code = "BAD_REQUEST"): void {
  res.status(400).json({ error: message, code });
}

export function parseBody<T>(schema: ZodSchema<T>, body: unknown, res: Response): T | null {
  const result = schema.safeParse(body);
  if (!result.success) {
    badRequest(res, result.error.issues[0]?.message ?? "Invalid request body", "VALIDATION_ERROR");
    return null;
  }
  return result.data;
}
