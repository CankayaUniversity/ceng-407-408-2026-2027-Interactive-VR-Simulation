import { useState } from 'react'
import type { EnvironmentEditorState } from './useEnvironmentEditor'
import type { InspectionZone, SceneDefinition } from './types'

export function SceneExplorer({ editor, ready, onZone, onHotspot }: {
  editor: EnvironmentEditorState; ready: boolean; onZone: (id: string) => void; onHotspot: (id: string) => void
}) {
  const [query, setQuery] = useState('')
  const matches = (title: string) => title.toLocaleLowerCase().includes(query.toLocaleLowerCase().trim())
  const zones = editor.data.zones.filter(zone => matches(zone.title))
  const hotspots = editor.data.hotspots.filter(spot => matches(spot.title))
  return <>
    <label className="v2-search">Search scene<input type="search" value={query} placeholder="Find a view or hotspot…" onChange={event => setQuery(event.target.value)} />
    </label>
    <details className="v2-tree-section" open>
      <summary>Inspection zones <span>{zones.length}</span>
      </summary>
      <ul>{zones.map(zone => <li key={zone.id}>
        <button disabled={!ready} aria-current={editor.activeZone === zone.id ? 'location' : undefined} onClick={() => onZone(zone.id)}>
          <span aria-hidden="true">◈</span>{zone.title}</button>
      </li>)}</ul>
      {!zones.length && <p>{query ? 'No matching zones.' : 'No saved inspection zones.'}</p>}
    </details>
    <details className="v2-tree-section" open>
      <summary>Saved hotspots <span>{hotspots.length}</span>
      </summary>
      <ul>{hotspots.map(spot => <li key={spot.id}>
        <button disabled={!ready} aria-current={editor.selected?.id === spot.id ? 'true' : undefined} onClick={() => onHotspot(spot.id)}>
          <span aria-hidden="true">⊙</span>{spot.title}</button>
      </li>)}</ul>
      {!hotspots.length && <p>{query ? 'No matching hotspots.' : 'No saved hotspots.'}</p>}
    </details>
    <p className="v2-panel-note">Views and annotations saved in this browser.</p>
  </>
}

export function ZoneInspector({ zone, editor, ready }: { zone: InspectionZone; editor: EnvironmentEditorState; ready: boolean }) {
  return <section className="v2-properties">
    <p className="v2-kicker">INSPECTION ZONE</p>
    <h3>{zone.title}</h3>
    <dl>
      <dt>Position · X / Y / Z</dt>
      <dd>{zone.camera.position.map(n => n.toFixed(3)).join(' / ')}</dd>
      <dt>Look target · X / Y / Z</dt>
      <dd>{zone.camera.target.map(n => n.toFixed(3)).join(' / ')}</dd>
      <dt>Field of view</dt>
      <dd>{zone.camera.fovDegrees.toFixed(1)}°</dd>
      <dt>Review status</dt>
      <dd>{zone.calibration}</dd>
    </dl>
    <button className="secondary-button" disabled={!ready} onClick={() => editor.goToZone(zone.id)}>Open saved view</button>{editor.editable && <button className="text-button delete-action" disabled={!ready} onClick={() => editor.deleteZone(zone.id)}>Delete zone {zone.title}</button>}</section>
}

export function EnvironmentInfo({ definition }: { definition: SceneDefinition }) {
  return <section className="v2-properties">
    <p className="v2-kicker">ENVIRONMENT</p>
    <h3>{definition.displayName}</h3>
    <dl>
      <dt>Navigation</dt>
      <dd>{definition.navigationMode === 'walking' ? 'Walking prototype · flat floor' : 'Seated inspection'}</dd>
      <dt>Coordinate system</dt>
      <dd>Right-handed · Y up</dd>
      <dt>Viewpoint</dt>
      <dd>{definition.camera.calibration}</dd>
      <dt>Storage</dt>
      <dd>Local browser · schema v1</dd>
    </dl>
    <p className="v2-panel-note">Camera and world scale require team review. Original model transforms are preserved.</p>
    {definition.interaction?.walking && <p className="v2-panel-note">Prototype: fixed floor Y={definition.interaction.walking.groundHeight}, eye height {definition.interaction.walking.eyeHeight} scene units. No wall or equipment collision. Starting walking restores standing height.</p>}
    {definition.attribution && <div className="v2-attribution">
      <p>Model: {definition.attribution.author}</p>
      <a href={definition.attribution.sourceUrl} target="_blank" rel="noreferrer">Original model ↗</a>
      <a href={definition.attribution.licenseUrl} target="_blank" rel="noreferrer">{definition.attribution.license} ↗</a>
      <p>Attribution from embedded asset metadata; verify before redistribution.</p>
    </div>}
  </section>
}
