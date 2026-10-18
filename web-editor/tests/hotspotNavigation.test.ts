import { test } from 'node:test'
import assert from 'node:assert/strict'
import { navigateAndOpenHotspot } from '../src/features/environments/hotspotNavigation.ts'
import type { Hotspot, InspectionZone } from '../src/features/environments/types.ts'
import type { CameraMoveResult } from '../src/features/environments/viewerInteraction.ts'

const zones: InspectionZone[] = [{ id: 'zone', title: 'Reviewed view', environmentId: 'env', calibration: 'reviewed', camera: { position: [0, 1, 0], target: [0, 1, 1], fovDegrees: 45 } }]
const hotspots: Hotspot[] = [
  { id: 'A', title: 'A', description: 'A information', position: [0, 0, 1], zoneId: 'zone' },
  { id: 'B', title: 'B', description: 'B information', position: [1, 0, 1], zoneId: 'zone' },
  { id: 'free', title: 'Free', description: '', position: [0, 0, 1] },
  { id: 'orphan', title: 'Orphan', description: '', position: [0, 0, 1], zoneId: 'missing' },
]
function deferred() {
  let resolve!: (result: CameraMoveResult) => void
  const promise = new Promise<CameraMoveResult>(complete => { resolve = complete })
  return { promise, resolve }
}
function fixture() {
  const opened: string[] = []
  const messages: string[] = []
  const moves: string[] = []
  const next = deferred()
  return { opened, messages, moves, next, options: {
    hotspots, zones, environmentId: 'env', isCurrent: () => true,
    navigate: async (_pose: unknown, zoneId: string) => { moves.push(zoneId); return next.promise },
    open: (spot: Hotspot) => { opened.push(spot.id) }, message: (message: string) => { messages.push(message) },
  } }
}

test('zone-linked selection waits for actual completion, then opens the requested hotspot', async () => {
  const f = fixture()
  const pending = navigateAndOpenHotspot('A', f.options)
  assert.deepEqual(f.moves, ['zone'])
  assert.deepEqual(f.opened, [])
  f.next.resolve('completed')
  await pending
  assert.deepEqual(f.opened, ['A'])
})
test('zone-free and missing-zone selections do not move the camera', async () => {
  const f = fixture()
  await navigateAndOpenHotspot('free', f.options)
  await navigateAndOpenHotspot('orphan', f.options)
  assert.deepEqual(f.moves, [])
  assert.deepEqual(f.opened, ['free', 'orphan'])
  assert.match(f.messages[0], /missing inspection zone/)
})
test('rapid A then B sharing a zone opens only B, even when A completes later', async () => {
  const f = fixture()
  let revision = 1
  const first = deferred(), second = deferred()
  const a = navigateAndOpenHotspot('A', { ...f.options, isCurrent: () => revision === 1, navigate: () => first.promise })
  revision = 2
  const b = navigateAndOpenHotspot('B', { ...f.options, isCurrent: () => revision === 2, navigate: () => second.promise })
  second.resolve('completed'); await b
  first.resolve('completed'); await a
  assert.deepEqual(f.opened, ['B'])
})
test('escape, mode change or unmount invalidates pending panel opening', async () => {
  const f = fixture()
  let current = true
  const pending = navigateAndOpenHotspot('A', { ...f.options, isCurrent: () => current })
  current = false
  f.next.resolve('completed'); await pending
  assert.deepEqual(f.opened, [])
})
test('cancelled movement never opens a panel; blocked movement opens with guidance', async () => {
  for (const result of ['cancelled', 'blocked'] as const) {
    const f = fixture()
    const pending = navigateAndOpenHotspot('A', f.options)
    f.next.resolve(result); await pending
    assert.deepEqual(f.opened, result === 'blocked' ? ['A'] : [])
    if (result === 'blocked') assert.match(f.messages[0], /could not be reached safely/)
  }
})
test('a completed/no-animation navigation opens only the explicitly selected item', async () => {
  const f = fixture()
  const options = { ...f.options, navigate: async () => 'completed' as const }
  await navigateAndOpenHotspot('A', options)
  await navigateAndOpenHotspot('B', options)
  await navigateAndOpenHotspot('unknown', options)
  assert.deepEqual(f.opened, ['A', 'B'])
})
