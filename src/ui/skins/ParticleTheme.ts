export interface ParticleTheme {
  id: string;
  perfectColor: string;
  comboColor: string;
  recordColor: string;
}

export const DEFAULT_PARTICLE_THEME: ParticleTheme = {
  id: "default",
  perfectColor: "#ffd54f",
  comboColor: "#4fd1ff",
  recordColor: "#ffffff",
};
