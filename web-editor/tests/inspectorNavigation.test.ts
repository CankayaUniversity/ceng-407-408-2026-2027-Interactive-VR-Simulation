import { test } from 'node:test'
import assert from 'node:assert/strict'
import { NullEngine } from '@babylonjs/core/Engines/nullEngine.js'
import { Scene } from '@babylonjs/core/scene.js'
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera.js'
import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import { createInspectorNavigation, defaultInspector } from '../src/features/environments/inspectorNavigation.ts'
import { pitchLimit, poseOrientation, setOrientation } from '../src/features/environments/cameraOrientation.ts'

function setup(ground?: { groundHeight: number; eyeHeight: number }) {
  const engine = new NullEngine(), scene = new Scene(engine)
  scene.useRightHandedSystem = true
  const camera = new FreeCamera('inspector-test', new Vector3(0, 1, 0), scene)
  camera.setTarget(new Vector3(0, 1, 1))
  const canvas = new EventTarget() as HTMLCanvasElement
  const doc = Object.assign(new EventTarget(), { activeElement: canvas as EventTarget | null, hidden: false, hasFocus: () => true })
  const win = new EventTarget()
  const changes: boolean[] = []
  const configuration = ground ? { ...defaultInspector, speed: 0.8, bounds: { min: [-1, 0, -1] as const, max: [1, 5, 1] as const } } : defaultInspector
  const navigation = createInspectorNavigation(camera, canvas, configuration, value => changes.push(value), { window: win as Window, document: doc as unknown as Document }, ground)
  function key(type: 'keydown' | 'keyup', code: string, target: EventTarget = canvas, modifiers = {}) {
    const event = Object.assign(new Event(type, { cancelable: true }), { code, altKey: false, ctrlKey: false, metaKey: false, repeat: false, ...modifiers })
    Object.defineProperty(event, 'target', { value: target })
    doc.dispatchEvent(event)
    return event
  }
  return { engine, scene, camera, canvas, doc, win, navigation, changes, key, dispose() { navigation.dispose(); scene.dispose(); engine.dispose() } }
}

test('Spectator WASD follows facing direction at different yaw/pitch and leaves orientation unchanged', () => {
  const f = setup()
  try {
    f.navigation.setEnabled(true)
    for (const [target, expected] of [
      [[0, 1, 1], [0, 0, 1]], [[1, 1, 0.25], [1, 0, 0.25]], [[0, 4, 1], [0, 3, 1]], [[0, 1, -1], [0, 0, -1]],
    ]) {
      f.camera.position.set(0, 1, 0); f.camera.setTarget(Vector3.FromArray(target))
      const rotation = f.camera.rotation.clone(), start = f.camera.position.clone()
      f.key('keydown', 'KeyW'); f.navigation.step(50); f.key('keyup', 'KeyW')
      const displacement = f.camera.position.subtract(start)
      assert.ok(Vector3.Distance(displacement, Vector3.FromArray(expected).normalize().scale(0.006)) < 0.000001, JSON.stringify({ target, expected, actual: displacement.asArray() }))
      assert.deepEqual(f.camera.rotation.asArray(), rotation.asArray())
      f.key('keydown', 'KeyS'); f.navigation.step(50); f.key('keyup', 'KeyS')
      assert.ok(Vector3.Distance(f.camera.position, start) < 0.000001)
    }
    f.camera.position.set(0, 1, 0); f.camera.setTarget(new Vector3(0, 1, 1))
    f.key('keydown', 'KeyD'); f.navigation.step(50); f.key('keyup', 'KeyD')
    assert.ok(f.camera.position.x < 0, 'RH forward +Z means screen-right is world -X')
    f.key('keydown', 'KeyA'); f.navigation.step(50); f.key('keyup', 'KeyA')
    assert.ok(Math.abs(f.camera.position.x) < 0.000001)
  } finally { f.dispose() }
})

test('delta clamp, diagonal normalization, world-up Q/E and moderate Shift boost are bounded', () => {
  const f = setup()
  try {
    f.navigation.setEnabled(true)
    const start = f.camera.position.clone()
    f.key('keydown', 'KeyW'); f.navigation.step(25); f.navigation.step(25); f.key('keyup', 'KeyW')
    assert.ok(Math.abs(Vector3.Distance(f.camera.position, start) - 0.006) < 0.000001, 'two 25 ms frames equal one 50 ms step')
    f.camera.position.copyFrom(start)
    f.key('keydown', 'KeyW'); f.key('keydown', 'KeyD'); f.navigation.step(10000)
    assert.ok(Math.abs(Vector3.Distance(f.camera.position, start) - 0.006) < 0.000001)
    f.navigation.clearInput(); f.camera.position.copyFrom(start)
    f.key('keydown', 'KeyE'); f.key('keydown', 'ShiftLeft'); f.navigation.step(50)
    assert.ok(Math.abs(f.camera.position.y - 1.012) < 0.000001)
    f.navigation.clearInput()
    f.key('keydown', 'KeyQ'); f.navigation.step(50)
    assert.ok(Math.abs(f.camera.position.y - 1.006) < 0.000001)
  } finally { f.dispose() }
})

test('ordinary UI/text focus is never intercepted and clears held input immediately', () => {
  const f = setup()
  try {
    f.navigation.setEnabled(true)
    assert.equal(f.key('keydown', 'KeyW').defaultPrevented, true)
    const field = new EventTarget()
    f.doc.activeElement = field; f.doc.dispatchEvent(new Event('focusin'))
    const before = f.camera.position.clone()
    for (const code of ['KeyW', 'KeyA', 'KeyS', 'KeyD', 'KeyQ', 'KeyE', 'ShiftLeft']) assert.equal(f.key('keydown', code, field).defaultPrevented, false)
    f.navigation.step(50)
    assert.deepEqual(f.camera.position.asArray(), before.asArray())
    f.doc.activeElement = f.canvas
    f.navigation.step(50)
    assert.deepEqual(f.camera.position.asArray(), before.asArray(), 'refocusing never resumes a held key')
    assert.equal(f.key('keydown', 'KeyW', f.canvas, { ctrlKey: true }).defaultPrevented, false)
  } finally { f.dispose() }
})

test('blur, visibility, disabled mode, inactive viewport and disposal stop translation', () => {
  const f = setup()
  try {
    const start = f.camera.position.clone()
    f.navigation.setEnabled(true); f.key('keydown', 'KeyW')
    f.win.dispatchEvent(new Event('blur')); f.navigation.step(50)
    assert.equal(f.navigation.isEnabled(), false)
    f.navigation.setEnabled(true); f.key('keydown', 'KeyW'); f.navigation.setVisible(false); f.navigation.step(50)
    f.navigation.setVisible(true); f.navigation.step(50)
    f.key('keydown', 'KeyW'); f.doc.hidden = true; f.doc.dispatchEvent(new Event('visibilitychange')); f.navigation.step(50)
    f.doc.hidden = false
    f.navigation.setEnabled(true); f.key('keydown', 'KeyW'); f.navigation.setEnabled(false); f.navigation.step(50)
    f.navigation.setEnabled(true); f.key('keydown', 'KeyW'); f.navigation.dispose(); f.navigation.step(50)
    assert.deepEqual(f.camera.position.asArray(), start.asArray())
    assert.equal(f.key('keydown', 'KeyW').defaultPrevented, false)
  } finally { f.dispose() }
})

test('pointer lock exit stops Spectator and disabling Spectator releases a held lock', () => {
  const f = setup()
  const doc = f.doc as typeof f.doc & { pointerLockElement: EventTarget | null; exitPointerLock: () => void }
  let releases = 0
  doc.pointerLockElement = null
  doc.exitPointerLock = () => { releases++; doc.pointerLockElement = null; doc.dispatchEvent(new Event('pointerlockchange')) }
  try {
    const start = f.camera.position.clone()
    f.navigation.setEnabled(true)
    doc.pointerLockElement = f.canvas; doc.dispatchEvent(new Event('pointerlockchange'))
    f.key('keydown', 'KeyW')
    doc.pointerLockElement = null; doc.dispatchEvent(new Event('pointerlockchange'))
    f.navigation.step(50)
    assert.equal(f.navigation.isEnabled(), false)
    assert.deepEqual(f.camera.position.asArray(), start.asArray())
    f.navigation.setEnabled(true)
    doc.pointerLockElement = f.canvas; doc.dispatchEvent(new Event('pointerlockchange'))
    f.navigation.setEnabled(false)
    assert.equal(releases, 1)
    assert.equal(doc.pointerLockElement, null)
  } finally { f.dispose() }
})

test('Walking stays grounded at multiple pitched yaw angles, ignores QE and preserves facing', () => {
  const f = setup({ groundHeight: -.1, eyeHeight: 1.75 })
  try {
    f.navigation.setEnabled(true)
    assert.equal(f.camera.position.y, 1.65)
    for (const direction of [[0, 9, 1], [1, -9, 0], [0, 9, -1], [-1, -9, 0]]) {
      f.camera.position.set(0, 1.65, 0)
      const pose = { position: f.camera.position.asArray() as [number, number, number], target: f.camera.position.add(Vector3.FromArray(direction)).asArray() as [number, number, number], fovDegrees: 70 }
      setOrientation(f.camera, poseOrientation(f.camera, pose, pitchLimit()), pitchLimit())
      const rotation = f.camera.rotation.clone()
      f.key('keydown', 'KeyW'); f.navigation.step(50); f.key('keyup', 'KeyW')
      const expected = new Vector3(direction[0], 0, direction[2]).normalize().scale(.04)
      assert.ok(Vector3.Distance(f.camera.position, expected.add(new Vector3(0, 1.65, 0))) < 1e-6)
      assert.deepEqual(f.camera.rotation.asArray(), rotation.asArray())
      const before = f.camera.position.clone()
      assert.equal(f.key('keydown', 'KeyQ').defaultPrevented, false)
      assert.equal(f.key('keydown', 'KeyE').defaultPrevented, false)
      f.navigation.step(50)
      assert.deepEqual(f.camera.position.asArray(), before.asArray())
    }
    f.camera.position.set(.99, 1.65, 0); f.camera.setTarget(new Vector3(2, 5, 0))
    f.key('keydown', 'KeyW'); f.key('keydown', 'ShiftLeft'); f.navigation.step(99999)
    assert.equal(f.camera.position.x, 1, 'coarse safety bounds do not allow escape')
    assert.equal(f.camera.position.y, 1.65)
    f.navigation.setEnabled(false)
    const stopped = f.camera.position.clone(); f.navigation.step(50)
    assert.deepEqual(f.camera.position.asArray(), stopped.asArray())
  } finally { f.dispose() }
})
