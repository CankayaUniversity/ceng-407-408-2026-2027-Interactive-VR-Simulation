import { test } from 'node:test'
import assert from 'node:assert/strict'
import { childrenOf, canParent, moveArea, removeArea, reorderArea, searchAreaIds, validateHierarchy, zonePath } from '../src/features/environments/zoneHierarchy.ts'
import { emptyEditor, editorKey, loadEditor, parseEditor, saveEditor } from '../src/features/environments/editorStorage.ts'
import { transitionDuration } from '../src/features/environments/transitionTiming.ts'
import type { SceneDefinition } from '../src/features/environments/types.ts'
import { navigateAndOpenHotspot } from '../src/features/environments/hotspotNavigation.ts'

const definition: SceneDefinition = { id: 'hierarchy-test', displayName: 'Test', modelUrl: '/test.glb', navigationMode: 'seated', modelTransform: { position: [0, 0, 0], rotation: [0, 0, 0], scale: [1, 1, 1] }, camera: { position: [0, 1, 0], target: [0, 1, -1], fovDegrees: 70, near: .01, far: 100, calibration: 'provisional' } }
const area = (id: string, parentZoneId?: string, order?: number) => ({ id, title: id, parentZoneId, order, environmentId: definition.id, calibration: 'reviewed' as const, camera: { position: [0, 1, 0] as const, target: [0, 1, -1] as const, fovDegrees: id === 'A1' ? 35 : 70 } })
const fixture = () => ({ ...emptyEditor(definition), hierarchyVersion: 1 as const, zones: [area('A'), area('A1', 'A', 0), area('A2', 'A', 1), area('A11', 'A1'), area('B')], hotspots: [{ id: 'p1', title: 'Brightness', description: 'Real trainer content', position: [1, 2, 3] as const, zoneId: 'A1' }, { id: 'p2', title: 'General', description: '', position: [0, 1, 0] as const }] })

test('compatible reader loads old v1 without writes; first explicit edit backs up exact raw record once', () => {
  const old = { ...emptyEditor(definition), zones: [area('A')], hotspots: fixture().hotspots }
  const raw = JSON.stringify(old, null, 2), key = editorKey(definition.id)
  const records = new Map([[key, raw], ['workplace-studio.projects.v1', 'existing projects']])
  const storage = { getItem: (k: string) => records.get(k) ?? null, setItem: (k: string, v: string) => { records.set(k, v) } }
  assert.equal(loadEditor(storage, definition).writable, true)
  assert.equal(records.size, 2)
  assert.equal(saveEditor(storage, definition, fixture()), '')
  assert.equal(records.get(key + '.before-hierarchy.v1'), raw)
  assert.deepEqual(loadEditor(storage, definition).data, JSON.parse(JSON.stringify(fixture())))
  assert.equal(saveEditor(storage, definition, { ...fixture(), zones: [...fixture().zones, area('C')] }), '')
  assert.equal(records.get(key + '.before-hierarchy.v1'), raw)
  assert.equal(records.get('workplace-studio.projects.v1'), 'existing projects')
})
test('backup failure prevents replacing old data; invalid current raw cannot be overwritten', () => {
  const raw = JSON.stringify(emptyEditor(definition)), key = editorKey(definition.id)
  let overwritten = false
  const storage = { getItem: (k: string) => k === key ? raw : null, setItem: (k: string) => { if (k !== key) throw new Error('quota'); overwritten = true } }
  assert.ok(saveEditor(storage, definition, fixture()))
  assert.equal(overwritten, false)
  assert.ok(saveEditor({ getItem: () => '{invalid', setItem: () => { overwritten = true } }, definition, fixture()))
  assert.equal(overwritten, false)
})
test('missing parents, self/long cycles, duplicate ids, invalid order and future versions stay read-only', () => {
  for (const zones of [[area('A', 'missing')], [area('A', 'A')], [area('A', 'B'), area('B', 'C'), area('C', 'A')], [area('A'), area('A')], [area('A', undefined, -1)], [area('A', undefined, 1.5)]]) {
    const raw = JSON.stringify({ ...fixture(), zones })
    assert.throws(() => parseEditor(raw, definition))
    assert.equal(loadEditor({ getItem: () => raw }, definition).writable, false)
  }
  assert.throws(() => parseEditor(JSON.stringify({ ...fixture(), hierarchyVersion: 2 }), definition))
})
test('reparenting rejects descendants and preserves absolute pose, identity and hotspot links', () => {
  const data = fixture()
  assert.equal(canParent(data.zones, 'A', 'A11'), false)
  assert.throws(() => moveArea(data, 'A', 'A11'))
  const next = moveArea(data, 'A1', 'B')
  validateHierarchy(next.zones)
  assert.deepEqual(next.zones.find(zone => zone.id === 'A1')?.camera, data.zones[1].camera)
  assert.deepEqual(next.hotspots, data.hotspots)
  assert.deepEqual(zonePath(next.zones, 'A11').map(zone => zone.id), ['B', 'A1', 'A11'])
  assert.equal(moveArea(next, 'A1').zones.find(zone => zone.id === 'A1')?.parentZoneId, undefined)
})
test('safe deletion lifts direct children and points, preserves descendants and unassigned content', () => {
  const data = fixture(), next = removeArea(data, 'A1')
  assert.equal(next.zones.find(zone => zone.id === 'A11')?.parentZoneId, 'A')
  assert.equal(next.hotspots[0].zoneId, 'A')
  assert.equal(next.hotspots[0].id, 'p1')
  assert.deepEqual(next.hotspots[1], data.hotspots[1])
  const rootRemoved = removeArea(data, 'A')
  assert.equal(rootRemoved.zones.find(zone => zone.id === 'A1')?.parentZoneId, undefined)
  assert.equal(rootRemoved.zones.find(zone => zone.id === 'A11')?.parentZoneId, 'A1')
  assert.equal(removeArea(next, 'A').hotspots[0].zoneId, undefined)
  validateHierarchy(rootRemoved.zones)
})
test('sibling ordering survives JSON round trip and search reveals matching ancestors', () => {
  const next = reorderArea(fixture(), 'A2', -1)
  const parsed = parseEditor(JSON.stringify(next), definition)
  assert.deepEqual(childrenOf(parsed.zones, 'A').map(zone => zone.id), ['A2', 'A1'])
  assert.deepEqual([...searchAreaIds(parsed, 'Brightness')], ['A', 'A1'])
  assert.deepEqual(childrenOf(reorderArea(next, 'A2', -1).zones, 'A').map(zone => zone.id), ['A2', 'A1'])
})
test('hierarchical cockpit and hospital persistence remains strictly isolated', () => {
  const second = { ...definition, id: 'hospital', modelUrl: '/hospital.glb' }
  const map = new Map<string, string>(), storage = { getItem: (key: string) => map.get(key) ?? null, setItem: (key: string, value: string) => { map.set(key, value) } }
  assert.equal(saveEditor(storage, definition, fixture()), '')
  const first = map.get(editorKey(definition.id))
  const hospital = { ...fixture(), environmentId: second.id, modelUrl: second.modelUrl, zones: fixture().zones.map(zone => ({ ...zone, environmentId: second.id })) }
  assert.equal(saveEditor(storage, second, hospital), '')
  assert.equal(map.get(editorKey(definition.id)), first)
  assert.deepEqual(loadEditor(storage, second).data, JSON.parse(JSON.stringify(hospital)))
})
test('transition timing grows with distance and angle, stays bounded and honours reduced motion', () => {
  assert.ok(transitionDuration(650, 0, .1, 0, false) < transitionDuration(650, 2, 0, 0, false))
  assert.ok(transitionDuration(650, 0, .1, 0, false) < transitionDuration(650, 0, 1, 0, false))
  assert.equal(transitionDuration(650, 100, Math.PI, 60, false), 650)
  assert.equal(transitionDuration(650, 100, Math.PI, 60, true), 1)
})

test('reassociated child hotspot uses its new absolute camera and waits for actual completion', async () => {
  const data = fixture()
  const hotspots = data.hotspots.map(spot => spot.id === 'p1' ? { ...spot, zoneId: 'A2' } : spot)
  let resolve!: (result: 'completed' | 'cancelled') => void
  const completion = new Promise<'completed' | 'cancelled'>(done => { resolve = done })
  const opened: string[] = []
  let destination = ''
  const pending = navigateAndOpenHotspot('p1', {
    hotspots, zones: data.zones, environmentId: definition.id, isCurrent: () => true,
    navigate: async (pose, id) => { destination = id; assert.deepEqual(pose, data.zones.find(zone => zone.id === 'A2')!.camera); return completion },
    open: spot => opened.push(spot.id), message() {},
  })
  assert.equal(destination, 'A2')
  assert.deepEqual(opened, [])
  resolve('completed'); await pending
  assert.deepEqual(opened, ['p1'])
})
