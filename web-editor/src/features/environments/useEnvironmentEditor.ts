import { useEffect, useState, useRef, type RefObject } from 'react'
import { addStarterContent, emptyEditor, loadEditor, saveEditor } from './editorStorage'
import type { CameraPose, Hotspot, SceneDefinition, SerializedEditorV1, Vector3Tuple } from './types'
import type { CameraMoveResult, ViewerInteraction } from './viewerInteraction'
import { navigateAndOpenHotspot } from './hotspotNavigation'
import { childrenOf, moveArea, removeArea, reorderArea } from './zoneHierarchy'

export function useEnvironmentEditor(definition: SceneDefinition, viewer: RefObject<ViewerInteraction | null>) {
  const [initial] = useState(() => {
    try { return loadEditor(window.localStorage, definition) }
    catch { return { data: emptyEditor(definition), error: 'Browser storage is unavailable. Editing is disabled.', writable: false } }
  })
  const [data, setData] = useState(initial.data)
  const [error, setError] = useState(initial.error)
  const [mode, setMode] = useState<'editor' | 'preview'>('preview')
  const [placing, setPlacing] = useState(false)
  const [draft, setDraft] = useState<Hotspot | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [activeZone, setActiveZone] = useState<string | null>(null)
  const [selectedZoneId, setSelectedZoneId] = useState<string | null>(null)
  const [pendingZone, setPendingZone] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const history = useRef<{ pose: CameraPose; zone: string | null }[]>([])
  const [hasPrevious, setHasPrevious] = useState(false)
  const navigationRevision = useRef(0)
  const pendingSelection = useRef(false)
  const pendingMove = useRef(false)
  const motionOrigin = useRef<{ pose: CameraPose; zone: string | null } | null>(null)
  useEffect(() => () => { navigationRevision.current++; viewer.current?.cancelMove() }, [viewer])
  const selected = data.hotspots.find(spot => spot.id === selectedId) ?? null
  const editable = mode === 'editor' && initial.writable
  const starter = definition.starterContent
  const canAddStarterContent = !!starter && (starter.hotspots.some(spot => !data.hotspots.some(item => item.id === spot.id)) || starter.zones.some(zone => !data.zones.some(item => item.id === zone.id)))
  const focusTools = () => requestAnimationFrame(() => document.getElementById('environment-mode-preview')?.focus())
  function cancelNavigation(preserveOrigin = false) {
    navigationRevision.current++
    pendingSelection.current = false
    pendingMove.current = false
    setPendingZone(null)
    viewer.current?.cancelMove()
    if (!preserveOrigin) motionOrigin.current = null
    return navigationRevision.current
  }
  function closePanel() { cancelNavigation(); setDraft(null); setSelectedId(null); setPlacing(false); focusTools() }
  function commit(next: SerializedEditorV1) {
    if (!editable) return false
    let failure: string
    try { failure = saveEditor(window.localStorage, definition, { ...next, hierarchyVersion: 1 }) }
    catch { failure = 'Browser storage is unavailable. Your previous data is preserved.' }
    setError(failure)
    if (failure) return false
    setData({ ...next, hierarchyVersion: 1 })
    return true
  }
  function changeMode(next: 'editor' | 'preview') {
    cancelNavigation()
    setMode(next); setPlacing(false); setDraft(null); setSelectedId(null); setMessage('')
  }
  async function select(id: string) {
    if (document.pointerLockElement) document.exitPointerLock()
    const request = cancelNavigation(true)
    pendingSelection.current = true
    setPlacing(false); viewer.current?.setPlacing(false)
    setDraft(null); setSelectedId(null); setMessage('')
    const associated = data.hotspots.find(spot => spot.id === id)?.zoneId
    setSelectedZoneId(associated && data.zones.some(zone => zone.id === associated) ? associated : null)
    await navigateAndOpenHotspot(id, {
      hotspots: data.hotspots, zones: data.zones, environmentId: definition.id,
      isCurrent: () => navigationRevision.current === request,
      navigate: (pose, zoneId) => move(pose, zoneId, request),
      message: setMessage,
      open: hotspot => {
        pendingSelection.current = false
        motionOrigin.current = null
        setSelectedId(hotspot.id)
        setDraft(editable ? { ...hotspot } : null)
      },
    })
    if (navigationRevision.current === request) pendingSelection.current = false
  }
  function place(position: Vector3Tuple, targetNode: string) {
    if (!editable) return
    cancelNavigation()
    setPlacing(false); setSelectedId(null)
    setDraft({ id: crypto.randomUUID(), title: '', description: '', position, targetNode, ...(selectedZoneId ? { zoneId: selectedZoneId } : {}) })
    setMessage('Draft marker placed. Enter training content and Save to confirm.')
  }
  function saveHotspot() {
    if (!draft || !draft.title.trim() || !editable) return
    const spot = { ...draft, title: draft.title.trim(), description: draft.description.trim() }
    if (data.hotspots.length >= 100 && !data.hotspots.some(item => item.id === spot.id)) { setError('A maximum of 100 hotspots is supported.'); return }
    if (commit({ ...data, hotspots: [...data.hotspots.filter(item => item.id !== spot.id), spot] })) {
      setDraft(null); setSelectedId(null); setMessage('Hotspot saved.'); focusTools()
    }
  }
  function deleteHotspot() {
    if (!draft || !selected || !editable) return
    if (commit({ ...data, hotspots: data.hotspots.filter(item => item.id !== draft.id) })) { closePanel(); setMessage('Hotspot deleted.') }
  }
  async function move(pose: CameraPose, zone: string | null, request: number, remember = true): Promise<CameraMoveResult> {
    const controller = viewer.current
    if (!controller) { setMessage('The 3D viewer is not ready. Hotspot information is shown without camera navigation.'); return 'blocked' }
    setPlacing(false); controller.setPlacing(false); setDraft(null); setSelectedId(null)
    if (controller.isAtPose(pose)) { setActiveZone(zone); return 'completed' }
    const origin = motionOrigin.current ?? { pose: controller.capture(), zone: activeZone }
    motionOrigin.current = origin
    pendingMove.current = true
    setPendingZone(zone)
    const result = await controller.move(pose)
    if (navigationRevision.current !== request) return 'cancelled'
    setPendingZone(null)
    pendingMove.current = false
    motionOrigin.current = null
    if (result === 'completed') {
      if (remember) { history.current = [...history.current.slice(-15), origin]; setHasPrevious(true) }
      setActiveZone(zone)
    }
    return result
  }
  async function overview() { if (await move(definition.camera, null, cancelNavigation(true)) === 'completed') { setSelectedZoneId(null); setMessage('Overview restored.') } }
  async function previous() {
    const last = history.current.at(-1)
    if (last && await move(last.pose, last.zone, cancelNavigation(true), false) === 'completed') { setSelectedZoneId(last.zone); history.current.pop(); setHasPrevious(history.current.length > 0); setMessage('Previous view restored.') }
  }
  function escape() {
    if (draft || selected || placing) { closePanel(); return }
    if (pendingSelection.current || pendingMove.current) { cancelNavigation(); setMessage('Navigation cancelled.'); return }
    if (activeZone) overview()
  }
  function saveZone(title: string, reviewed: boolean, parentZoneId?: string) {
    const camera = viewer.current?.capture()
    if (!camera || !title.trim() || !reviewed || !editable) return false
    if (data.zones.length >= 32) { setError('A maximum of 32 zones is supported.'); return false }
    if (parentZoneId && !data.zones.some(zone => zone.id === parentZoneId)) return false
    const siblings = childrenOf(data.zones, parentZoneId)
    const zone = { id: crypto.randomUUID(), title: title.trim(), environmentId: definition.id, camera, calibration: 'reviewed' as const, ...(parentZoneId ? { parentZoneId } : {}), order: Math.max(-1, ...siblings.map((item, index) => item.order ?? index)) + 1 }
    if (!commit({ ...data, zones: [...data.zones, zone] })) return false
    setMessage('Current camera saved as an inspection zone. Review the entire transition before using it for training.')
    setSelectedZoneId(zone.id)
    return true
  }
  function deleteZone(id: string) {
    cancelNavigation()
    const parent = data.zones.find(zone => zone.id === id)?.parentZoneId ?? null
    if (commit(removeArea(data, id))) {
      if (activeZone === id) setActiveZone(null)
      if (selectedZoneId === id) setSelectedZoneId(parent)
      history.current = history.current.filter(item => item.zone !== id); setHasPrevious(history.current.length > 0)
      setMessage('Area removed. All sub-areas and information points were kept and moved one level up.')
    }
  }
  return {
    data, error, mode, placing, draft, selected, activeZone, selectedZoneId, pendingZone, message, editable,
    canAddStarterContent,
    sourceAttribution: starter ? definition.attribution : undefined,
    addSourceHotspots() {
      if (!editable || !canAddStarterContent) return
      try {
        const next = addStarterContent(data, definition)
        if (commit(next)) { closePanel(); setMessage('Source annotations added. Your existing content has been preserved.') }
      } catch { setError('Source annotations could not be added. Check the 32-view / 100-hotspot limits. Your existing content has been preserved.') }
    },
    hasPrevious,
    setDraft, setMessage, changeMode, select, place, closePanel, saveHotspot, deleteHotspot, cancelNavigation,
    overview, previous, escape, saveZone, deleteZone,
    inspectZone(id: string | null) { cancelNavigation(); setSelectedZoneId(id); setDraft(null); setSelectedId(null); setPlacing(false) },
    updateZone(id: string, title: string, parentZoneId?: string) {
      if (!title.trim()) return false
      try {
        let next = data
        const zone = data.zones.find(item => item.id === id)
        if (!zone) return false
        if (zone.parentZoneId !== parentZoneId) next = moveArea(data, id, parentZoneId)
        const saved = commit({ ...next, zones: next.zones.map(item => item.id === id ? { ...item, title: title.trim() } : item) })
        if (saved) setMessage('Area details saved.')
        return saved
      } catch { setError('Choose a parent outside this area and its sub-areas.'); return false }
    },
    reorderZone(id: string, delta: -1 | 1) { if (commit(reorderArea(data, id, delta))) setMessage('Area order saved.') },
    updateZoneView(id: string) {
      const camera = viewer.current?.capture()
      if (camera && commit({ ...data, zones: data.zones.map(zone => zone.id === id ? { ...zone, camera } : zone) })) setMessage('Saved view updated from the current camera.')
    },
    async parent() {
      const parentId = data.zones.find(item => item.id === activeZone)?.parentZoneId
      const parent = data.zones.find(item => item.id === parentId)
      if (parent) { setSelectedZoneId(parent.id); await move(parent.camera, parent.id, cancelNavigation(true)) }
      else await overview()
    },
    startPlacement() {
      if (!editable) return
      cancelNavigation()
      if (document.pointerLockElement) document.exitPointerLock()
      setDraft(null); setSelectedId(null); setPlacing(true); setMessage('Click model geometry to place a draft marker. Escape cancels.')
    },
    async goToZone(id: string) {
      const zone = data.zones.find(item => item.id === id)
      if (!zone) return
      setSelectedZoneId(id)
      const result = await move(zone.camera, id, cancelNavigation(true))
      if (result === 'completed') setMessage(`Current area: ${zone.title}`)
      else if (result === 'blocked') setMessage('This path is obstructed. The current area has not changed. Choose another view or use Spectator in Editor.')
    },
    async reset() { if (await move(definition.camera, null, cancelNavigation(true)) === 'completed') { setSelectedZoneId(null); setMessage('View reset.'); history.current = []; setHasPrevious(false) } },
  }
}

export type EnvironmentEditorState = ReturnType<typeof useEnvironmentEditor>
