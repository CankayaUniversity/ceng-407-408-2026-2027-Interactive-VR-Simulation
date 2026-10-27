import type { EnvironmentEditorState } from './useEnvironmentEditor'
import { childrenOf, zonePath } from './zoneHierarchy'

export function TrainingNavigation({ editor, name, ready }: { editor: EnvironmentEditorState; name: string; ready: boolean }) {
  const path = zonePath(editor.data.zones, editor.activeZone)
  const current = path.at(-1)
  const children = childrenOf(editor.data.zones, current?.id)
  const points = editor.data.hotspots.filter(spot => current ? spot.zoneId === current.id : !spot.zoneId || !editor.data.zones.some(zone => zone.id === spot.zoneId))
  const siblings = current ? childrenOf(editor.data.zones, current.parentZoneId) : []
  const index = siblings.findIndex(zone => zone.id === current?.id)
  return <div className="training-navigation">
    <nav aria-label="Current training area" className="training-breadcrumb"><button disabled={!ready} onClick={editor.overview}>{name}</button>{path.map((zone, index) => <span key={zone.id}><span aria-hidden="true">/</span><button disabled={!ready} aria-current={index === path.length - 1 ? 'location' : undefined} onClick={() => editor.goToZone(zone.id)}>{zone.title}</button></span>)}</nav>
    {editor.mode === 'preview' && <div className="training-tour">
      <div className="tour-context">{!current && <strong>Choose an area to begin</strong>}{current && <button disabled={!ready} onClick={editor.parent}>Back to parent</button>}{current && siblings.length > 1 && <div role="group" aria-label="Adjacent areas"><button disabled={!ready || index <= 0} onClick={() => editor.goToZone(siblings[index - 1].id)}>Previous area</button><button disabled={!ready || index === siblings.length - 1} onClick={() => editor.goToZone(siblings[index + 1].id)}>Next area</button></div>}</div>
      <div className="tour-items">{children.map(zone => <button key={zone.id} disabled={!ready} onClick={() => editor.goToZone(zone.id)}>{zone.title}<span>Open area</span></button>)}{points.map(spot => <button key={spot.id} disabled={!ready} onClick={() => editor.select(spot.id)}>{spot.title}<span>{current ? 'Information point' : 'Unassigned point · stays in this view'}</span></button>)}{!children.length && !points.length && <p>{current ? 'No sub-areas or information points here yet.' : 'No saved training content yet. Switch to Editor to add an area.'}</p>}</div>
    </div>}
  </div>
}
