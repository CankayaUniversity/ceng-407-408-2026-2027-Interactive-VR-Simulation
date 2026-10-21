import { useState } from 'react'
import type { EnvironmentEditorState } from './useEnvironmentEditor'
import type { InspectionZone, SceneDefinition } from './types'

export function AuthoringHelp({ editing, disabled, onSaveView, onAddHotspot }: {
  editing: boolean; disabled: boolean; onSaveView: () => void; onAddHotspot: () => void
}) {
  return <section className="v2-authoring-start">
    <h3>{editing ? 'Create training content' : 'Explore the training'}</h3>
    <p>{editing ? 'Frame the equipment, save a view, then add a hotspot to its surface.' : 'Choose a saved view or hotspot in Scene Explorer.'}</p>
    {editing && <div>
      <button className="secondary-button" disabled={disabled} onClick={onSaveView}>Save a viewpoint</button>
      <button className="secondary-button" disabled={disabled} onClick={onAddHotspot}>Add a hotspot</button>
    </div>}
  </section>
}

export function SceneExplorer({ editor, ready, onZone, onHotspot, onSaveView, onAddHotspot, onClear }: {
  editor: EnvironmentEditorState; ready: boolean; onZone: (id: string) => void; onHotspot: (id: string) => void
  onSaveView: () => void; onAddHotspot: () => void; onClear: () => void
}) {
  const [query, setQuery] = useState('')
  const matches = (title: string) => title.toLocaleLowerCase().includes(query.toLocaleLowerCase().trim())
  const zones = editor.data.zones.filter(zone => matches(zone.title))
  const hotspots = editor.data.hotspots.filter(spot => matches(spot.title))
  return <>
    <label className="v2-search">Search scene<input type="search" value={query} placeholder="Find a view or hotspot…" onChange={event => setQuery(event.target.value)} />
    </label>
    <details className="v2-tree-section" open>
      <summary>Inspection views <span>{zones.length}{query ? ` / ${editor.data.zones.length}` : ''}</span>
      </summary>
      <ul>{zones.map(zone => <li key={zone.id}>
        <button disabled={!ready} aria-current={editor.activeZone === zone.id ? 'location' : undefined} onClick={() => onZone(zone.id)}>
          <span className="v2-item-kind" aria-hidden="true">View</span><span>{zone.title}</span></button>
      </li>)}</ul>
      {!zones.length && <div className="v2-list-empty"><p>{query ? 'No matching views.' : 'Save a camera angle to revisit equipment.'}</p>{!query && editor.editable && <button disabled={!ready} onClick={onSaveView}>Save first view</button>}</div>}
    </details>
    <details className="v2-tree-section" open>
      <summary>Equipment hotspots <span>{hotspots.length}{query ? ` / ${editor.data.hotspots.length}` : ''}</span>
      </summary>
      <ul>{hotspots.map(spot => <li key={spot.id}>
        <button disabled={!ready} aria-current={editor.selected?.id === spot.id ? 'true' : undefined} onClick={() => onHotspot(spot.id)}>
          <span className="v2-item-number" aria-hidden="true">{editor.data.hotspots.findIndex(item => item.id === spot.id) + 1}</span><span>{spot.title}</span></button>
      </li>)}</ul>
      {!hotspots.length && <div className="v2-list-empty"><p>{query ? 'No matching hotspots.' : 'Add training information to a real model surface.'}</p>{!query && editor.editable && <button disabled={!ready} onClick={onAddHotspot}>Add first hotspot</button>}</div>}
    </details>
    {(editor.selected || editor.draft) && <button className="v2-clear-selection" onClick={onClear}>Clear selection</button>}
    <p className="v2-panel-note">Saved in this browser · This environment only</p>
  </>
}

export function ZoneInspector({ zone, editor, ready }: { zone: InspectionZone; editor: EnvironmentEditorState; ready: boolean }) {
  return <section className="v2-properties">
    <p className="v2-selection-type">Saved inspection view</p>
    <h3>{zone.title}</h3>
    <details className="v2-pose-details"><summary>Camera position &amp; field of view</summary><dl>
      <dt>Position · X / Y / Z</dt>
      <dd>{zone.camera.position.map(n => n.toFixed(3)).join(' / ')}</dd>
      <dt>Look target · X / Y / Z</dt>
      <dd>{zone.camera.target.map(n => n.toFixed(3)).join(' / ')}</dd>
      <dt>Field of view</dt>
      <dd>{zone.camera.fovDegrees.toFixed(1)}°</dd>
      <dt>Review status</dt>
      <dd>{zone.calibration}</dd>
    </dl></details>
    <button className="secondary-button" disabled={!ready} onClick={() => editor.goToZone(zone.id)}>Open saved view</button>{editor.editable && <details className="v2-danger-section"><summary>Delete this view</summary><p>Hotspots stay saved, but their link to this view will be removed. This cannot be undone.</p><button className="danger-button" disabled={!ready} onClick={() => editor.deleteZone(zone.id)}>Delete view {zone.title}</button></details>}</section>
}

export function EnvironmentInfo({ definition }: { definition: SceneDefinition }) {
  return <section className="v2-properties">
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
