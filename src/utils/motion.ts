/**
 * Central `prefers-reduced-motion` check. All effect systems (shake,
 * slow-motion, particles, transitions) consult this before doing anything
 * disorienting — gameplay itself never changes, only its presentation.
 */
let cached: boolean | null = null;

export function prefersReducedMotion(): boolean {
  if (cached !== null) return cached;
  try {
    cached = typeof window !== "undefined" && window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true;
  } catch {
    cached = false;
  }
  return cached;
}

if (typeof window !== "undefined" && window.matchMedia) {
  try {
    window.matchMedia("(prefers-reduced-motion: reduce)").addEventListener("change", (e) => {
      cached = e.matches;
    });
  } catch {
    // Older browsers without addEventListener on MediaQueryList — safe to ignore.
  }
}
