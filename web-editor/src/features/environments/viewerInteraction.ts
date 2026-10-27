import { Vector3, Matrix } from '@babylonjs/core/Maths/math.vector.js'
import { pitchLimit, poseOrientation, setOrientation, stabilizeOrientation, shortestYawDelta, type YawPitch } from './cameraOrientation.ts'
import { isCameraPathClear } from './cameraPath.ts'
import { createInspectorNavigation, defaultInspector } from './inspectorNavigation.ts'
import { transitionDuration } from './transitionTiming.ts'
import type { FreeCamera } from '@babylonjs/core/Cameras/freeCamera'
import type { Scene } from '@babylonjs/core/scene'
import type { TransformNode } from '@babylonjs/core/Meshes/transformNode'
import type { CameraPose, InteractionConfiguration, Vector3Tuple } from './types'

export const defaultInteraction: InteractionConfiguration = {
  zoom: { minDegrees: 25, maxDegrees: 85, sensitivity: 0.035, durationMs: 180 },
  transition: { durationMs: 650, clearance: 0.02 },
}
export interface ViewerInteraction {
  capture: () => CameraPose
  move: (pose: CameraPose) => Promise<CameraMoveResult>
  cancelMove: () => void
  isAtPose: (pose: CameraPose) => boolean
  apply: (pose: CameraPose) => void
  setPlacing: (placing: boolean) => void
  setInspector: (enabled: boolean) => void
  isInspectorEnabled: () => boolean
  setWalking: (enabled: boolean) => void
  isWalkingEnabled: () => boolean
  dispose: () => void
}
export type CameraMoveResult = 'completed' | 'cancelled' | 'blocked'
export interface MarkerProjection { position: Vector3Tuple; element: HTMLElement }

export function createViewerInteraction(
  scene: Scene, camera: FreeCamera, root: TransformNode, canvas: HTMLCanvasElement,
  config: InteractionConfiguration,
  callbacks: {
    place: (position: Vector3Tuple, targetNode: string) => void
    message: (message: string) => void
    transitioning: (active: boolean) => void
    markers: () => MarkerProjection[]
    inspectorChanged?: (enabled: boolean) => void
    walkingChanged?: (enabled: boolean) => void
  },
): ViewerInteraction {
  let placing = false
  let pointerStart: { x: number; y: number } | null = null
  let transition: { from: CameraPose; to: CameraPose; start: number; duration: number; startRotation: YawPitch; endRotation: YawPitch; resolve: (result: CameraMoveResult) => void } | null = null
  let zoom: { from: number; to: number; start: number } | null = null
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  const limit = pitchLimit(config.look?.maxPitchDegrees)
  stabilizeOrientation(camera, limit)
  // Runs after Babylon applies mouse/inertia, before its view matrix is computed.
  const orientationObserver = camera.onAfterCheckInputsObservable.add(() => stabilizeOrientation(camera, limit))
  const inspector = createInspectorNavigation(camera, canvas, config.inspector ?? defaultInspector, enabled => callbacks.inspectorChanged?.(enabled))
  const walking = config.walking ? createInspectorNavigation(camera, canvas, config.walking, enabled => callbacks.walkingChanged?.(enabled), { window, document }, config.walking) : null
  const intersection = typeof IntersectionObserver === 'undefined' ? undefined : new IntersectionObserver(entries => { const visible = entries.some(entry => entry.isIntersecting); inspector.setVisible(visible); walking?.setVisible(visible) })
  intersection?.observe(canvas)
  const capture = (): CameraPose => ({ position: camera.position.asArray() as [number, number, number], target: camera.position.add(camera.getForwardRay().direction).asArray() as [number, number, number], fovDegrees: camera.fov * 180 / Math.PI })
  const attach = () => { if (!placing) camera.attachControl(canvas, true) }
  const stop = () => {
    inspector.clearInput()
    walking?.clearInput()
    if (transition) { const pending = transition; transition = null; callbacks.transitioning(false); attach(); pending.resolve('cancelled') }
    zoom = null
    camera.cameraRotation.set(0, 0)
    camera.movement.resetRotationVelocity()
    camera.movement.rotationAccumulatedPixels.setAll(0)
  }
  const apply = (pose: CameraPose) => {
    inspector.setEnabled(false)
    walking?.setEnabled(false)
    stop()
    camera.position.copyFromFloats(...pose.position)
    setOrientation(camera, poseOrientation(camera, pose, limit), limit)
    camera.fov = Math.max(config.zoom.minDegrees, Math.min(config.zoom.maxDegrees, pose.fovDegrees)) * Math.PI / 180
  }
  const modelMeshes = new Set(root.getChildMeshes())
  const isAtPose = (pose: CameraPose) => {
    const target = poseOrientation(camera, pose, limit)
    const fov = Math.max(config.zoom.minDegrees, Math.min(config.zoom.maxDegrees, pose.fovDegrees))
    return !transition && Vector3.Distance(camera.position, Vector3.FromArray(pose.position)) < 0.0001 &&
      Math.abs(shortestYawDelta(camera.rotation.y, target.yaw)) < 0.0001 && Math.abs(camera.rotation.x - target.pitch) < 0.0001 &&
      Math.abs(camera.fov * 180 / Math.PI - fov) < 0.01
  }
  // Babylon'un otomatik pointer-move picking'i yerine yalnızca bilinçli yerleştirme tıklaması.
  scene.skipPointerMovePicking = true
  scene.skipPointerDownPicking = true
  scene.skipPointerUpPicking = true
  const move = (pose: CameraPose): Promise<CameraMoveResult> => {
    inspector.setEnabled(false)
    walking?.setEnabled(false)
    stop()
    if (isAtPose(pose)) return Promise.resolve('completed')
    if (!isCameraPathClear(scene, modelMeshes, camera.position, Vector3.FromArray(pose.position), config.transition.clearance)) {
      callbacks.message('Transition blocked by model geometry. Calibrate a clear camera path; no automatic detour is applied.')
      return Promise.resolve('blocked')
    }
    const from = capture()
    const startRotation = { yaw: camera.rotation.y, pitch: camera.rotation.x }
    const endRotation = poseOrientation(camera, pose, limit)
    const duration = transitionDuration(config.transition.durationMs, Vector3.Distance(camera.position, Vector3.FromArray(pose.position)), Math.hypot(shortestYawDelta(startRotation.yaw, endRotation.yaw), endRotation.pitch - startRotation.pitch), pose.fovDegrees - from.fovDegrees, reducedMotion)
    camera.detachControl()
    callbacks.transitioning(true)
    return new Promise(resolve => {
      transition = { from, to: { ...pose, fovDegrees: Math.max(config.zoom.minDegrees, Math.min(config.zoom.maxDegrees, pose.fovDegrees)) }, start: performance.now(), duration, startRotation, endRotation, resolve }
    })
  }
  const wheel = (event: WheelEvent) => {
    if (event.ctrlKey || event.metaKey) return // Tarayıcı erişilebilirlik zoom'u korunur.
    event.preventDefault()
    if (transition || placing) return
    const pixels = event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? canvas.clientHeight : 1)
    const current = camera.fov * 180 / Math.PI
    const target = Math.max(config.zoom.minDegrees, Math.min(config.zoom.maxDegrees, (zoom?.to ?? current) + Math.max(-240, Math.min(240, pixels)) * config.zoom.sensitivity))
    zoom = { from: current, to: target, start: performance.now() }
  }
  const down = (event: PointerEvent) => {
    if (event.button !== 0) return
    if (transition) stop()
    if (inspector.isEnabled() || walking?.isEnabled()) canvas.focus({ preventScroll: true })
    pointerStart = { x: event.clientX, y: event.clientY }
  }
  const up = (event: PointerEvent) => {
    const start = pointerStart
    pointerStart = null
    if (!placing || !start || event.button !== 0 || Math.hypot(event.clientX - start.x, event.clientY - start.y) > 5) return
    const bounds = canvas.getBoundingClientRect()
    const hit = scene.pick(event.clientX - bounds.left, event.clientY - bounds.top, mesh => modelMeshes.has(mesh))
    if (!hit?.pickedPoint || !hit.pickedMesh) { callbacks.message('No model surface here. Click visible geometry.'); return }
    const local = Vector3.TransformCoordinates(hit.pickedPoint, Matrix.Invert(root.computeWorldMatrix(true)))
    callbacks.place(local.asArray() as [number, number, number], hit.pickedMesh.name)
  }
  const cancelPointer = () => { pointerStart = null }
  canvas.addEventListener('wheel', wheel, { passive: false })
  canvas.addEventListener('pointerdown', down)
  canvas.addEventListener('pointerup', up)
  canvas.addEventListener('pointercancel', cancelPointer)
  let previousFrame = performance.now()
  const before = scene.onBeforeRenderObservable.add(() => {
    const now = performance.now()
    inspector.step(now - previousFrame)
    walking?.step(now - previousFrame)
    previousFrame = now
    if (transition) {
      const t = Math.min(1, (now - transition.start) / transition.duration)
      const eased = t * t * (3 - 2 * t)
      camera.position.copyFrom(Vector3.Lerp(Vector3.FromArray(transition.from.position), Vector3.FromArray(transition.to.position), eased))
      setOrientation(camera, {
        yaw: transition.startRotation.yaw + shortestYawDelta(transition.startRotation.yaw, transition.endRotation.yaw) * eased,
        pitch: transition.startRotation.pitch + (transition.endRotation.pitch - transition.startRotation.pitch) * eased,
      }, limit)
      camera.fov = (transition.from.fovDegrees + (transition.to.fovDegrees - transition.from.fovDegrees) * eased) * Math.PI / 180
      if (t === 1) { const completed = transition; transition = null; callbacks.transitioning(false); attach(); completed.resolve('completed') }
    } else if (zoom) {
      const t = reducedMotion ? 1 : Math.min(1, (now - zoom.start) / Math.max(50, Math.min(600, config.zoom.durationMs)))
      camera.fov = (zoom.from + (zoom.to - zoom.from) * (1 - (1 - t) ** 3)) * Math.PI / 180
      if (t === 1) zoom = null
    }
  })
  const after = scene.onAfterRenderObservable.add(() => {
    const viewport = camera.viewport.toGlobal(canvas.clientWidth, canvas.clientHeight)
    const matrix = root.getWorldMatrix()
    for (const marker of callbacks.markers()) {
      const point = Vector3.Project(Vector3.FromArray(marker.position), matrix, scene.getTransformMatrix(), viewport)
      const world = Vector3.TransformCoordinates(Vector3.FromArray(marker.position), matrix)
      const inFront = Vector3.Dot(world.subtract(camera.position), camera.getForwardRay().direction) > 0
      const visible = inFront && point.z >= 0 && point.z <= 1 && point.x > 10 && point.x < canvas.clientWidth - 10 && point.y > 10 && point.y < canvas.clientHeight - 10
      marker.element.style.visibility = visible ? 'visible' : 'hidden'
      marker.element.style.transform = `translate(${point.x}px, ${point.y}px) translate(-50%, -50%)`
    }
  })
  return {
    capture, move, apply, cancelMove() { inspector.setEnabled(false); walking?.setEnabled(false); stop() }, isAtPose,
    setInspector(value) { walking?.setEnabled(false); if (value && !placing && !transition) { stop(); inspector.setEnabled(true); canvas.focus({ preventScroll: true }) } else inspector.setEnabled(false) },
    isInspectorEnabled: inspector.isEnabled,
    setWalking(value) { inspector.setEnabled(false); if (value && !placing && !transition && walking) { stop(); walking.setEnabled(true); canvas.focus({ preventScroll: true }) } else walking?.setEnabled(false) },
    isWalkingEnabled: () => walking?.isEnabled() ?? false,
    setPlacing(value) { if (placing === value) return; placing = value; inspector.setEnabled(false); walking?.setEnabled(false); stop(); if (placing) camera.detachControl(); else attach() },
    dispose() {
      intersection?.disconnect()
      inspector.dispose()
      walking?.dispose()
      stop()
      scene.onBeforeRenderObservable.remove(before)
      camera.onAfterCheckInputsObservable.remove(orientationObserver)
      scene.onAfterRenderObservable.remove(after)
      canvas.removeEventListener('wheel', wheel)
      canvas.removeEventListener('pointerdown', down)
      canvas.removeEventListener('pointerup', up)
      canvas.removeEventListener('pointercancel', cancelPointer)
      modelMeshes.clear()
    },
  }
}
