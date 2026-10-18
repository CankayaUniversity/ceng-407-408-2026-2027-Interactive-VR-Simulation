import { test } from 'node:test'
import assert from 'node:assert/strict'
import { NullEngine } from '@babylonjs/core/Engines/nullEngine.js'
import { Scene } from '@babylonjs/core/scene.js'
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera.js'
import { Vector3, Quaternion } from '@babylonjs/core/Maths/math.vector.js'
import '@babylonjs/core/Culling/ray.js'
import { pitchLimit, poseOrientation, setOrientation, stabilizeOrientation, shortestYawDelta } from '../src/features/environments/cameraOrientation.ts'

const rad = (degrees: number) => degrees * Math.PI / 180
function setup(rightHanded = true) {
  const engine = new NullEngine(), scene = new Scene(engine)
  scene.useRightHandedSystem = rightHanded
  const camera = new FreeCamera('orientation-test', new Vector3(.4, 2.15, 1.1), scene)
  return { engine, scene, camera, dispose() { scene.dispose(); engine.dispose() } }
}
function upright(camera: FreeCamera) {
  assert.equal(camera.rotation.z, 0)
  assert.equal(camera.rotationQuaternion, null)
  assert.ok(camera.upVector.equalsWithEpsilon(Vector3.UpReadOnly, 1e-7))
  assert.ok(Vector3.Dot(camera.getDirection(Vector3.UpReadOnly), Vector3.UpReadOnly) > .08)
  assert.ok(Math.abs(camera.rotation.x) <= rad(85) + 1e-8)
}

test('old quaternion/Euler transition generates roll; yaw/pitch path stays upright through overhead angles', () => {
  const f = setup()
  try {
    const start = { yaw: rad(170), pitch: rad(15) }, end = { yaw: rad(-100), pitch: rad(85) }
    const old = Quaternion.Slerp(Quaternion.FromEulerAngles(start.pitch, start.yaw, 0), Quaternion.FromEulerAngles(end.pitch, end.yaw, 0), .5).toEulerAngles()
    assert.ok(Math.abs(old.z) > rad(20), 'reproduces the old transition roll')
    for (let i = 0; i <= 100; i++) {
      const t = i / 100
      setOrientation(f.camera, { yaw: start.yaw + shortestYawDelta(start.yaw, end.yaw) * t, pitch: start.pitch + (end.pitch - start.pitch) * t }, pitchLimit())
      upright(f.camera)
    }
    assert.ok(Math.abs(shortestYawDelta(rad(179), rad(-179)) - rad(2)) < 1e-8)
  } finally { f.dispose() }
})

test('numeric and vertical look targets preserve exact position, world-up and facing for RH and LH', () => {
  for (const rh of [true, false]) {
    const f = setup(rh)
    try {
      for (const position of [[.4, 2.15, 1.1], [0, 3, 0], [-2, -1, 4]]) {
        for (const direction of [[0, 1, 0], [0, -1, 0], [1e-12, 1, 0], [1, .3, -2], [-1, -.3, 2]]) {
          const pose = { position: position as [number, number, number], target: position.map((v, i) => v + direction[i]) as [number, number, number], fovDegrees: 70 }
          f.camera.position.copyFromFloats(...pose.position)
          const oldYaw = f.camera.rotation.y
          const orientation = poseOrientation(f.camera, pose, pitchLimit())
          setOrientation(f.camera, orientation, pitchLimit())
          upright(f.camera)
          assert.deepEqual(f.camera.position.asArray(), position, 'no setTarget epsilon position nudge')
          if (Math.hypot(direction[0], direction[2]) < 1e-8) assert.equal(orientation.yaw, oldYaw)
          else assert.ok(Vector3.Dot(f.camera.getForwardRay().direction, Vector3.FromArray(direction).normalize()) > .999999)
        }
      }
    } finally { f.dispose() }
  }
})

test('repeated mouse pitch overshoot, lingering inertia and stale roll cannot invert view', () => {
  const f = setup()
  try {
    f.camera.rotation.z = rad(45); f.camera.getViewMatrix(true)
    for (const pitch of [100, -100, 100, 100, -100, 89, -89]) {
      f.camera.rotation.set(rad(pitch), f.camera.rotation.y + rad(120), f.camera.rotation.z)
      f.camera.cameraRotation.x = .3
      stabilizeOrientation(f.camera, pitchLimit())
      upright(f.camera)
      assert.equal(f.camera.cameraRotation.x, 0)
    }
    assert.equal(pitchLimit(200), rad(89))
    assert.equal(pitchLimit(NaN), rad(85))
    setOrientation(f.camera, { yaw: 20 * Math.PI, pitch: rad(90) }, pitchLimit(80))
    assert.equal(f.camera.rotation.x, rad(80))
  } finally { f.dispose() }
})
