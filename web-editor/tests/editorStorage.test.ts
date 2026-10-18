import { test } from 'node:test'
import assert from 'node:assert/strict'
import { emptyEditor, editorKey, loadEditor, parseEditor, saveEditor } from '../src/features/environments/editorStorage.ts'
import type { SceneDefinition } from '../src/features/environments/types.ts'

const definition: SceneDefinition = {
  id: 'storage-test', displayName: 'Test', modelUrl: '/test.glb',
  modelTransform: { position: [3, 2, 1], rotation: [0, 0.5, 0], scale: [2, 2, 2] },
  navigationMode: 'seated', camera: { position: [0, 1, 0], target: [0, 1, 1], fovDegrees: 70, near: 0.01, far: 100, calibration: 'provisional' },
}
const fixture = () => ({ ...emptyEditor(definition), zones: [{ id: 'zone', title: 'Reviewed view', environmentId: definition.id, calibration: 'reviewed' as const, camera: definition.camera }], hotspots: [{ id: 'spot', title: 'Training note', description: 'User content', position: [1, 2, 3] as const, zoneId: 'zone' }] })

test('versioned environment record round-trips without touching project storage', () => {
  const writes = new Map<string, string>()
  const storage = { setItem: (key: string, value: string) => { writes.set(key, value) }, getItem: (key: string) => writes.get(key) ?? null }
  assert.equal(saveEditor(storage, definition, fixture()), '')
  assert.equal(writes.size, 1)
  assert.equal(writes.has(editorKey(definition.id)), true)
  assert.equal(writes.has('workplace-studio.projects.v1'), false)
  assert.deepEqual(loadEditor(storage, definition).data, fixture())
})

test('malformed JSON and unknown versions are preserved read-only', () => {
  for (const raw of ['{broken', JSON.stringify({ ...fixture(), schemaVersion: 99 })]) {
    const result = loadEditor({ getItem: () => raw }, definition)
    assert.equal(result.writable, false)
    assert.ok(result.error)
    assert.deepEqual(result.data, emptyEditor(definition))
  }
})

test('rejects invalid positions, duplicate IDs and model mismatches', () => {
  const valid = fixture()
  for (const data of [
    { ...valid, hotspots: [{ ...valid.hotspots[0], position: [1, null, 3] }] },
    { ...valid, hotspots: [valid.hotspots[0], valid.hotspots[0]] },
    { ...valid, environmentId: 'different-environment' },
    { ...valid, modelUrl: '/different.glb' },
    { ...valid, modelTransform: { ...valid.modelTransform, scale: [1, 1, 1] } },
    { ...valid, zones: [{ ...valid.zones[0], camera: { ...definition.camera, target: definition.camera.position } }] },
  ]) assert.throws(() => parseEditor(JSON.stringify(data), definition))
})

test('missing zone references load with a warning without rewriting or migrating records', () => {
  const data = { ...fixture(), zones: [] }
  const raw = JSON.stringify(data)
  const loaded = loadEditor({ getItem: () => raw }, definition)
  assert.equal(loaded.writable, true)
  assert.match(loaded.error, /missing inspection zones/)
  assert.deepEqual(loaded.data, data)
  assert.equal(loaded.data.schemaVersion, 1)
})

test('quota and permission failures report errors and do not claim a save', () => {
  const result = saveEditor({ setItem: () => { throw new Error('Quota exceeded') } }, definition, fixture())
  assert.ok(result)
  assert.equal(loadEditor({ getItem: () => { throw new Error('Denied') } }, definition).writable, false)
})

test('two environments with identical annotation ids remain isolated without changing schema or project data', () => {
  const second: SceneDefinition = { ...definition, id: 'hospital-test', modelUrl: '/hospital.glb', navigationMode: 'walking' }
  const writes = new Map<string, string>([['workplace-studio.projects.v1', '[{"id":"existing-project"}]']])
  const storage = { setItem: (key: string, value: string) => { writes.set(key, value) }, getItem: (key: string) => writes.get(key) ?? null }
  assert.equal(saveEditor(storage, definition, fixture()), '')
  const firstRaw = writes.get(editorKey(definition.id))
  const hospital = { ...emptyEditor(second), zones: [{ ...fixture().zones[0], environmentId: second.id }], hotspots: [{ ...fixture().hotspots[0], title: 'Hospital-only user note' }] }
  assert.equal(saveEditor(storage, second, hospital), '')
  assert.equal(writes.get(editorKey(definition.id)), firstRaw)
  assert.deepEqual(loadEditor(storage, definition).data, fixture())
  assert.deepEqual(loadEditor(storage, second).data, hospital)
  assert.equal(loadEditor(storage, second).data.schemaVersion, 1)
  assert.equal(writes.get('workplace-studio.projects.v1'), '[{"id":"existing-project"}]')
  assert.throws(() => parseEditor(firstRaw!, second), 'cross-environment records cannot be silently loaded')
})
