import type { CameraPose, Hotspot, InspectionZone } from './types'
import type { CameraMoveResult } from './viewerInteraction'

/** Yalnız kullanıcı seçimiyle çağrılır; kayıt yükleme/draft değişimi kamerayı hareket ettirmez. */
export async function navigateAndOpenHotspot(id: string, options: {
  hotspots: readonly Hotspot[]
  zones: readonly InspectionZone[]
  environmentId: string
  isCurrent: () => boolean
  navigate: (pose: CameraPose, zoneId: string) => Promise<CameraMoveResult>
  open: (hotspot: Hotspot) => void
  message: (message: string) => void
}) {
  const hotspot = options.hotspots.find(spot => spot.id === id)
  if (!hotspot || !options.isCurrent()) return
  const zone = hotspot.zoneId ? options.zones.find(item => item.id === hotspot.zoneId && item.environmentId === options.environmentId) : undefined
  if (hotspot.zoneId && !zone) {
    options.message('This hotspot refers to a missing inspection zone. Its information is shown without moving the camera; the saved reference has been preserved.')
  }
  if (zone) {
    const result = await options.navigate(zone.camera, zone.id)
    if (!options.isCurrent() || result === 'cancelled') return
    if (result === 'blocked') options.message('The inspection view could not be reached safely. Hotspot information is shown at the current view.')
  }
  if (options.isCurrent()) options.open(hotspot)
}
