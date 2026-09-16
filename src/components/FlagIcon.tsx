import type { RevealMode } from "@/types";
import type { FlagPattern } from "@/data/countries";

export interface FlagIconProps {
  pattern: FlagPattern;
  countryName: string;
  width?: number;
  revealMode?: RevealMode;
  /** 0..1 — how "revealed" the flag is. 0 = least visible, 1 = fully revealed. Ignored for FULL/SILHOUETTE. */
  revealProgress?: number;
  className?: string;
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

/** W:H viewBox is 150x100 (3:2), the common national flag ratio. */
function flagBody(pattern: FlagPattern): React.JSX.Element {
  switch (pattern.kind) {
    case "horizontal": {
      const n = pattern.colors.length;
      const h = 100 / n;
      return (
        <>
          {pattern.colors.map((color, i) => (
            <rect key={i} x="0" y={i * h} width="150" height={h + 0.5} fill={color} />
          ))}
        </>
      );
    }
    case "vertical": {
      const n = pattern.colors.length;
      const w = 150 / n;
      return (
        <>
          {pattern.colors.map((color, i) => (
            <rect key={i} x={i * w} y="0" width={w + 0.5} height="100" fill={color} />
          ))}
        </>
      );
    }
    case "nordic-cross":
      return (
        <>
          <rect x="0" y="0" width="150" height="100" fill={pattern.field} />
          <rect x="55" y="0" width="18" height="100" fill={pattern.cross} />
          <rect x="0" y="41" width="150" height="18" fill={pattern.cross} />
        </>
      );
    case "swiss-cross":
      return (
        <>
          <rect x="0" y="0" width="150" height="100" fill={pattern.field} />
          <rect x="63" y="30" width="24" height="40" fill={pattern.cross} />
          <rect x="53" y="40" width="44" height="20" fill={pattern.cross} />
        </>
      );
    case "circle":
      return (
        <>
          <rect x="0" y="0" width="150" height="100" fill={pattern.field} />
          <circle cx="75" cy="50" r="24" fill={pattern.circle} />
        </>
      );
    case "canton-stripes": {
      const stripeCount = 7;
      const stripeH = 100 / stripeCount;
      return (
        <>
          {Array.from({ length: stripeCount }, (_, i) => (
            <rect key={i} x="0" y={i * stripeH} width="150" height={stripeH + 0.5} fill={i % 2 === 0 ? pattern.stripeColors[0] : pattern.stripeColors[1]} />
          ))}
          <rect x="0" y="0" width="65" height={stripeH * 4} fill={pattern.cantonColor} />
        </>
      );
    }
    case "single":
      return (
        <>
          <rect x="0" y="0" width="150" height="100" fill={pattern.color} />
          {pattern.overlay === "star-crescent" && (
            <>
              <mask id="crescent-mask">
                <rect x="0" y="0" width="150" height="100" fill="white" />
                <circle cx="82" cy="50" r="20" fill="black" />
              </mask>
              <circle cx="72" cy="50" r="24" fill="#ffffff" mask="url(#crescent-mask)" />
              <polygon points={starPoints(102, 50, 8, 3.4)} fill="#ffffff" />
            </>
          )}
        </>
      );
    case "union-jack":
      return (
        <>
          <rect x="0" y="0" width="150" height="100" fill="#00247D" />
          <line x1="0" y1="0" x2="150" y2="100" stroke="#FFFFFF" strokeWidth="22" />
          <line x1="150" y1="0" x2="0" y2="100" stroke="#FFFFFF" strokeWidth="22" />
          <line x1="0" y1="0" x2="150" y2="100" stroke="#CF142B" strokeWidth="8" />
          <line x1="150" y1="0" x2="0" y2="100" stroke="#CF142B" strokeWidth="8" />
          <rect x="0" y="40" width="150" height="20" fill="#FFFFFF" />
          <rect x="65" y="0" width="20" height="100" fill="#FFFFFF" />
          <rect x="0" y="45" width="150" height="10" fill="#CF142B" />
          <rect x="70" y="0" width="10" height="100" fill="#CF142B" />
        </>
      );
    default:
      return <rect x="0" y="0" width="150" height="100" fill="#cccccc" />;
  }
}

/**
 * Renders a country's real flag from structured data (data/countries.ts) —
 * simplified geometry but accurate colors/layout, with the same reveal-mode
 * treatment as ClubCrest (FULL/ZOOM/BLUR/SILHOUETTE/PIECE — see
 * docs/GAME_DESIGN.md). National flags are official state symbols, not a
 * private trademark, so this is never a licensing concern.
 */
export function FlagIcon({ pattern, countryName, width = 180, revealMode = "FULL", revealProgress = 1, className }: FlagIconProps) {
  const progress = Math.min(1, Math.max(0, revealProgress));
  const height = width * (2 / 3);
  const pieceClipId = `flag-piece-${countryName.replace(/\s+/g, "-")}`;
  const isSilhouette = revealMode === "SILHOUETTE";

  const wrapperStyle: React.CSSProperties = {
    width,
    height,
    borderRadius: 6,
    overflow: "hidden",
    boxShadow: "0 4px 14px rgba(0,0,0,0.18)",
    filter: revealMode === "BLUR" ? `blur(${(1 - progress) * 9}px)` : undefined,
    transition: "filter 200ms linear",
  };

  const isZoom = revealMode === "ZOOM";
  const zoomScale = isZoom ? 1 + (1 - progress) * 2 : 1;

  const pieceRadius = revealMode === "PIECE" ? 10 + progress * 110 : null;

  return (
    <div className={className} style={wrapperStyle}>
      <svg
        viewBox="0 0 150 100"
        width="100%"
        height="100%"
        style={isZoom ? { transform: `scale(${zoomScale})`, transformOrigin: "50% 50%", transition: "transform 200ms linear" } : undefined}
        role="img"
        aria-label={isSilhouette ? "Mystery flag" : `Flag of ${countryName}`}
      >
        <defs>
          {pieceRadius !== null && (
            <clipPath id={pieceClipId}>
              <circle cx="75" cy="50" r={pieceRadius} />
            </clipPath>
          )}
        </defs>
        <g clipPath={pieceRadius !== null ? `url(#${pieceClipId})` : undefined}>{isSilhouette ? <rect x="0" y="0" width="150" height="100" fill="#0b0f18" /> : flagBody(pattern)}</g>
      </svg>
    </div>
  );
}
