/** Bounded travel/angle timing; scene units are not claimed to be metres. */
export function transitionDuration(baseMs: number, distance: number, angle: number, fovDelta: number, reducedMotion: boolean) {
  if (reducedMotion) return 1
  const base = Math.max(100, Math.min(1500, baseMs))
  const weight = Math.min(1, Math.max(distance / 2, angle / (Math.PI / 2), Math.abs(fovDelta) / 45))
  return base * (0.65 + 0.35 * weight)
}
