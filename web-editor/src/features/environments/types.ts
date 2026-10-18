export type NavigationMode = 'seated' | 'walking'
export type Vector3Tuple = readonly [number, number, number]

export interface ModelTransform {
  position: Vector3Tuple
  /** Euler XYZ, radyan. Kaynak GLB'nin kendi düğüm dönüşümleri korunur. */
  rotation: Vector3Tuple
  scale: Vector3Tuple
}

export interface CameraConfiguration {
  position: Vector3Tuple
  target: Vector3Tuple
  fovDegrees: number
  near: number
  far: number
  calibration: 'provisional' | 'reviewed'
}

export interface SceneDefinition {
  id: string
  displayName: string
  modelUrl: string
  modelTransform: ModelTransform
  navigationMode: NavigationMode
  camera: CameraConfiguration
  interaction?: InteractionConfiguration
  attribution?: { author: string; sourceUrl: string; license: string; licenseUrl: string }
}

export interface CameraPose {
  /** Sağ el sahne koordinatları; modelTransform uygulandıktan sonra. */
  position: Vector3Tuple
  target: Vector3Tuple
  fovDegrees: number
}

export interface InteractionConfiguration {
  /** Simetrik dikey bakış sınırı; varsayılan 85°, en fazla 89°. Kayıt şeması değildir. */
  look?: { maxPitchDegrees: number }
  zoom: { minDegrees: number; maxDegrees: number; sensitivity: number; durationMs: number }
  transition: { durationMs: number; clearance: number }
  /** Oturumluk editör serbest kamera aracı; navigationMode veya kayıt şeması değildir. */
  inspector?: InspectorConfiguration
  /** Düz referans zeminli prototip; fiziksel duvar çarpışması garantisi yoktur. */
  walking?: InspectorConfiguration & { groundHeight: number; eyeHeight: number }
}

export interface InspectorConfiguration {
  speed: number
  boostMultiplier: number
  maxDeltaMs: number
  bounds?: { min: Vector3Tuple; max: Vector3Tuple }
}

export interface InspectionZone {
  id: string
  title: string
  environmentId: string
  camera: CameraPose
  /** Editörün görüntüyü görsel olarak inceleyip onayladığını belirtir; uçuş doğruluğu değildir. */
  calibration: 'reviewed'
}

export interface Hotspot {
  id: string
  title: string
  description: string
  /** GLB köküne göre yerel sağ el XYZ koordinatı; modelTransform uygulanmadan önce. */
  position: Vector3Tuple
  /** GLB düğüm adı; tekrarlı adlar içeren modellerde sonraki fazda kararlı kimlik gerekir. */
  targetNode?: string
  zoneId?: string
}

export interface SerializedEditorV1 {
  schemaVersion: 1
  environmentId: string
  modelUrl: string
  modelTransform: ModelTransform
  zones: InspectionZone[]
  hotspots: Hotspot[]
}

/** Gelecek kayıtlar için veri sözleşmesi. Bu fazda depolamaya yazılmaz. */
export interface SerializedSceneV1 {
  schemaVersion: 1
  environmentId: string
  modelTransform: ModelTransform
  navigationMode: NavigationMode
  camera: CameraConfiguration
  hotspots: Hotspot[]
}
