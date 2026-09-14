import express from "express";
import cors from "cors";
import { playerRouter } from "./routes/player";
import { sessionRouter } from "./routes/session";
import { leaderboardRouter } from "./routes/leaderboard";
import { dailyRouter } from "./routes/daily";
import { challengeRouter } from "./routes/challenge";

export const app = express();

app.use(cors({ origin: true }));
app.use(express.json({ limit: "256kb" })); // generous for a full placement trace, small enough to block payload-flood abuse

app.use(playerRouter);
app.use(sessionRouter);
app.use(leaderboardRouter);
app.use(dailyRouter);
app.use(challengeRouter);

app.use((_req, res) => {
  res.status(404).json({ error: "Not found", code: "NOT_FOUND" });
});

// Centralized error handler — never leak stack traces/internals to clients (docs/SECURITY.md).
// eslint-disable-next-line @typescript-eslint/no-unused-vars
app.use((err: unknown, _req: express.Request, res: express.Response, _next: express.NextFunction) => {
  console.error(err);
  res.status(500).json({ error: "Internal server error", code: "INTERNAL" });
});
