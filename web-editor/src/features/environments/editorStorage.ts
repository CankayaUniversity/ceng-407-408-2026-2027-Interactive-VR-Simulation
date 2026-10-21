import type { CameraPose, Hotspot, InspectionZone, SceneDefinition, SerializedEditorV1, Vector3Tuple } from './types'

export const editorKey = (environmentId: string) => `workplace-studio.environment-editor.v1.${encodeURIComponent(environmentId)}`
type RecordValue = Record<string, unknown>
const record = (value: unknown): value is RecordValue => typeof value === 'object' && value !== null && !Array.isArray(value)
const text = (value: unknown, max: number, required = true): value is string => typeof value === 'string' && value.length <= max && (!required || value.trim().length > 0)
const vector = (value: unknown): value is Vector3Tuple => Array.isArray(value) && value.length === 3 && value.every(n => typeof n === 'number' && Number.isFinite(n) && Math.abs(n) <= 100000)
export function isCameraPose(value: unknown): value is CameraPose {
  return record(value) && vector(value.position) && vector(value.target) &&
    value.position.some((n, i) => Math.abs(n - (value.target as number[])[i]) > 0.000001) &&
    typeof value.fovDegrees === 'number' && Number.isFinite(value.fovDegrees) && value.fovDegrees >= 10 && value.fovDegrees <= 120
}
const isZone = (value: unknown, id: string): value is InspectionZone => record(value) && text(value.id, 100) && text(value.title, 100) && value.environmentId === id && value.calibration === 'reviewed' && isCameraPose(value.camera)
const isHotspot = (value: unknown): value is Hotspot => record(value) && text(value.id, 100) && text(value.title, 100) && text(value.description, 2000, false) && vector(value.position) && (value.targetNode === undefined || text(value.targetNode, 500)) && (value.zoneId === undefined || text(value.zoneId, 100))

export function emptyEditor(definition: SceneDefinition): SerializedEditorV1 {
  return { schemaVersion: 1, environmentId: definition.id, modelUrl: definition.modelUrl, modelTransform: definition.modelTransform, zones: [], hotspots: [] }
}

/** Add only missing source IDs; keep user edits, ordering and zone links intact. */
export function addStarterContent(data: SerializedEditorV1, definition: SceneDefinition): SerializedEditorV1 {
  const starter = definition.starterContent
  if (!starter) return parseEditor(JSON.stringify(data), definition)
  return parseEditor(JSON.stringify({
    ...data,
    zones: [...data.zones, ...starter.zones.filter(zone => !data.zones.some(item => item.id === zone.id))],
    hotspots: [...data.hotspots, ...starter.hotspots.filter(spot => !data.hotspots.some(item => item.id === spot.id))],
  }), definition)
}

export function parseEditor(raw: string, definition: SceneDefinition): SerializedEditorV1 {
  const value: unknown = JSON.parse(raw)
  if (!record(value) || value.schemaVersion !== 1 || value.environmentId !== definition.id || value.modelUrl !== definition.modelUrl || !record(value.modelTransform) ||
    !(['position', 'rotation', 'scale'] as const).every(key => vector((value.modelTransform as RecordValue)[key]) && ((value.modelTransform as RecordValue)[key] as number[]).every((n, i) => n === definition.modelTransform[key][i])) ||
    !Array.isArray(value.zones) || value.zones.length > 32 || !value.zones.every(zone => isZone(zone, definition.id)) ||
    !Array.isArray(value.hotspots) || value.hotspots.length > 100 || !value.hotspots.every(isHotspot)) throw new Error('Invalid or incompatible editor data')
  const data = value as unknown as SerializedEditorV1
  if (new Set(data.zones.map(zone => zone.id)).size !== data.zones.length || new Set(data.hotspots.map(spot => spot.id)).size !== data.hotspots.length) throw new Error('Invalid editor identifiers')
  return data
}

export function loadEditor(storage: Pick<Storage, 'getItem'>, definition: SceneDefinition) {
  try {
    const raw = storage.getItem(editorKey(definition.id))
    const data = raw === null ? addStarterContent(emptyEditor(definition), definition) : parseEditor(raw, definition)
    const missingZone = data.hotspots.some(spot => spot.zoneId && !data.zones.some(zone => zone.id === spot.zoneId))
    return { data, writable: true, error: missingZone ? 'Some hotspots refer to missing inspection zones. Their information remains available without automatic camera navigation; saved references are preserved.' : '' }
  } catch {
    return { data: emptyEditor(definition), writable: false, error: 'Saved environment data could not be read. The original data is preserved; editing is disabled. Recover the stored data before retrying.' }
  }
}

export function saveEditor(storage: Pick<Storage, 'setItem'>, definition: SceneDefinition, data: SerializedEditorV1): string {
  try {
    const serialized = JSON.stringify(data)
    parseEditor(serialized, definition)
    storage.setItem(editorKey(definition.id), serialized)
    return ''
  } catch {
    return 'Editor data could not be saved. Check storage permissions, space and data validity. The previous saved version has been preserved.'
  }
}
