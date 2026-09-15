import { ClubCrest } from "./ClubCrest";
import type { Club } from "@/types";

export interface TransferChainProps {
  clubs: Club[];
  /** 0..1 — how much of the chain to reveal so far; unrevealed stops show a "?" placeholder instead of the crest. */
  revealProgress: number;
  crestSize?: number;
}

function MysteryStop({ size }: { size: number }) {
  return (
    <div style={{ width: size, height: size, borderRadius: "50%", border: "2px dashed rgba(255,255,255,0.25)", display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 800, color: "var(--text-dim)" }}>
      ?
    </div>
  );
}

/** The career path a "Guess the Player" question is built from — see docs/GAME_DESIGN.md §Guess the Player. Reuses the same placeholder crests as the logo mode. */
export function TransferChain({ clubs, revealProgress, crestSize = 56 }: TransferChainProps) {
  const visibleCount = Math.max(1, Math.min(clubs.length, Math.ceil(revealProgress * clubs.length)));

  return (
    <div style={{ display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "center", gap: 8, maxWidth: 340 }}>
      {clubs.map((club, i) => (
        <div key={club.id} style={{ display: "flex", alignItems: "center", gap: 8 }}>
          {i > 0 && <span style={{ color: "var(--text-dim)", fontWeight: 700 }}>»</span>}
          {i < visibleCount ? <ClubCrest club={club} size={crestSize} /> : <MysteryStop size={crestSize} />}
        </div>
      ))}
    </div>
  );
}
