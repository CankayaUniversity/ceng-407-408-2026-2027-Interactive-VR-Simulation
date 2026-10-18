import { test } from 'node:test'
import assert from 'node:assert/strict'
import { NullEngine } from '@babylonjs/core/Engines/nullEngine.js'
import { Scene } from '@babylonjs/core/scene.js'
import { CreateBox } from '@babylonjs/core/Meshes/Builders/boxBuilder.js'
import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import { isCameraPathClear } from '../src/features/environments/cameraPath.ts'

test('camera path rejects geometry intersections but accepts stationary zoom and clear paths', () => {
  const engine = new NullEngine()
  const scene = new Scene(engine)
  try {
    const wall = CreateBox('test-wall', { width: 2, height: 2, depth: 0.1 }, scene)
    wall.position.z = 1
    wall.computeWorldMatrix(true)
    const model = new Set([wall])
    assert.equal(isCameraPathClear(scene, model, Vector3.Zero(), new Vector3(0, 0, 2), 0.02), false)
    assert.equal(isCameraPathClear(scene, model, Vector3.Zero(), Vector3.Zero(), 0.02), true)
    assert.equal(isCameraPathClear(scene, model, new Vector3(3, 0, 0), new Vector3(3, 0, 2), 0.02), true)
    assert.equal(isCameraPathClear(scene, new Set(), Vector3.Zero(), new Vector3(0, 0, 2), 0.02), true)
  } finally { scene.dispose(); engine.dispose() }
})
