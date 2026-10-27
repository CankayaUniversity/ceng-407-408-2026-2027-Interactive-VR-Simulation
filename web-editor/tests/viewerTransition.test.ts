import { test } from 'node:test'
import assert from 'node:assert/strict'
import { NullEngine } from '@babylonjs/core/Engines/nullEngine.js'
import { Scene } from '@babylonjs/core/scene.js'
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera.js'
import { TransformNode } from '@babylonjs/core/Meshes/transformNode.js'
import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import { createViewerInteraction, defaultInteraction } from '../src/features/environments/viewerInteraction.ts'

test('reduced motion controller still completes on a rendered frame with stable world-up', async () => {
  const savedWindow = Object.getOwnPropertyDescriptor(globalThis, 'window')
  const savedDocument = Object.getOwnPropertyDescriptor(globalThis, 'document')
  const savedNow = Object.getOwnPropertyDescriptor(performance, 'now')
  let time = 0
  Object.defineProperty(globalThis, 'window', { configurable: true, value: Object.assign(new EventTarget(), { matchMedia: () => ({ matches: true }) }) })
  Object.defineProperty(globalThis, 'document', { configurable: true, value: Object.assign(new EventTarget(), { activeElement: null, hidden: false, hasFocus: () => true }) })
  Object.defineProperty(performance, 'now', { configurable: true, value: () => time })
  const engine = new NullEngine(), scene = new Scene(engine)
  const camera = new FreeCamera('reduced-camera', new Vector3(0, 1, 0), scene)
  camera.attachControl = () => {}; camera.detachControl = () => {}
  const root = new TransformNode('root', scene)
  const canvas = Object.assign(new EventTarget(), { focus() {} }) as HTMLCanvasElement
  const viewer = createViewerInteraction(scene, camera, root, canvas, defaultInteraction, { place() {}, message() {}, transitioning() {}, markers: () => [] })
  try {
    let completed = false
    const pending = viewer.move({ position: [0, 1, 0], target: [1, 1, -1], fovDegrees: 40 }).then(result => { completed = true; return result })
    await Promise.resolve(); assert.equal(completed, false)
    time = 1; scene.onBeforeRenderObservable.notifyObservers(scene)
    assert.equal(await pending, 'completed')
    assert.equal(camera.rotation.z, 0)
    assert.ok(camera.upVector.equalsWithEpsilon(Vector3.UpReadOnly))
  } finally {
    viewer.dispose(); scene.dispose(); engine.dispose()
    for (const [key, descriptor] of [['window', savedWindow], ['document', savedDocument]] as const) {
      if (descriptor) Object.defineProperty(globalThis, key, descriptor); else Reflect.deleteProperty(globalThis, key)
    }
    if (savedNow) Object.defineProperty(performance, 'now', savedNow); else Reflect.deleteProperty(performance, 'now')
  }
})

test('real controller resolves only on final frame and settles supersession, cancellation and dispose', async () => {
  const windowDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'window')
  const documentDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'document')
  const nowDescriptor = Object.getOwnPropertyDescriptor(performance, 'now')
  let time = 0
  Object.defineProperty(globalThis, 'window', { configurable: true, value: Object.assign(new EventTarget(), { matchMedia: () => ({ matches: false }) }) })
  Object.defineProperty(globalThis, 'document', { configurable: true, value: Object.assign(new EventTarget(), { activeElement: null, hidden: false, hasFocus: () => true }) })
  Object.defineProperty(performance, 'now', { configurable: true, value: () => time })
  const engine = new NullEngine(), scene = new Scene(engine)
  scene.useRightHandedSystem = true
  const camera = new FreeCamera('test-camera', new Vector3(0, 1, 0), scene)
  camera.setTarget(new Vector3(0, 1, 1)); camera.fov = 70 * Math.PI / 180
  // Test yalnız kamera/animasyon sözleşmesidir; native input tarayıcı testine aittir.
  camera.attachControl = () => {}; camera.detachControl = () => {}
  const root = new TransformNode('test-root', scene)
  const canvas = Object.assign(new EventTarget(), { focus() {} }) as HTMLCanvasElement
  const states: boolean[] = []
  const viewer = createViewerInteraction(scene, camera, root, canvas, { ...defaultInteraction, walking: { speed: .8, boostMultiplier: 2, maxDeltaMs: 50, groundHeight: 0, eyeHeight: 1.65 } }, { place() {}, message() {}, transitioning: value => states.push(value), markers: () => [] })
  try {
    const pose = { position: [0, 1, 0] as const, target: [0.3, 0.9, 1] as const, fovDegrees: 45 }
    let completed = false
    const motion = viewer.move(pose).then(result => { completed = true; return result })
    time = 325; scene.onBeforeRenderObservable.notifyObservers(scene)
    await Promise.resolve()
    assert.equal(completed, false)
    time = 650; scene.onBeforeRenderObservable.notifyObservers(scene)
    assert.equal(await motion, 'completed')
    assert.equal(viewer.isAtPose(pose), true)
    const count = states.length
    assert.equal(await viewer.move(pose), 'completed')
    assert.equal(states.length, count, 'same physical pose does not start another animation')
    const other = { ...pose, target: [0, 1, 1] as const, fovDegrees: 70 }
    const first = viewer.move(other)
    const second = viewer.move(pose)
    assert.equal(await first, 'cancelled')
    assert.equal(await second, 'completed')
    const cancelled = viewer.move(other); viewer.cancelMove()
    assert.equal(await cancelled, 'cancelled')
    for (const position of [[0, 1, 0], [0.4, 2.15, 1.1], [-1, 3, -2]] as const) {
      const overhead = { position, target: [position[0], position[1] + 2, position[2]] as const, fovDegrees: 50 }
      viewer.apply(overhead)
      assert.deepEqual(camera.position.asArray(), [...position])
      assert.ok(Math.abs(camera.rotation.x - 85 * Math.PI / 180) < 1e-8)
      for (let i = 0; i < 5; i++) {
        camera.cameraRotation.x = 2
        camera.update()
        assert.ok(camera.rotation.x <= 85 * Math.PI / 180)
        assert.equal(camera.rotation.z, 0)
        assert.ok(camera.upVector.equalsWithEpsilon(Vector3.UpReadOnly, 1e-8))
      }
      const saved = JSON.parse(JSON.stringify(viewer.capture()))
      assert.deepEqual(saved.position, [...position])
      const turn = viewer.move({ ...saved, target: [position[0] + 1, position[1], position[2] - 1] })
      time += 650; scene.onBeforeRenderObservable.notifyObservers(scene)
      assert.equal(await turn, 'completed')
      const restore = viewer.move(saved)
      time += 325; scene.onBeforeRenderObservable.notifyObservers(scene)
      assert.equal(camera.rotation.z, 0)
      time += 325; scene.onBeforeRenderObservable.notifyObservers(scene)
      assert.equal(await restore, 'completed')
      assert.ok(viewer.isAtPose(saved), 'captured overhead pose restores exactly')
    }
    viewer.setWalking(true)
    assert.equal(viewer.isWalkingEnabled(), true)
    viewer.setInspector(true)
    assert.equal(viewer.isWalkingEnabled(), false, 'Spectator and Walking never run together')
    assert.equal(viewer.isInspectorEnabled(), true)
    viewer.setWalking(true)
    assert.equal(viewer.isInspectorEnabled(), false)
    viewer.cancelMove()
    assert.equal(viewer.isWalkingEnabled(), false, 'navigation cancellation clears walking')
    viewer.setWalking(true); viewer.setPlacing(true)
    assert.equal(viewer.isWalkingEnabled(), false, 'placement cannot run while walking')
    viewer.setPlacing(false)
    viewer.setWalking(true); viewer.apply(pose)
    assert.equal(viewer.isWalkingEnabled(), false, 'numeric camera application stops walking')
    viewer.setWalking(true)
    const disposing = viewer.move(other)
    assert.equal(viewer.isWalkingEnabled(), false, 'zone transition stops walking before movement')
    viewer.dispose()
    assert.equal(await disposing, 'cancelled')
  } finally {
    viewer.dispose(); scene.dispose(); engine.dispose()
    if (windowDescriptor) Object.defineProperty(globalThis, 'window', windowDescriptor); else Reflect.deleteProperty(globalThis, 'window')
    if (documentDescriptor) Object.defineProperty(globalThis, 'document', documentDescriptor); else Reflect.deleteProperty(globalThis, 'document')
    if (nowDescriptor) Object.defineProperty(performance, 'now', nowDescriptor); else Reflect.deleteProperty(performance, 'now')
  }
})
