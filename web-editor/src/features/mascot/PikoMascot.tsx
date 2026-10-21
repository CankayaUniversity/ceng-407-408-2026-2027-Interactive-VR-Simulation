import { useEffect, useRef, useState } from 'react'
import { Engine } from '@babylonjs/core/Engines/engine'
import { Scene } from '@babylonjs/core/scene'
import { ArcRotateCamera } from '@babylonjs/core/Cameras/arcRotateCamera'
import { HemisphericLight } from '@babylonjs/core/Lights/hemisphericLight'
import { DirectionalLight } from '@babylonjs/core/Lights/directionalLight'
import { Color3, Color4 } from '@babylonjs/core/Maths/math.color'
import { Vector3 } from '@babylonjs/core/Maths/math.vector'
import { LoadAssetContainerAsync } from '@babylonjs/core/Loading/sceneLoader'
import '@babylonjs/loaders/glTF'
import '../../styles/piko-mascot.css'

function PikoPortrait() {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const waveRef = useRef<(() => void) | null>(null)
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [animation, setAnimation] = useState<'idle' | 'wave'>('idle')

  useEffect(() => {
    const canvas = canvasRef.current!
    let disposed = false
    let engine: Engine | undefined
    let scene: Scene | undefined
    let observer: ResizeObserver | undefined
    let removePlaybackListeners: (() => void) | undefined

    // Defer creation so StrictMode's first cleanup does not create a second engine.
    void Promise.resolve().then(async () => {
      if (disposed) return
      try {
        engine = new Engine(canvas, true, {
          alpha: true, stencil: false, preserveDrawingBuffer: true,
          premultipliedAlpha: false, powerPreference: 'low-power',
        })
        engine.maxFPS = 30
        scene = new Scene(engine)
        scene.useRightHandedSystem = true
        scene.clearColor = new Color4(0, 0, 0, 0)
        const camera = new ArcRotateCamera('piko-portrait-camera', Math.PI / 2 + .08, Math.PI / 2 - .04, 5, Vector3.Zero(), scene)
        camera.fov = .65
        camera.minZ = .01
        scene.activeCamera = camera
        const fill = new HemisphericLight('piko-fill', new Vector3(0, 1, 1), scene)
        fill.intensity = 1.3
        fill.groundColor = new Color3(.35, .4, .5)
        const key = new DirectionalLight('piko-key', new Vector3(-.3, -.5, -1), scene)
        key.intensity = 2.2

        const container = await LoadAssetContainerAsync('/guides/piko/piko.glb', scene, {
          pluginOptions: { gltf: { animationStartMode: 0 } },
        })
        if (disposed) { container.dispose(); return }
        container.addAllToScene()
        // This GLB stores the articulated rest transforms in Idle's first frame.
        container.animationGroups.forEach(group => group.stop(true))
        const idle = container.animationGroups.find(group => group.name === 'Idle')
        const wave = container.animationGroups.find(group => group.name === 'Wave')
        if (!idle || !wave) throw new Error('Piko Idle/Wave animations unavailable')
        idle.start(false)
        idle.goToFrame(idle.from)
        idle.pause()
        for (const mesh of container.meshes) mesh.computeWorldMatrix(true)
        const bounds = container.rootNodes[0]?.getHierarchyBoundingVectors(true)
        if (!bounds || !Number.isFinite(bounds.min.x) || !Number.isFinite(bounds.max.x)) throw new Error('Piko bounds unavailable')
        // Keep a fixed camera that also fits the raised hand throughout Wave.
        idle.stop(true)
        wave.start(false)
        wave.pause()
        for (let step = 0; step <= 10; step++) {
          wave.goToFrame(wave.from + (wave.to - wave.from) * step / 10)
          for (const mesh of container.meshes) mesh.computeWorldMatrix(true)
          const poseBounds = container.rootNodes[0].getHierarchyBoundingVectors(true)
          bounds.min.minimizeInPlace(poseBounds.min)
          bounds.max.maximizeInPlace(poseBounds.max)
        }
        wave.stop(true)
        idle.start(true)
        idle.goToFrame(idle.from)
        idle.pause()
        const size = bounds.max.subtract(bounds.min)
        camera.setTarget(bounds.min.add(bounds.max).scale(.5))
        await scene.whenReadyAsync()
        if (disposed) return

        const renderPortrait = () => {
          if (!disposed) scene?.render()
        }
        const resizePortrait = () => {
          if (disposed || !engine || !scene) return
          engine.resize()
          const aspect = engine.getRenderWidth() / Math.max(1, engine.getRenderHeight())
          camera.radius = Math.max(size.y, size.x / aspect) / (2 * Math.tan(camera.fov / 2)) * 1.12 + size.z / 2
          if (!document.hidden) renderPortrait()
        }
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)')
        let waving = false
        const syncPlayback = () => {
          if (disposed) return
          engine!.stopRenderLoop(renderPortrait)
          const active = waving ? wave : idle
          active.pause()
          if (document.hidden) return
          if (reducedMotion.matches && !waving) {
            idle.goToFrame(idle.from)
            renderPortrait()
          } else {
            active.play(!waving)
            engine!.runRenderLoop(renderPortrait)
          }
        }
        wave.onAnimationGroupEndObservable.add(() => {
          if (disposed) return
          waving = false
          idle.start(true)
          idle.goToFrame(idle.from)
          setAnimation('idle')
          syncPlayback()
        })
        waveRef.current = () => {
          if (disposed || waving || document.hidden) return
          idle.stop(true)
          waving = true
          wave.start(false)
          wave.goToFrame(wave.from)
          setAnimation('wave')
          syncPlayback()
        }
        document.addEventListener('visibilitychange', syncPlayback)
        reducedMotion.addEventListener('change', syncPlayback)
        removePlaybackListeners = () => {
          document.removeEventListener('visibilitychange', syncPlayback)
          reducedMotion.removeEventListener('change', syncPlayback)
        }
        observer = new ResizeObserver(resizePortrait)
        observer.observe(canvas)
        resizePortrait()
        syncPlayback()
        setState('ready')
      } catch (error) {
        if (disposed) return
        waveRef.current = null
        removePlaybackListeners?.()
        observer?.disconnect()
        scene?.dispose()
        engine?.dispose()
        setState('error')
        if (import.meta.env.DEV) console.warn('Piko portrait could not load', error)
      }
    })

    return () => {
      disposed = true
      waveRef.current = null
      removePlaybackListeners?.()
      observer?.disconnect()
      scene?.dispose()
      engine?.dispose()
    }
  }, [])

  return <button type="button" className="piko-portrait" data-state={state} data-animation={animation}
    disabled={state !== 'ready'} aria-label="Wave with Piko" title="Click Piko to wave hello"
    onClick={() => waveRef.current?.()}>
    <canvas ref={canvasRef} aria-hidden="true" />
  </button>
}

export default function PikoMascot() {
  const [minimized, setMinimized] = useState(false)
  return <aside className={`piko-mascot${minimized ? ' is-minimized' : ''}`} aria-label="Piko">
    {minimized ? <button className="piko-show" onClick={() => setMinimized(false)} aria-label="Show Piko"><span aria-hidden="true" />Piko</button> : <>
      <button className="piko-minimize" onClick={() => setMinimized(true)} aria-label="Minimize Piko" title="Minimize Piko">−</button>
      <PikoPortrait />
      <span className="piko-name">Piko</span>
    </>}
  </aside>
}
