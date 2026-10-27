import { useState } from 'react'
import type { EnvironmentEditorState } from './useEnvironmentEditor'
import type { InspectionZone, SceneDefinition } from './types'
import { canParent, childrenOf, zonePath } from './zoneHierarchy'
import { TrainingTree } from './TrainingTree'

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
  return <><TrainingTree editor={editor} ready={ready} onZone={onZone} onHotspot={onHotspot} onSaveView={onSaveView} />{editor.editable && <div className="tree-actions"><button disabled={!ready} onClick={onSaveView}>Save an area</button><button disabled={!ready} onClick={onAddHotspot}>Add information point</button><button onClick={onClear}>Clear selection</button></div>}
    {editor.canAddStarterContent && editor.editable && <div className="v2-list-empty">
      <button disabled={!ready} onClick={editor.addSourceHotspots}>Add source annotations</button>
      <p>Add missing original annotations while keeping your existing content.</p>
    </div>}
    {editor.sourceAttribution && <p className="v2-panel-note">
      Annotations: {editor.sourceAttribution.author} · <a href={editor.sourceAttribution.sourceUrl} target="_blank" rel="noreferrer">Sketchfab source ↗</a><br />
      <a href={editor.sourceAttribution.licenseUrl} target="_blank" rel="noreferrer">{editor.sourceAttribution.license}</a>
    </p>}
</>
}

export function ZoneInspector({ zone, editor, ready, onAddChild }: { zone: InspectionZone; editor: EnvironmentEditorState; ready: boolean; onAddChild: () => void }) {
  const [title, setTitle] = useState(zone.title)
  const [parent, setParent] = useState(zone.parentZoneId ?? '')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const siblings = childrenOf(editor.data.zones, zone.parentZoneId)
  const index = siblings.findIndex(item => item.id === zone.id)
  const childCount = childrenOf(editor.data.zones, zone.id).length
  const pointCount = editor.data.hotspots.filter(spot => spot.zoneId === zone.id).length
  return <section className="v2-properties">
    <h3>{zone.title}</h3>
    <p className="v2-panel-note">{zonePath(editor.data.zones, zone.id).map(item => item.title).join(' / ')} · {childCount} sub-areas · {pointCount} points</p>
    <button className="secondary-button" disabled={!ready} onClick={() => editor.goToZone(zone.id)}>Open saved view</button>
    {editor.editable && <>
      <form className="area-form" onSubmit={event => { event.preventDefault(); editor.updateZone(zone.id, title, parent || undefined) }}>
        <label>Area name<input maxLength={100} required value={title} onChange={event => setTitle(event.target.value)} /></label>
        <label>Move to area<select value={parent} onChange={event => setParent(event.target.value)}><option value="">Environment · root area</option>{editor.data.zones.filter(item => canParent(editor.data.zones, zone.id, item.id)).map(item => <option key={item.id} value={item.id}>{zonePath(editor.data.zones, item.id).map(area => area.title).join(' / ')}</option>)}</select></label>
        <button type="submit" disabled={!title.trim()}>Save area details</button>
      </form>
      <div className="area-actions"><button disabled={!ready} onClick={onAddChild}>Add sub-area</button><button disabled={!ready} onClick={() => editor.updateZoneView(zone.id)}>Update saved view</button></div>
      <p className="v2-panel-note">Update replaces this saved camera with the view currently on screen.</p>
      <div className="area-actions" role="group" aria-label="Sibling order"><button disabled={index <= 0} onClick={() => editor.reorderZone(zone.id, -1)}>Move earlier</button><button disabled={index === siblings.length - 1} onClick={() => editor.reorderZone(zone.id, 1)}>Move later</button></div>
    </>}
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
    {editor.editable && <div className="v2-danger-section">{!confirmDelete ? <button onClick={() => setConfirmDelete(true)}>Remove area…</button> : <><p>Remove “{zone.title}”? {childCount} direct sub-areas and {pointCount} information points move to {editor.data.zones.find(item => item.id === zone.parentZoneId)?.title ?? 'the environment level'}. All deeper content is kept. Only this saved view is removed.</p><button className="danger-button" disabled={!ready} onClick={() => editor.deleteZone(zone.id)}>Confirm removal</button><button onClick={() => setConfirmDelete(false)}>Keep area</button></>}</div>}</section>
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
