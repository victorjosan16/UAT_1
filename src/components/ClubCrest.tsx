import { useMemo } from "react";
import { crestSpecFor } from "@/assets/crestPlaceholder";
import type { Club, RevealMode } from "@/types";

export interface ClubCrestProps {
  club: Club;
  size?: number;
  revealMode?: RevealMode;
  /** 0..1 — how "revealed" the crest is. 0 = least visible, 1 = fully revealed. Ignored for FULL/SILHOUETTE. */
  revealProgress?: number;
  className?: string;
}

const SHAPE_PATH: Record<"shield" | "hexagon", string> = {
  shield: "M50,4 L90,18 L90,50 C90,78 72,92 50,98 C28,92 10,78 10,50 L10,18 Z",
  hexagon: "M50,4 L92,27 L92,73 L50,96 L8,73 L8,27 Z",
};

/**
 * Renders the deterministic placeholder crest from crestPlaceholder.ts,
 * with support for the game's logo reveal modes (FULL/ZOOM/BLUR/
 * SILHOUETTE/PIECE — see docs/GAME_DESIGN.md). Swapping in real licensed
 * artwork later only means changing this component's render body (or the
 * asset resolver it reads from); callers never change.
 */
export function ClubCrest({ club, size = 120, revealMode = "FULL", revealProgress = 1, className }: ClubCrestProps) {
  const spec = useMemo(() => crestSpecFor(club), [club]);
  const progress = Math.min(1, Math.max(0, revealProgress));
  const clipId = `crest-clip-${club.id}`;
  const gradId = `crest-grad-${club.id}`;

  const shapeElement =
    spec.shape === "circle" ? (
      <circle cx="50" cy="50" r="46" />
    ) : (
      <path d={SHAPE_PATH[spec.shape]} />
    );

  const wrapperStyle: React.CSSProperties = {
    width: size,
    height: size,
    filter: revealMode === "BLUR" ? `blur(${(1 - progress) * 10}px)` : undefined,
    transition: "filter 200ms linear",
  };

  const isZoom = revealMode === "ZOOM";
  const zoomScale = isZoom ? 1 + (1 - progress) * 2.4 : 1;
  const focusX = spec.zoomFocus.x * 100;
  const focusY = spec.zoomFocus.y * 100;

  const pieceRadius = revealMode === "PIECE" ? 8 + progress * 90 : null;

  return (
    <div className={className} style={wrapperStyle}>
      <svg
        viewBox="0 0 100 100"
        width="100%"
        height="100%"
        style={isZoom ? { transform: `scale(${zoomScale})`, transformOrigin: `${focusX}% ${focusY}%`, transition: "transform 200ms linear" } : undefined}
        role="img"
        aria-label={revealMode === "SILHOUETTE" || (revealMode === "BLUR" && progress < 0.4) ? "Mystery club crest" : `${club.name} crest`}
      >
        <defs>
          <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={spec.secondaryColor} />
            <stop offset="100%" stopColor={spec.primaryColor} />
          </linearGradient>
          {pieceRadius !== null && (
            <clipPath id={clipId}>
              <circle cx="50" cy="50" r={pieceRadius} />
            </clipPath>
          )}
        </defs>
        <g clipPath={pieceRadius !== null ? `url(#${clipId})` : undefined}>
          <g fill={revealMode === "SILHOUETTE" ? "#0b0f18" : `url(#${gradId})`} stroke="rgba(255,255,255,0.18)" strokeWidth="2">
            {shapeElement}
          </g>
          {revealMode !== "SILHOUETTE" && (
            <text x="50" y="58" textAnchor="middle" fontSize="30" fontWeight="800" fill="rgba(255,255,255,0.92)" fontFamily="system-ui, sans-serif">
              {spec.initials}
            </text>
          )}
        </g>
      </svg>
    </div>
  );
}
