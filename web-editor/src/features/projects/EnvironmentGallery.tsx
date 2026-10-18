import { useState } from 'react'
import { environments, type EnvironmentKey } from '../environments/registry'
import type { SceneDefinition } from '../environments/types'

function EnvironmentCard({ definition, onOpen }: { definition: SceneDefinition; onOpen: () => void }) {
  const [imageFailed, setImageFailed] = useState(false)
  const preview = `/previews/environments/${definition.id}`
  return <article className="home-environment-card" data-environment-id={definition.id}>
    <div className="home-preview">
      {imageFailed ? <div className="home-preview-fallback"><span aria-hidden="true">◈</span><strong>Preview unavailable</strong><span>Open the environment to explore the original model.</span></div> : <img
        src={`${preview}.jpg`}
        srcSet={`${preview}-640.jpg 640w, ${preview}.jpg 1200w`}
        sizes="(max-width: 760px) calc(100vw - 40px), (max-width: 1440px) 47vw, 900px"
        width={1200} height={675} decoding="async"
        alt={`${definition.displayName} — captured from the actual 3D model`}
        onError={() => setImageFailed(true)}
      />}
      <span className="home-preview-tag">Actual model view</span>
      {definition.attribution && <span className="home-preview-credit">{definition.attribution.author} · {definition.attribution.license}</span>}
    </div>
    <div className="home-environment-body">
      <div className="home-environment-copy">
        <p className="home-label">{definition.navigationMode === 'seated' ? 'Seated inspection' : 'Walking prototype'}</p>
        <h2><button className="home-environment-open" onClick={onOpen} aria-label={`Open ${definition.displayName}`}>{definition.displayName}</button></h2>
        <p className="home-environment-description">{definition.navigationMode === 'seated' ? 'Explore the cockpit, inspection views and saved annotations.' : 'Explore the room with flat-floor prototype navigation.'}</p>
      </div>
      <span className="home-open-action" aria-hidden="true">Open environment <span>↗</span></span>
    </div>
  </article>
}

export function EnvironmentGallery({ onExplore }: { onExplore: (key: EnvironmentKey) => void }) {
  const entries = Object.entries(environments) as [EnvironmentKey, SceneDefinition][]
  return <div className="home-environment-grid">
    {entries.map(([key, definition]) => <EnvironmentCard key={definition.id} definition={definition} onOpen={() => onExplore(key)} />)}
  </div>
}
