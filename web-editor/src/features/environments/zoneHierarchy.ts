import type { InspectionZone, SerializedEditorV1 } from './types'

export function zonePath(zones: readonly InspectionZone[], id: string | null): InspectionZone[] {
  const path: InspectionZone[] = [], seen = new Set<string>()
  let current = zones.find(zone => zone.id === id)
  while (current && !seen.has(current.id)) {
    seen.add(current.id); path.unshift(current)
    current = zones.find(zone => zone.id === current!.parentZoneId)
  }
  return path
}
export function childrenOf(zones: readonly InspectionZone[], parent?: string) {
  return zones.filter(zone => zone.parentZoneId === parent).map((zone, index) => ({ zone, index }))
    .sort((a, b) => (a.zone.order ?? a.index) - (b.zone.order ?? b.index) || a.index - b.index).map(item => item.zone)
}
export function validateHierarchy(zones: readonly InspectionZone[]) {
  for (const zone of zones) {
    if (zone.order !== undefined && (!Number.isSafeInteger(zone.order) || zone.order < 0)) throw new Error('Invalid area order')
    if (zone.parentZoneId !== undefined && (typeof zone.parentZoneId !== 'string' || !zones.some(parent => parent.id === zone.parentZoneId))) throw new Error('Missing parent area')
    const visited = new Set([zone.id])
    let parent = zones.find(item => item.id === zone.parentZoneId)
    while (parent) {
      if (visited.has(parent.id)) throw new Error('Area hierarchy contains a cycle')
      visited.add(parent.id); parent = zones.find(item => item.id === parent!.parentZoneId)
    }
  }
}
export function canParent(zones: readonly InspectionZone[], id: string, parent?: string) {
  return !parent || (zones.some(zone => zone.id === parent) && !zonePath(zones, parent).some(zone => zone.id === id))
}
export function moveArea(data: SerializedEditorV1, id: string, parentZoneId?: string): SerializedEditorV1 {
  if (!data.zones.some(zone => zone.id === id) || !canParent(data.zones, id, parentZoneId)) throw new Error('Choose a parent outside this area and its sub-areas')
  const siblings = childrenOf(data.zones.filter(zone => zone.id !== id), parentZoneId)
  return { ...data, zones: data.zones.map(zone => {
    if (zone.id !== id) return zone
    const { parentZoneId: _old, ...rest } = zone
    return { ...rest, ...(parentZoneId ? { parentZoneId } : {}), order: Math.max(-1, ...siblings.map((item, index) => item.order ?? index)) + 1 }
  }) }
}
export function reorderArea(data: SerializedEditorV1, id: string, delta: -1 | 1): SerializedEditorV1 {
  const zone = data.zones.find(item => item.id === id)
  if (!zone) return data
  const siblings = childrenOf(data.zones, zone.parentZoneId), index = siblings.findIndex(item => item.id === id)
  const target = index + delta
  if (target < 0 || target >= siblings.length) return data
  ;[siblings[index], siblings[target]] = [siblings[target], siblings[index]]
  return { ...data, zones: data.zones.map(item => {
    const order = siblings.findIndex(sibling => sibling.id === item.id)
    return order < 0 ? item : { ...item, order }
  }) }
}
/** Yalnız alanı kaldır; doğrudan alt alanlar ve bilgi noktaları üst alana taşınır. */
export function removeArea(data: SerializedEditorV1, id: string): SerializedEditorV1 {
  const removed = data.zones.find(zone => zone.id === id)
  if (!removed) return data
  let next = data
  for (const child of childrenOf(data.zones, id)) next = moveArea(next, child.id, removed.parentZoneId)
  return { ...next, zones: next.zones.filter(zone => zone.id !== id), hotspots: next.hotspots.map(spot => {
    if (spot.zoneId !== id) return spot
    const { zoneId: _old, ...rest } = spot
    return { ...rest, ...(removed.parentZoneId ? { zoneId: removed.parentZoneId } : {}) }
  }) }
}
export function searchAreaIds(data: SerializedEditorV1, query: string) {
  const match = (text: string) => text.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase())
  const ids = new Set<string>()
  for (const zone of data.zones) if (match(zone.title)) zonePath(data.zones, zone.id).forEach(item => ids.add(item.id))
  for (const spot of data.hotspots) if (match(spot.title)) zonePath(data.zones, spot.zoneId ?? null).forEach(item => ids.add(item.id))
  return ids
}
