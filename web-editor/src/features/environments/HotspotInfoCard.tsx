import { useEffect, useRef } from 'react'
import type { Hotspot } from './types'

export function HotspotInfoCard({ hotspot, zoneTitle, onClose }: { hotspot: Hotspot; zoneTitle?: string; onClose: () => void }) {
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => { heading.current?.focus() }, [hotspot.id])
  return <article className="hotspot-info-card" aria-labelledby="hotspot-panel-title">
    <header className="info-card-header">
      <p className="info-card-label">Training information</p>
      <button className="info-card-close" onClick={onClose} aria-label="Close hotspot information">Close <span aria-hidden="true">×</span></button>
    </header>
    <div className="info-card-content">
      {zoneTitle && <p className="info-card-zone">{zoneTitle}</p>}
      <h2 id="hotspot-panel-title" ref={heading} tabIndex={-1}>{hotspot.title}</h2>
      <div className="info-card-rule" aria-hidden="true" />
      <p className="info-card-description">{hotspot.description || 'No training information has been added.'}</p>
    </div>
    <footer className="info-card-footer">Training information <span>Read-only preview</span></footer>
  </article>
}
