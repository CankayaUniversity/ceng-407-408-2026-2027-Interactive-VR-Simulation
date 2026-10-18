import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import type { FreeCamera } from '@babylonjs/core/Cameras/freeCamera.js'
import type { InspectorConfiguration } from './types'

export const defaultInspector: InspectorConfiguration = { speed: 0.12, boostMultiplier: 2, maxDeltaMs: 50 }
const movementKeys = new Set(['KeyW', 'KeyA', 'KeyS', 'KeyD', 'KeyQ', 'KeyE', 'ShiftLeft', 'ShiftRight'])

/** Mevcut render döngüsünden step çağrılır. Sadece odaklı canvas klavye girdisi alır. */
export function createInspectorNavigation(camera: FreeCamera, canvas: HTMLCanvasElement, configuration: InspectorConfiguration, changed: (enabled: boolean) => void, host = { window, document }, ground?: { groundHeight: number; eyeHeight: number }) {
  const keys = new Set<string>()
  let enabled = false
  let visible = true
  let disposed = false
  let wasLocked = host.document.pointerLockElement === canvas
  const localForward = new Vector3(0, 0, camera.getScene().useRightHandedSystem ? -1 : 1)
  let horizontal = localForward.clone()
  const clearInput = () => keys.clear()
  const active = () => enabled && visible && !host.document.hidden && host.document.activeElement === canvas && host.document.hasFocus()
  const setEnabled = (value: boolean) => {
    clearInput()
    if (disposed || enabled === value) return
    enabled = value
    if (value && ground) camera.position.y = ground.groundHeight + ground.eyeHeight
    if (!value && host.document.pointerLockElement === canvas) host.document.exitPointerLock()
    changed(value)
  }
  const down = (event: KeyboardEvent) => {
    if (!active() || event.target !== canvas || event.altKey || event.ctrlKey || event.metaKey || !movementKeys.has(event.code) || (ground && (event.code === 'KeyQ' || event.code === 'KeyE'))) return
    if (event.repeat && !keys.has(event.code)) return
    keys.add(event.code)
    event.preventDefault()
  }
  const up = (event: KeyboardEvent) => { keys.delete(event.code) }
  const focus = () => { if (host.document.activeElement !== canvas) clearInput() }
  const blur = () => setEnabled(false)
  const visibility = () => { if (host.document.hidden) setEnabled(false) }
  const lock = () => {
    const locked = host.document.pointerLockElement === canvas
    if (wasLocked && !locked) setEnabled(false)
    wasLocked = locked
  }
  host.document.addEventListener('keydown', down)
  host.document.addEventListener('keyup', up)
  host.document.addEventListener('focusin', focus)
  host.document.addEventListener('visibilitychange', visibility)
  host.document.addEventListener('pointerlockchange', lock)
  host.window.addEventListener('blur', blur)
  return {
    setEnabled, clearInput,
    isEnabled: () => enabled,
    setVisible(value: boolean) { visible = value; if (!visible) clearInput() },
    step(deltaMs: number) {
      if (!active()) { clearInput(); return }
      if (keys.size === 0 || !Number.isFinite(deltaMs) || deltaMs <= 0) return
      const direction = camera.getDirection(localForward)
      const flat = new Vector3(direction.x, 0, direction.z)
      if (flat.lengthSquared() > 0.000001) horizontal = flat.normalize()
      const right = camera.getScene().useRightHandedSystem ? Vector3.Cross(horizontal, Vector3.Up()) : Vector3.Cross(Vector3.Up(), horizontal)
      const movement = (ground ? horizontal : direction.normalize()).scale(Number(keys.has('KeyW')) - Number(keys.has('KeyS')))
        .add(right.scale(Number(keys.has('KeyD')) - Number(keys.has('KeyA'))))
        .add(new Vector3(0, ground ? 0 : Number(keys.has('KeyE')) - Number(keys.has('KeyQ')), 0))
      if (movement.lengthSquared() === 0) return
      const speed = Math.max(0.01, Math.min(1, configuration.speed))
      const boost = keys.has('ShiftLeft') || keys.has('ShiftRight') ? Math.max(1, Math.min(3, configuration.boostMultiplier)) : 1
      const elapsed = Math.min(deltaMs, Math.max(1, Math.min(100, configuration.maxDeltaMs))) / 1000
      const position = camera.position.add(movement.normalize().scale(speed * boost * elapsed))
      if (configuration.bounds) {
        const { min, max } = configuration.bounds
        position.x = Math.max(min[0], Math.min(max[0], position.x))
        position.y = Math.max(min[1], Math.min(max[1], position.y))
        position.z = Math.max(min[2], Math.min(max[2], position.z))
      }
      if (ground) position.y = ground.groundHeight + ground.eyeHeight
      camera.position.copyFrom(position)
    },
    dispose() {
      setEnabled(false)
      disposed = true
      host.document.removeEventListener('keydown', down)
      host.document.removeEventListener('keyup', up)
      host.document.removeEventListener('focusin', focus)
      host.document.removeEventListener('visibilitychange', visibility)
      host.document.removeEventListener('pointerlockchange', lock)
      host.window.removeEventListener('blur', blur)
    },
  }
}
