import type { NextFunction, Request, Response } from "express";
import { getAuth } from "firebase-admin/auth";

export interface AuthedRequest extends Request {
  uid?: string;
}

/**
 * Verifies the Firebase Auth ID token (from anonymous sign-in on the
 * client) sent as `Authorization: Bearer <token>`. Every route that
 * touches player-specific data requires this — never trust a client-
 * supplied playerId directly, only the verified token's uid.
 */
export async function requireAuth(req: AuthedRequest, res: Response, next: NextFunction): Promise<void> {
  const header = req.header("authorization") ?? "";
  const match = /^Bearer (.+)$/.exec(header);
  if (!match) {
    res.status(401).json({ error: "Missing bearer token", code: "UNAUTHENTICATED" });
    return;
  }
  const token = match[1] ?? "";

  try {
    const decoded = await getAuth().verifyIdToken(token);
    req.uid = decoded.uid;
    next();
  } catch {
    res.status(401).json({ error: "Invalid or expired token", code: "UNAUTHENTICATED" });
  }
}

/** Same verification, but a missing/invalid token is not an error — used for public endpoints that personalize output when signed in. */
export async function optionalAuth(req: AuthedRequest, _res: Response, next: NextFunction): Promise<void> {
  const header = req.header("authorization") ?? "";
  const match = /^Bearer (.+)$/.exec(header);
  if (!match) {
    next();
    return;
  }
  try {
    const decoded = await getAuth().verifyIdToken(match[1] ?? "");
    req.uid = decoded.uid;
  } catch {
    // Ignore — treat as anonymous/unauthenticated for optional-auth routes.
  }
  next();
}
