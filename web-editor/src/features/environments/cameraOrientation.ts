import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import type { FreeCamera } from '@babylonjs/core/Cameras/freeCamera.js'
import type { CameraPose } from './types'

export interface YawPitch { yaw: number; pitch: number }

export function pitchLimit(degrees = 85) {
  return Math.max(1, Math.min(89, Number.isFinite(degrees) ? degrees : 85)) * Math.PI / 180
}

/** RH camera looks along local -Z; LH along +Z. Vertical targets retain the previous yaw. */
export function poseOrientation(camera: FreeCamera, pose: CameraPose, limit: number): YawPitch {
  const direction = Vector3.FromArray(pose.target).subtract(Vector3.FromArray(pose.position))
  const length = direction.length()
  if (!Number.isFinite(length) || length < 1e-10) return { yaw: camera.rotation.y, pitch: camera.rotation.x }
  const sign = camera.getScene().useRightHandedSystem ? -1 : 1
  const yaw = Math.hypot(direction.x, direction.z) < length * 1e-8
    ? camera.rotation.y : Math.atan2(sign * direction.x, sign * direction.z)
  const pitch = -sign * Math.asin(Math.max(-1, Math.min(1, direction.y / length)))
  return { yaw, pitch: Math.max(-limit, Math.min(limit, pitch)) }
}

export const shortestYawDelta = (from: number, to: number) => Math.atan2(Math.sin(to - from), Math.cos(to - from))

/** Never call setTarget: its vertical look-at is singular and may nudge position.z. */
export function setOrientation(camera: FreeCamera, orientation: YawPitch, limit: number) {
  camera.rotationQuaternion = null
  camera.updateUpVectorFromRotation = false
  camera.rotation.set(Math.max(-limit, Math.min(limit, orientation.pitch)), orientation.yaw, 0)
  camera.upVector.set(0, 1, 0)
  // Clear Babylon's cached roll/up relationship once when entering from a rolled orientation.
  camera.getViewMatrix(true)
  if (!camera.upVector.equalsWithEpsilon(Vector3.UpReadOnly, 1e-8)) {
    camera.upVector.set(0, 1, 0)
    camera.getViewMatrix(true)
  }
}

export function stabilizeOrientation(camera: FreeCamera, limit: number) {
  const bounded = Math.max(-limit, Math.min(limit, camera.rotation.x))
  if (bounded !== camera.rotation.x) {
    camera.cameraRotation.x = 0
    camera.movement.resetRotationVelocity()
  }
  setOrientation(camera, { yaw: camera.rotation.y, pitch: bounded }, limit)
}
