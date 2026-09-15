export interface PlayerSilhouetteProps {
  size?: number;
}

/**
 * Generic silhouette — never a real photo (no rights/likeness concern —
 * see docs/ASSETS_AND_RIGHTS.md). The guess comes entirely from the
 * transfer chain (see TransferChain.tsx), not from any player-specific
 * artwork, so every player reuses this exact same shape.
 */
export function PlayerSilhouette({ size = 140 }: PlayerSilhouetteProps) {
  return (
    <svg viewBox="0 0 100 120" width={size} height={size * 1.2} role="img" aria-label="Mystery player silhouette">
      <path
        d="M50,8 C62,8 70,18 70,32 C70,42 65,50 58,54 C74,60 84,74 84,96 L84,112 L16,112 L16,96 C16,74 26,60 42,54 C35,50 30,42 30,32 C30,18 38,8 50,8 Z"
        fill="#0b0f18"
        stroke="rgba(255,255,255,0.1)"
        strokeWidth="1.5"
      />
      <text x="50" y="46" textAnchor="middle" fontSize="30" fontWeight="900" fill="#ffd54f" fontFamily="system-ui, sans-serif">
        ?
      </text>
    </svg>
  );
}
