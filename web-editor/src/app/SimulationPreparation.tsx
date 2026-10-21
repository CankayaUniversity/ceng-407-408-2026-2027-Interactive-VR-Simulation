import { useEffect, useRef, useState } from 'react'
import type { SceneDefinition } from '../features/environments/types'
import '../styles/simulation-preparation.css'

/** A full 4s inhale / 6s exhale before mounting the selected workspace. */
export function SimulationPreparation({ definition, onComplete, onCancel }: {
  definition: SceneDefinition
  onComplete: () => void
  onCancel: () => void
}) {
  const [exhaling, setExhaling] = useState(false)
  const surface = useRef<HTMLElement>(null)
  const cue = useRef<HTMLHeadingElement>(null)

  useEffect(() => {
    cue.current?.focus()
    const exhale = window.setTimeout(() => setExhaling(true), 4000)
    const complete = window.setTimeout(onComplete, 10000)
    const cancel = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onCancel()
    }
    window.addEventListener('keydown', cancel)
    return () => {
      window.clearTimeout(exhale)
      window.clearTimeout(complete)
      window.removeEventListener('keydown', cancel)
    }
  }, [onComplete, onCancel])

  useEffect(() => {
    const element = surface.current
    if (!element) return
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)')
    let x = 0, y = 0, targetX = 0, targetY = 0, previous = 0, frame = 0
    const move = (event: PointerEvent) => {
      targetX = (event.clientX / window.innerWidth - .5) * 26
      targetY = (event.clientY / window.innerHeight - .5) * 20
    }
    const reset = () => { targetX = 0; targetY = 0 }
    const draw = (now: number) => {
      const dt = previous ? Math.min((now - previous) / 1000, .1) : 0
      previous = now
      const ease = 1 - Math.exp(-dt / 1.8)
      x += (targetX - x) * ease
      y += (targetY - y) * ease
      element.style.setProperty('--breath-x', `${reduced.matches ? 0 : x}px`)
      element.style.setProperty('--breath-y', `${reduced.matches ? 0 : y}px`)
      frame = requestAnimationFrame(draw)
    }
    frame = requestAnimationFrame(draw)
    window.addEventListener('pointermove', move, { passive: true })
    document.documentElement.addEventListener('pointerleave', reset)
    return () => {
      cancelAnimationFrame(frame)
      window.removeEventListener('pointermove', move)
      document.documentElement.removeEventListener('pointerleave', reset)
    }
  }, [])

  return <section ref={surface} className="simulation-preparation" aria-label={`Prepare for ${definition.displayName}`}>
    <div className="preparation-background" aria-hidden="true" style={{ backgroundImage: `url("/previews/environments/${definition.id}.jpg")` }} />
    <div className="preparation-shade" aria-hidden="true" />
    <div className="preparation-breath">
      <div className="preparation-light" aria-hidden="true">
        <div className="preparation-halo" />
        <div className="preparation-corona" />
        <div className="preparation-sphere" />
      </div>
      <h1 ref={cue} tabIndex={-1} aria-live="polite">{exhaling ? 'Slowly exhale' : 'Breathe in'}</h1>
    </div>
    <p className="sr-only">The environment opens after one ten-second breathing cycle. Press Escape to return to environments.</p>
  </section>
}
