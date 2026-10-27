import { test } from 'node:test'
import assert from 'node:assert/strict'
import { addStarterContent, emptyEditor, loadEditor, parseEditor, saveEditor } from '../src/features/environments/editorStorage.ts'
import { hospitalStarterContent } from '../src/features/environments/hospitalAnnotations.ts'
import type { SceneDefinition } from '../src/features/environments/types.ts'

const definition: SceneDefinition = {
  id: 'charite-operating-room', displayName: 'Hospital', modelUrl: '/hospital.glb',
  modelTransform: { position: [0, 0, 0], rotation: [0, 0, 0], scale: [1, 1, 1] },
  navigationMode: 'walking',
  camera: { position: [0, 1.65, 4.5], target: [0, 1.65, 0], fovDegrees: 70, near: .03, far: 100, calibration: 'provisional' },
  starterContent: hospitalStarterContent,
}

test('a new hospital record gets ten original annotations with valid Y-up positions and linked views', () => {
  const loaded = loadEditor({ getItem: () => null }, definition)
  assert.equal(loaded.writable, true)
  assert.equal(loaded.error, '')
  assert.equal(loaded.data.hotspots.length, 10)
  assert.equal(loaded.data.zones.length, 10)
  assert.deepEqual(parseEditor(JSON.stringify(loaded.data), definition), loaded.data)
  assert.deepEqual(loaded.data.hotspots[0].position, [.005148481225094504, 1.221689886149534, .06651238007191948])
  assert.equal(loaded.data.hotspots[9].title, 'Cauter & Suction tower')
  for (const spot of loaded.data.hotspots) {
    assert.ok(loaded.data.zones.some(zone => zone.id === spot.zoneId))
    assert.ok(spot.position[1] > 0 && spot.position[1] < 4)
  }
})

test('saved edits, deliberate deletions and unreadable records are never replaced by starter content', () => {
  const empty = emptyEditor(definition)
  const edited = addStarterContent(empty, definition)
  edited.hotspots[0].title = 'User correction'
  edited.hotspots.pop()
  for (const saved of [empty, edited]) {
    assert.deepEqual(loadEditor({ getItem: () => JSON.stringify(saved) }, definition).data, saved)
  }
  const broken = loadEditor({ getItem: () => '{broken' }, definition)
  assert.equal(broken.writable, false)
  assert.deepEqual(broken.data, empty)
  const fresh = loadEditor({ getItem: () => null }, definition)
  assert.equal(fresh.data.hotspots[0].title, 'Patient bed socket', 'starter objects are not shared with editable records')
})

test('explicit source import is additive and idempotent, preserving edited source IDs and custom content', () => {
  const source = addStarterContent(emptyEditor(definition), definition)
  const edited = {
    ...source,
    hotspots: [
      { ...source.hotspots[0], title: 'User title', description: 'User note', zoneId: undefined },
      { id: 'custom', title: 'Custom equipment', description: 'Custom note', position: [1, 1, 1] as const },
    ],
    zones: [{ ...source.zones[0], title: 'User view' }],
  }
  const before = JSON.stringify(edited)
  const merged = addStarterContent(edited, definition)
  assert.equal(merged.hotspots.length, 11)
  assert.equal(merged.zones.length, 10)
  assert.equal(merged.hotspots[0].title, 'User title')
  assert.equal(merged.hotspots[0].zoneId, undefined)
  assert.equal(merged.hotspots[1].id, 'custom')
  assert.equal(merged.zones[0].title, 'User view')
  assert.deepEqual(addStarterContent(merged, definition), merged)
  assert.equal(JSON.stringify(edited), before)
  let raw = ''
  assert.equal(saveEditor({ setItem: (_key, value) => { raw = value } }, definition, merged), '')
  assert.deepEqual(loadEditor({ getItem: () => raw }, definition).data, merged)
})

test('source import enforces existing limits and leaves unrelated environments empty', () => {
  const full = { ...emptyEditor(definition), hotspots: Array.from({ length: 100 }, (_, i) => ({
    id: 'custom-' + i, title: 'Custom ' + i, description: '', position: [0, 1, 0] as const,
  })) }
  const before = JSON.stringify(full)
  assert.throws(() => addStarterContent(full, definition))
  assert.equal(JSON.stringify(full), before)
  const { starterContent: _starter, ...other } = definition
  const cockpit = { ...other, id: 'cockpit', modelUrl: '/cockpit.glb' }
  assert.deepEqual(loadEditor({ getItem: () => null }, cockpit).data, emptyEditor(cockpit))
})

test('source import preserves hierarchy, camera edits and exact legacy backup on explicit save', () => {
  const source = addStarterContent(emptyEditor(definition), definition)
  const parent = { ...source.zones[0], id: 'custom-parent', title: 'Custom parent', order: 0 }
  const child = { ...source.zones[1], parentZoneId: parent.id, order: 3 }
  const legacy = JSON.stringify({ ...source, hotspots: source.hotspots.slice(0, 2) })
  const data = { ...source, hierarchyVersion: 1 as const, zones: [parent, child], hotspots: source.hotspots.slice(0, 2) }
  const merged = addStarterContent(data, definition)
  assert.deepEqual(merged.zones[1], child)
  assert.equal(merged.hierarchyVersion, 1)
  assert.deepEqual(addStarterContent(merged, definition), merged)
  const records = new Map<string, string>([['workplace-studio.environment-editor.v1.charite-operating-room', legacy]])
  const storage = { getItem: (key: string) => records.get(key) ?? null, setItem: (key: string, value: string) => { records.set(key, value) } }
  assert.equal(saveEditor(storage, definition, merged), '')
  assert.equal(records.get('workplace-studio.environment-editor.v1.charite-operating-room.before-hierarchy.v1'), legacy)
  assert.deepEqual(loadEditor(storage, definition).data, merged)
})
