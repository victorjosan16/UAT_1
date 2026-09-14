/**
 * Central tuning for every game-feel effect. Nothing here changes
 * gameplay, scoring, or level rules — only how placements/misses/records
 * are presented. Keep every "magic number" for juice in this one file.
 */
export const VFX_CONFIG = {
  perfect: {
    durationMs: 380,
    particleCountBase: 14,
    particleCountPerStreak: 3,
    particleCountCap: 60,
    glowStrength: 1,
    scalePulse: 1.08,
    shakeTrauma: 0.06,
    shakeTraumaBigStreak: 0.14,
  },
  perfectSlowMo: {
    // Only fires at meaningful streaks / records — never on every Perfect.
    streakThresholds: [5, 10, 20],
    scale: 0.65,
    downMs: 60,
    holdMs: 90,
    upMs: 140,
  },
  combo: {
    milestones: [5, 10, 20, 30, 50],
    particleCountBase: 18,
    particleCountPerTier: 10,
    shakeTrauma: 0.1,
  },
  cut: {
    particleCount: 6,
    shakeTraumaPerFraction: 0.18, // scaled by fraction of block cut away
  },
  nearMiss: {
    // Placement survives with less than this fraction of the block remaining.
    survivalRatioThreshold: 0.12,
    particleCount: 10,
    shakeTrauma: 0.12,
  },
  gameOver: {
    transitionMs: 780,
    shakeTrauma: 0.55,
    slowMoScale: 0.5,
    slowMoMs: 420,
    darkenPeak: 0.72,
    particleCount: 24,
  },
  record: {
    particleCount: 40,
    shakeTrauma: 0.08,
    glowDurationMs: 900,
  },
  milestone: {
    particleCount: 20,
    pulseDurationMs: 500,
  },
  trail: {
    minSpeedForTrail: 150,
    maxReferenceSpeed: 500,
    maxSamples: 7,
    sampleIntervalMs: 16,
    maxAgeMs: 220,
  },
  shakeDecayPerSecond: 2.6,
  shakeMaxOffset: 10,
};
