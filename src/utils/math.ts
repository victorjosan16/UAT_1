export function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

export function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

/** Frame-rate independent exponential smoothing, e.g. for camera easing. */
export function damp(current: number, target: number, smoothing: number, dtSeconds: number): number {
  const t = 1 - Math.pow(smoothing, dtSeconds);
  return lerp(current, target, t);
}

export function roundTo(value: number, decimals: number): number {
  const factor = 10 ** decimals;
  return Math.round(value * factor) / factor;
}
