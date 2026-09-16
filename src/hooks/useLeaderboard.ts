import { useEffect, useState } from "react";
import { LeaderboardService, type LeaderboardEntry, type LeaderboardScope } from "@/services/LeaderboardService";

export interface UseLeaderboardResult {
  entries: LeaderboardEntry[];
  loading: boolean;
}

/** Subscribes to a live leaderboard scope for as long as the component is mounted; re-subscribes whenever `scope` changes. */
export function useLeaderboard(scope: LeaderboardScope): UseLeaderboardResult {
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    const unsubscribe = LeaderboardService.subscribe(scope, (next) => {
      setEntries(next);
      setLoading(false);
    });
    return unsubscribe;
  }, [scope]);

  return { entries, loading };
}
