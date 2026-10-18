import { useEffect, useLayoutEffect, useRef, useState, type RefObject } from 'react'
import { Engine } from '@babylonjs/core/Engines/engine'
import { Scene } from '@babylonjs/core/scene'
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera'
import { HemisphericLight } from '@babylonjs/core/Lights/hemisphericLight'
import { DirectionalLight } from '@babylonjs/core/Lights/directionalLight'
import { Vector3, Quaternion } from '@babylonjs/core/Maths/math.vector'
import { Color3, Color4 } from '@babylonjs/core/Maths/math.color'
import { TransformNode } from '@babylonjs/core/Meshes/transformNode'
import { LoadAssetContainerAsync } from '@babylonjs/core/Loading/sceneLoader'
import '@babylonjs/loaders/glTF'
import type { CameraConfiguration, SceneDefinition } from './types'
import { createViewerInteraction, defaultInteraction, type ViewerInteraction } from './viewerInteraction'
import { useEnvironmentEditor } from './useEnvironmentEditor'
import { HotspotPanel, ZoneCalibration } from './EnvironmentEditorPanels'
import { isCameraPose } from './editorStorage'
import { pitchLimit, poseOrientation, setOrientation } from './cameraOrientation'
import { defaultInspector } from './inspectorNavigation'
import { EnvironmentInfo, SceneExplorer, ZoneInspector } from './WorkspacePanels'

interface ViewportProps {
  definition: SceneDefinition
  headingRef: RefObject<HTMLHeadingElement | null>
  onBack: () => void
}

type LoadState = { phase: 'loading' | 'ready' | 'error'; message: string; progress?: number }

export function BabylonViewport({ definition, headingRef, onBack }: ViewportProps) {
  const workspaceRef = useRef<HTMLElement>(null)
  const [explorerOpen, setExplorerOpen] = useState(() => !window.matchMedia('(max-width: 1000px)').matches)
  const [inspectorOpen, setInspectorOpen] = useState(() => !window.matchMedia('(max-width: 1000px)').matches)
  const [inspectorTab, setInspectorTab] = useState<'environment' | 'zone' | 'save'>('environment')
  const [zoneId, setZoneId] = useState<string | null>(null)
  const [fullscreen, setFullscreen] = useState(false)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const cameraRef = useRef<FreeCamera | null>(null)
  const viewer = useRef<ViewerInteraction | null>(null)
  const markerElements = useRef(new Map<string, HTMLElement>())
  const editor = useEnvironmentEditor(definition, viewer)
  const editorRef = useRef(editor)
  useLayoutEffect(() => { editorRef.current = editor }, [editor])
  const [load, setLoad] = useState<LoadState>({ phase: 'loading', message: 'Preparing 3D viewer…' })
  const [attempt, setAttempt] = useState(0)
  const [locked, setLocked] = useState(false)
  const [interactionError, setInteractionError] = useState('')
  const [calibration, setCalibration] = useState<CameraConfiguration>(definition.camera)
  const [transitioning, setTransitioning] = useState(false)
  const [inspector, setInspector] = useState(false)
  const [walking, setWalking] = useState(false)
  const zoomConfiguration = (definition.interaction ?? defaultInteraction).zoom
  const spectatorBoost = Math.max(1, Math.min(3, definition.interaction?.inspector?.boostMultiplier ?? defaultInspector.boostMultiplier))

  useEffect(() => { viewer.current?.setPlacing(editor.placing) }, [editor.placing, load.phase])
  useEffect(() => { if (editor.mode !== 'editor' || load.phase !== 'ready') viewer.current?.setInspector(false) }, [editor.mode, load.phase])
  useEffect(() => { if (editor.mode !== 'preview' || load.phase !== 'ready') viewer.current?.setWalking(false) }, [editor.mode, load.phase])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    let disposed = false
    let engine: Engine | undefined
    let scene: Scene | undefined
    let observer: ResizeObserver | undefined
    let render: (() => void) | undefined
    const releasePointer = () => {
      if (document.pointerLockElement === canvas) document.exitPointerLock()
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      // Spectator exits first; subsequent Esc keeps the existing panel → overview order.
      const wasSpectator = viewer.current?.isInspectorEnabled()
      const wasWalking = viewer.current?.isWalkingEnabled()
      releasePointer()
      if (wasSpectator) {
        viewer.current?.setInspector(false)
        return
      }
      if (wasWalking) { viewer.current?.setWalking(false); return }
      editorRef.current.escape()
    }
    const onLock = () => { if (!disposed) setLocked(document.pointerLockElement === canvas) }
    document.addEventListener('pointerlockchange', onLock)
    document.addEventListener('keydown', onKey)
    window.addEventListener('blur', releasePointer)

    // StrictMode temizliğinin ilk geçişte ikinci engine/indirme oluşturmasını önler.
    void Promise.resolve().then(async () => {
      if (disposed) return
      setLoad({ phase: 'loading', message: 'Downloading environment…' })
      setCalibration(definition.camera)
      try {
        if (definition.navigationMode === 'walking' && !definition.interaction?.walking) throw new Error('Walking requires explicit ground, eye height and movement configuration.')
        engine = new Engine(canvas, true, { preserveDrawingBuffer: false, stencil: true })
        scene = new Scene(engine)
        scene.useRightHandedSystem = true
        scene.clearColor = Color4.FromHexString('#111216ff')
        const camera = new FreeCamera('environment-camera', Vector3.FromArray(definition.camera.position), scene)
        cameraRef.current = camera
        const limit = pitchLimit(definition.interaction?.look?.maxPitchDegrees)
        setOrientation(camera, poseOrientation(camera, definition.camera, limit), limit)
        camera.fov = definition.camera.fovDegrees * Math.PI / 180
        camera.minZ = definition.camera.near
        camera.maxZ = definition.camera.far
        camera.inertia = 0.65
        camera.inputs.clear()
        camera.inputs.addMouse()
        camera.angularSensibility = 2200
        camera.attachControl(canvas, true)
        // Yalnızca fare girdisi: klavye/gamepad/dokunmatik öteleme yok.
        scene.activeCamera = camera
        const sky = new HemisphericLight('ambient-light', new Vector3(0, 1, 0), scene)
        sky.intensity = 1.1
        sky.groundColor = new Color3(0.35, 0.35, 0.4)
        const sun = new DirectionalLight('key-light', new Vector3(-0.4, -1, -0.5), scene)
        sun.intensity = 1.5
        const modelRoot = new TransformNode('environment-root', scene)
        modelRoot.position = Vector3.FromArray(definition.modelTransform.position)
        modelRoot.rotationQuaternion = Quaternion.FromEulerVector(Vector3.FromArray(definition.modelTransform.rotation))
        modelRoot.scaling = Vector3.FromArray(definition.modelTransform.scale)
        observer = new ResizeObserver(() => engine?.resize())
        observer.observe(canvas)
        const container = await LoadAssetContainerAsync(definition.modelUrl, scene, {
          onProgress: (event) => {
            if (disposed) return
            const progress = event.lengthComputable && event.total > 0 ? event.loaded / event.total : undefined
            setLoad({ phase: 'loading', progress, message: progress === 1 ? 'Preparing geometry and materials…' : `Downloading environment… ${(event.loaded / 1024 ** 2).toFixed(1)} MiB` })
          },
        })
        if (disposed) { container.dispose(); return }
        container.addAllToScene()
        for (const node of container.rootNodes) node.parent = modelRoot
        await scene.whenReadyAsync()
        if (disposed) return
        viewer.current = createViewerInteraction(scene, camera, modelRoot, canvas, definition.interaction ?? defaultInteraction, {
          place: (position, targetNode) => editorRef.current.place(position, targetNode),
          message: message => editorRef.current.setMessage(message),
          transitioning: active => { if (!disposed) setTransitioning(active) },
          inspectorChanged: enabled => { if (!disposed) setInspector(enabled) },
          walkingChanged: enabled => { if (!disposed) setWalking(enabled) },
          markers: () => {
            const state = editorRef.current
            const spots = [...state.data.hotspots.filter(spot => spot.id !== state.draft?.id), ...(state.draft ? [state.draft] : [])]
            return spots.flatMap(spot => { const element = markerElements.current.get(spot.id); return element ? [{ position: spot.position, element }] : [] })
          },
        })
        render = () => scene?.render()
        engine.runRenderLoop(render)
        setLoad({ phase: 'ready', message: 'Environment loaded' })
        if (import.meta.env.DEV) console.info('[Environment viewer] ready', { meshes: container.meshes.length, materials: container.materials.length, textures: container.textures.length })
      } catch (error) {
        if (disposed) return
        const message = error instanceof Error ? error.message : String(error)
        setLoad({ phase: 'error', message: `The environment could not load. Check WebGL support and the local model file. ${message}` })
        viewer.current?.dispose()
        viewer.current = null
        releasePointer()
        observer?.disconnect()
        scene?.dispose()
        engine?.dispose()
        cameraRef.current = null
      }
    })

    return () => {
      disposed = true
      releasePointer()
      document.removeEventListener('pointerlockchange', onLock)
      document.removeEventListener('keydown', onKey)
      window.removeEventListener('blur', releasePointer)
      observer?.disconnect()
      viewer.current?.dispose()
      viewer.current = null
      cameraRef.current?.detachControl()
      cameraRef.current = null
      if (render) engine?.stopRenderLoop(render)
      // Scene.dispose aktif yükleme isteklerini de abort eder.
      scene?.dispose()
      engine?.dispose()
      if (import.meta.env.DEV) console.info('[Environment viewer] disposed')
    }
  }, [definition, attempt])

  function applyCamera(next: CameraConfiguration) {
    if (!isCameraPose(next)) { editor.setMessage('Camera position and target must be different, finite coordinates.'); return }
    const bounded = { ...next, fovDegrees: Math.max(zoomConfiguration.minDegrees, Math.min(zoomConfiguration.maxDegrees, next.fovDegrees)) }
    setCalibration(bounded)
    const camera = cameraRef.current
    if (!camera) return
    editor.cancelNavigation()
    viewer.current?.apply(bounded)
  }

  async function toggleLook() {
    if (document.pointerLockElement === canvasRef.current) { document.exitPointerLock(); return }
    try {
      await canvasRef.current?.requestPointerLock()
      canvasRef.current?.focus({ preventScroll: true })
      setInteractionError('')
    } catch {
      setInteractionError('Mouse capture is unavailable. Drag inside the viewport to look around instead.')
    }
  }


  useEffect(() => {
    const update = () => setFullscreen(document.fullscreenElement === workspaceRef.current)
    document.addEventListener('fullscreenchange', update)
    return () => { document.removeEventListener('fullscreenchange', update) }
  }, [])

  useEffect(() => {
    const narrow = window.matchMedia('(max-width: 1000px)')
    const update = () => { setExplorerOpen(!narrow.matches); setInspectorOpen(!narrow.matches) }
    narrow.addEventListener('change', update)
    return () => narrow.removeEventListener('change', update)
  }, [])

  function revealInspector(tab: 'environment' | 'zone' | 'save') {
    setInspectorTab(tab); setInspectorOpen(true)
    if (window.matchMedia('(max-width: 1000px)').matches) setExplorerOpen(false)
  }
  function selectHotspot(id: string) {
    setInspectorTab('environment')
    if (editor.mode === 'editor') revealInspector('environment')
    else if (window.matchMedia('(max-width: 1000px)').matches) setExplorerOpen(false)
    void editor.select(id)
  }
  const selectedZone = editor.data.zones.find(zone => zone.id === zoneId)
  const ready = load.phase === 'ready'
  const editing = editor.mode === 'editor'
  return <section ref={workspaceRef} className={`editor-v2 ${editing ? 'is-editing' : 'is-preview'}`} data-explorer={explorerOpen} data-inspector={inspectorOpen} aria-labelledby="environment-heading">
    <header className="v2-topbar">
      <button className="v2-brand" onClick={onBack} aria-label="Workplace Studio dashboard">
        <span aria-hidden="true">W</span>
      </button>
      <div className="v2-breadcrumb">
        <span>WORKPLACE STUDIO / ENVIRONMENTS</span>
        <h1 id="environment-heading" ref={headingRef} tabIndex={-1}>{definition.displayName}</h1>
      </div>
      <div className="mode-switch" role="group" aria-label="Interaction mode">
        <button id="environment-mode-preview" aria-pressed={!editing} onClick={() => { editor.changeMode('preview'); setInspectorTab('environment') }}>Preview</button>
        <button aria-pressed={editing} onClick={() => editor.changeMode('editor')}>Editor</button>
      </div>
      <div className="v2-top-actions">
        <button disabled={!ready} onClick={editor.reset}>Reset Camera</button>
        {document.fullscreenEnabled && <button aria-pressed={fullscreen} onClick={async () => {
          try { if (document.fullscreenElement === workspaceRef.current) await document.exitFullscreen(); else await workspaceRef.current?.requestFullscreen() }
          catch { setInteractionError('Fullscreen is unavailable in this browser.') }
        }}>{fullscreen ? 'Exit fullscreen' : 'Fullscreen'}</button>}
        <button onClick={onBack}>← Back to Dashboard</button>
      </div>
    </header>
    <div className="v2-panel-controls">
      <button id="v2-explorer-toggle" aria-expanded={explorerOpen} aria-controls="v2-explorer" onClick={() => { setExplorerOpen(value => !value); if (window.matchMedia('(max-width: 1000px)').matches) setInspectorOpen(false) }}>Scene Explorer</button>
      <span>{editing ? 'Editor workspace' : 'Read-only preview'}</span>
      <button id="v2-inspector-toggle" aria-expanded={inspectorOpen} aria-controls="v2-inspector" onClick={() => { setInspectorOpen(value => !value); if (window.matchMedia('(max-width: 1000px)').matches) setExplorerOpen(false) }}>Inspector</button>
    </div>
    <div className="v2-body">
      <nav className="v2-toolrail" aria-label="Workspace tools">
        <button title="Selection tool" aria-label="Selection tool" aria-pressed={!inspector && !walking && !editor.placing} onClick={() => { editor.closePanel(); revealInspector('environment') }}>↖</button>
        {editing && <>
          <button title="Spectator Mode" aria-label={inspector ? 'Exit Spectator' : 'Spectator Mode'} aria-pressed={inspector} disabled={!ready || transitioning || editor.placing} onClick={() => { const enabled = viewer.current?.isInspectorEnabled(); editor.cancelNavigation(); viewer.current?.setInspector(!enabled) }}>⌁</button>
          <button title="Add Hotspot" aria-label="Add hotspot" aria-pressed={editor.placing} disabled={!ready || transitioning || !editor.editable || editor.data.hotspots.length >= 100} onClick={() => { editor.startPlacement(); revealInspector('environment') }}>⊙</button>
          <button title="Save Current View" aria-label="Save Current View" disabled={!ready || transitioning || !editor.editable} aria-pressed={inspectorTab === 'save' && inspectorOpen} onClick={() => { editor.closePanel(); revealInspector('save') }}>▣</button>
        </>}
      </nav>
      <aside className="v2-explorer v2-panel" id="v2-explorer" aria-label="Scene Explorer">
        <div className="v2-panel-heading">
          <h2>Scene Explorer</h2>
          <button aria-label="Close Scene Explorer" onClick={() => { setExplorerOpen(false); document.getElementById('v2-explorer-toggle')?.focus() }}>×</button>
        </div>
        <div className="v2-panel-scroll">
          <SceneExplorer editor={editor} ready={ready} onZone={id => { setZoneId(id); revealInspector('zone'); void editor.goToZone(id) }} onHotspot={selectHotspot} />
        </div>
      </aside>
      <section className="v2-stage" aria-label="Environment viewport">
        <div className="v2-viewbar">
          <span>
            <i /> {editing ? 'EDITOR' : 'PREVIEW'} / {definition.navigationMode}</span>
          <div>{!editing && definition.navigationMode === 'walking' && <button aria-pressed={walking} disabled={!ready || transitioning} onClick={() => { const enabled = viewer.current?.isWalkingEnabled(); editor.cancelNavigation(); viewer.current?.setWalking(!enabled) }}>{walking ? 'Stop walking' : 'Start walking'}</button>}<button disabled={!ready} onClick={editor.overview}>Back to Overview</button>
            <button disabled={!ready || !editor.hasPrevious} onClick={editor.previous}>Back to Previous View</button>
            <button disabled={!ready || transitioning || editor.placing} onClick={toggleLook}>{locked ? 'Release cursor (Esc)' : 'Capture mouse'}</button>
          </div>
        </div>
        <div className="v2-canvas-wrap">
          <div className={`environment-viewport${editor.placing ? ' is-placing' : ''}`} aria-busy={load.phase === 'loading'}>
            <canvas ref={canvasRef} tabIndex={0} aria-label={`${definition.displayName} — drag to look around; wheel to zoom; Escape closes information or returns to overview`} />
            <div className="hotspot-markers" aria-label="3D hotspot markers">
              {load.phase === 'ready' && [...editor.data.hotspots.filter(spot => spot.id !== editor.draft?.id), ...(editor.draft ? [editor.draft] : [])].map((spot, index) => editor.draft?.id === spot.id && !editor.selected ?
                <span className="hotspot-marker is-draft" key={spot.id} ref={element => { if (element) markerElements.current.set(spot.id, element); else markerElements.current.delete(spot.id) }} aria-label="Draft hotspot location">＋</span> :
                <button type="button" key={spot.id} className={`hotspot-marker${editor.selected?.id === spot.id ? ' is-selected' : ''}`} ref={element => { if (element) markerElements.current.set(spot.id, element); else markerElements.current.delete(spot.id) }} aria-label={`Hotspot: ${spot.title}`} title={spot.title} onClick={() => selectHotspot(spot.id)}>{index + 1}</button>)}
            </div>
            {load.phase !== 'ready' && <div className="environment-overlay">
              <p role={load.phase === 'error' ? 'alert' : 'status'}>{load.message}</p>
              {load.phase === 'loading' && <progress aria-label="Model download progress" max={1} value={load.progress} />}
              {load.phase === 'error' && <button className="secondary-button" onClick={() => setAttempt(value => value + 1)}>Retry loading</button>}
            </div>}
          </div>

          {!editing && <HotspotPanel editor={editor} />}
          {inspector && <div className="v2-navigation-hint" role="status">
            <strong>Spectator Mode active</strong>
            <span>W/S along view · A/D strafe · Q/E down/up · Shift ×{spectatorBoost} · Esc to exit</span>
            <span>Free-fly · no collision protection</span>
          </div>}
          {!editing && definition.navigationMode === 'walking' && <div className="v2-navigation-hint" role="status">
            <strong>{walking ? 'Walking active' : 'Walking prototype'}</strong>
            <span>{walking ? 'WASD · horizontal movement · Shift boost · drag to look · Esc to stop' : 'Start walking, then focus the viewport. Drag to look.'}</span>
            <span>Flat reference floor · no wall/equipment collision · eye height {definition.interaction?.walking?.eyeHeight} scene units</span>
          </div>}
          {editor.placing && <div className="v2-placement-hint" role="status">Click model geometry to place a hotspot · Esc cancels</div>}
        </div>
        <footer className="v2-status">
          <span role="status">{transitioning ? 'Moving to inspection view…' : editor.message || (ready ? 'Environment loaded · Original model materials' : load.message)}</span>
          <span>{editor.data.zones.length} views · {editor.data.hotspots.length} hotspots</span>
        </footer>
        {(editor.error || interactionError) && <p className="v2-error" role="alert">{editor.error || interactionError}</p>}
      </section>
      <aside className="v2-inspector v2-panel" id="v2-inspector" aria-label="Contextual Inspector">
        <div className="v2-panel-heading">
          <h2>Inspector</h2>
          <button aria-label="Close Inspector" onClick={() => { setInspectorOpen(false); document.getElementById('v2-inspector-toggle')?.focus() }}>×</button>
        </div>
        <div className="v2-panel-scroll">
          {editing && (editor.draft || editor.selected) ? <HotspotPanel editor={editor} /> : <>
            {inspectorTab === 'zone' && selectedZone ? <ZoneInspector zone={selectedZone} editor={editor} ready={ready && !transitioning} /> : inspectorTab !== 'save' || !editing ? <EnvironmentInfo definition={definition} /> : null}
            {editing && <>
              {inspectorTab === 'save' && <ZoneCalibration editor={editor} ready={ready} transitioning={transitioning} expanded />}
              {inspectorTab !== 'save' && <button className="v2-save-shortcut" disabled={!ready || !editor.editable} onClick={() => revealInspector('save')}>Save current view as a zone ↗</button>}
              <details className="camera-calibration">
                <summary>Advanced Camera Calibration · {definition.camera.calibration === 'provisional' ? 'Review required' : 'Reviewed viewpoint'}</summary>
                <p>Temporary setup controls. Right-handed scene coordinates; one unit is not yet verified as one metre. Changes stay in this session.</p>
                <button className="text-button" disabled={load.phase !== 'ready' || transitioning} onClick={() => {
                  const current = viewer.current?.capture()
                  if (current) setCalibration({ ...definition.camera, ...current })
                }}>Read current camera</button>
                <div className="calibration-fields">
                  {(['position', 'target'] as const).map(key => <fieldset key={key}>
                    <legend>{key === 'position' ? 'Camera position' : 'Look target'}</legend>
                    {(['X', 'Y', 'Z'] as const).map((axis, index) => <label key={axis}>{axis}<input type="number" step="0.05" value={calibration[key][index]} onChange={event => {
                      const value = event.currentTarget.valueAsNumber
                      if (!Number.isFinite(value)) return
                      const vector: [number, number, number] = [...calibration[key]]
                      vector[index] = value
                      applyCamera({ ...calibration, [key]: vector })
                    }} />
                    </label>)}
                  </fieldset>)}
                  <label>Field of view<input type="number" min={zoomConfiguration.minDegrees} max={zoomConfiguration.maxDegrees} value={calibration.fovDegrees} onChange={event => {
                    const value = event.currentTarget.valueAsNumber
                    if (value >= zoomConfiguration.minDegrees && value <= zoomConfiguration.maxDegrees) applyCamera({ ...calibration, fovDegrees: value })
                  }} />
                  </label>
                </div>
              </details>
            </>}
          </>}
        </div>
      </aside>
    </div>
  </section>
}

