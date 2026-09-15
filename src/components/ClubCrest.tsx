import { useMemo } from "react";
import { crestSpecFor, type CrestPattern, type CrestShape, type CrestSpec } from "@/assets/crestPlaceholder";
import type { Club, RevealMode } from "@/types";

export interface ClubCrestProps {
  club: Club;
  size?: number;
  revealMode?: RevealMode;
  /** 0..1 — how "revealed" the crest is. 0 = least visible, 1 = fully revealed. Ignored for FULL/SILHOUETTE. */
  revealProgress?: number;
  className?: string;
}

const SHAPE_PATH: Record<Exclude<CrestShape, "circle">, string> = {
  shield: "M50,4 L90,18 L90,50 C90,78 72,92 50,98 C28,92 10,78 10,50 L10,18 Z",
  hexagon: "M50,4 L92,27 L92,73 L50,96 L8,73 L8,27 Z",
  pentagon: "M50,4 L93.75,35.8 L77,87.2 L23,87.2 L6.25,35.8 Z",
};

function shapeGeometry(shape: CrestShape): React.JSX.Element {
  return shape === "circle" ? <circle cx="50" cy="50" r="46" /> : <path d={SHAPE_PATH[shape]} />;
}

function starPoints(cx: number, cy: number, outerR: number, innerR: number): string {
  const pts: string[] = [];
  for (let i = 0; i < 10; i++) {
    const r = i % 2 === 0 ? outerR : innerR;
    const angle = ((-90 + i * 36) * Math.PI) / 180;
    pts.push(`${(cx + r * Math.cos(angle)).toFixed(2)},${(cy + r * Math.sin(angle)).toFixed(2)}`);
  }
  return pts.join(" ");
}

function patternFill(pattern: CrestPattern, spec: CrestSpec, gradId: string): React.JSX.Element {
  const { primaryColor: p, secondaryColor: s, mirrorDiagonal } = spec;
  switch (pattern) {
    case "halves":
      return (
        <>
          <rect x="0" y="0" width="50" height="100" fill={s} />
          <rect x="50" y="0" width="50" height="100" fill={p} />
        </>
      );
    case "diagonal":
      return (
        <>
          <polygon points={mirrorDiagonal ? "0,100 100,100 100,0" : "0,0 100,0 0,100"} fill={p} />
          <polygon points={mirrorDiagonal ? "0,0 100,0 0,100" : "0,100 100,100 100,0"} fill={s} />
        </>
      );
    case "quarters":
      return (
        <>
          <rect x="0" y="0" width="50" height="50" fill={p} />
          <rect x="50" y="0" width="50" height="50" fill={s} />
          <rect x="0" y="50" width="50" height="50" fill={s} />
          <rect x="50" y="50" width="50" height="50" fill={p} />
        </>
      );
    case "stripes":
      return (
        <>
          <rect x="0" y="0" width="100" height="100" fill={p} />
          <rect x="0" y="0" width="25" height="100" fill={s} />
          <rect x="50" y="0" width="25" height="100" fill={s} />
        </>
      );
    case "solid":
    default:
      return <rect x="0" y="0" width="100" height="100" fill={`url(#${gradId})`} />;
  }
}

/**
 * Renders the deterministic placeholder crest from crestPlaceholder.ts —
 * shape + two-tone pattern + ring border + prestige stars, all derived from
 * the club id — with support for the game's logo reveal modes (FULL/ZOOM/
 * BLUR/SILHOUETTE/PIECE — see docs/GAME_DESIGN.md). Swapping in real,
 * licensed artwork for a specific club only means setting `club.logoUrl`
 * (see public/crests/README.md) — callers never change.
 */
export function ClubCrest({ club, size = 120, revealMode = "FULL", revealProgress = 1, className }: ClubCrestProps) {
  const spec = useMemo(() => crestSpecFor(club), [club]);
  const progress = Math.min(1, Math.max(0, revealProgress));
  const shapeClipId = `crest-shape-${club.id}`;
  const pieceClipId = `crest-piece-${club.id}`;
  const gradId = `crest-grad-${club.id}`;

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
  const isSilhouette = revealMode === "SILHOUETTE";

  if (club.logoUrl) {
    return (
      <div className={className} style={wrapperStyle}>
        <svg viewBox="0 0 100 100" width="100%" height="100%" role="img" aria-label={isSilhouette ? "Mystery club crest" : `${club.name} crest`}>
          <defs>
            {pieceRadius !== null && (
              <clipPath id={pieceClipId}>
                <circle cx="50" cy="50" r={pieceRadius} />
              </clipPath>
            )}
          </defs>
          <g clipPath={pieceRadius !== null ? `url(#${pieceClipId})` : undefined}>
            <image
              href={club.logoUrl}
              x="4"
              y="4"
              width="92"
              height="92"
              preserveAspectRatio="xMidYMid meet"
              style={{
                transform: isZoom ? `scale(${zoomScale})` : undefined,
                transformOrigin: isZoom ? `${focusX}% ${focusY}%` : undefined,
                transition: "transform 200ms linear",
                filter: isSilhouette ? "brightness(0)" : undefined,
              }}
            />
          </g>
        </svg>
      </div>
    );
  }

  const starRowWidth = (spec.starCount - 1) * 14;
  const starRowStartX = 50 - starRowWidth / 2;
  const initialsY = spec.starCount > 0 ? 66 : 60;

  return (
    <div className={className} style={wrapperStyle}>
      <svg
        viewBox="0 0 100 100"
        width="100%"
        height="100%"
        style={isZoom ? { transform: `scale(${zoomScale})`, transformOrigin: `${focusX}% ${focusY}%`, transition: "transform 200ms linear" } : undefined}
        role="img"
        aria-label={isSilhouette || (revealMode === "BLUR" && progress < 0.4) ? "Mystery club crest" : `${club.name} crest`}
      >
        <defs>
          <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={spec.secondaryColor} />
            <stop offset="100%" stopColor={spec.primaryColor} />
          </linearGradient>
          <clipPath id={shapeClipId}>{shapeGeometry(spec.shape)}</clipPath>
          {pieceRadius !== null && (
            <clipPath id={pieceClipId}>
              <circle cx="50" cy="50" r={pieceRadius} />
            </clipPath>
          )}
        </defs>
        <g clipPath={pieceRadius !== null ? `url(#${pieceClipId})` : undefined}>
          <g clipPath={`url(#${shapeClipId})`}>{isSilhouette ? <rect x="0" y="0" width="100" height="100" fill="#0b0f18" /> : patternFill(spec.pattern, spec, gradId)}</g>

          <g fill="none" stroke={isSilhouette ? "rgba(255,255,255,0.1)" : spec.ringColor} strokeWidth={3.5}>
            {shapeGeometry(spec.shape)}
          </g>
          {!isSilhouette && (
            <g fill="none" stroke={spec.highlightColor} strokeWidth={1} opacity={0.55} transform="translate(50 50) scale(0.88) translate(-50 -50)">
              {shapeGeometry(spec.shape)}
            </g>
          )}

          {!isSilhouette && spec.starCount > 0 && (
            <g fill={spec.highlightColor} opacity={0.9}>
              {Array.from({ length: spec.starCount }, (_, i) => (
                <polygon key={i} points={starPoints(starRowStartX + i * 14, 22, 5, 2.2)} />
              ))}
            </g>
          )}

          {!isSilhouette && (
            <text x="50" y={initialsY} textAnchor="middle" fontSize="28" fontWeight="800" fill="rgba(255,255,255,0.95)" fontFamily="system-ui, sans-serif">
              {spec.initials}
            </text>
          )}
        </g>
      </svg>
    </div>
  );
}
