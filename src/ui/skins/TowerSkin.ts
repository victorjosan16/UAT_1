/**
 * Cosmetic-only block appearance. Gameplay code never reads this — see
 * docs/ARCHITECTURE.md §Skins. A future skin store only needs to add
 * entries here and to the player's unlocked-skins list.
 */
export interface TowerSkin {
  id: string;
  name: string;
  blockFill: string;
  blockStroke: string;
  blockGradientTop?: string;
  perfectGlowColor: string;
}

export const DEFAULT_TOWER_SKIN: TowerSkin = {
  id: "default",
  name: "CLASSIC",
  blockFill: "#4fd1ff",
  blockStroke: "#0a0e17",
  blockGradientTop: "#8be9ff",
  perfectGlowColor: "#ffd54f",
};

export const TOWER_SKINS: readonly TowerSkin[] = [
  DEFAULT_TOWER_SKIN,
  { id: "neon", name: "NEON", blockFill: "#ff2fd6", blockStroke: "#1a001a", blockGradientTop: "#ff8bf0", perfectGlowColor: "#39ff88" },
  { id: "ice", name: "ICE", blockFill: "#bfeaff", blockStroke: "#0a2a3a", blockGradientTop: "#ffffff", perfectGlowColor: "#4fd1ff" },
  { id: "gold", name: "GOLD", blockFill: "#ffd54f", blockStroke: "#3a2a00", blockGradientTop: "#fff2b3", perfectGlowColor: "#ffffff" },
];
