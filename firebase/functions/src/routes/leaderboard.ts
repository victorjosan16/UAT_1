import { Router } from "express";
import { db } from "../lib/firestore";
import { optionalAuth, type AuthedRequest } from "../lib/auth";
import { LeaderboardWindowSchema } from "../validation/schemas";
import { isoWeekKey } from "../lib/date";
import { utcDateKey } from "../lib/gameRules";

export const leaderboardRouter = Router();

const PAGE_SIZE = 50;

function collectionForWindow(window: "daily" | "weekly" | "all-time"): FirebaseFirestore.CollectionReference {
  if (window === "daily") return db.collection("leaderboard_daily").doc(utcDateKey()).collection("entries");
  if (window === "weekly") return db.collection("leaderboard_weekly").doc(isoWeekKey()).collection("entries");
  return db.collection("leaderboard_alltime");
}

leaderboardRouter.get("/api/leaderboard/:window", optionalAuth, async (req: AuthedRequest, res) => {
  const parsedWindow = LeaderboardWindowSchema.safeParse(req.params.window);
  if (!parsedWindow.success) {
    res.status(400).json({ error: "Invalid leaderboard window", code: "VALIDATION_ERROR" });
    return;
  }
  const window = parsedWindow.data;

  const snap = await collectionForWindow(window).orderBy("score", "desc").limit(PAGE_SIZE).get();
  const entries = snap.docs.map((doc, i) => ({
    rank: i + 1,
    playerId: doc.id,
    nickname: (doc.data().nickname as string | undefined) ?? "PLAYER",
    score: doc.data().score as number,
    height: doc.data().height as number,
  }));

  let you = entries.find((e) => e.playerId === req.uid) ?? null;
  if (!you && req.uid) {
    const mine = await collectionForWindow(window).doc(req.uid).get();
    if (mine.exists) {
      const aheadSnap = await collectionForWindow(window)
        .where("score", ">", mine.data()?.score as number)
        .count()
        .get();
      you = {
        rank: aheadSnap.data().count + 1,
        playerId: req.uid,
        nickname: (mine.data()?.nickname as string | undefined) ?? "PLAYER",
        score: mine.data()?.score as number,
        height: mine.data()?.height as number,
      };
    }
  }

  res.json({ window, entries, you });
});
