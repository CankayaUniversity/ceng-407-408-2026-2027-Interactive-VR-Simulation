import { useRef, useState, type KeyboardEvent } from 'react'
import type { EnvironmentEditorState } from './useEnvironmentEditor'
import { childrenOf, searchAreaIds, zonePath } from './zoneHierarchy'

function TreeIcon({ point = false }: { point?: boolean }) {
  return <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">{point ? <><circle cx="12" cy="12" r="8" /><path d="M12 10v7m0-10v1" /></> : <><rect x="3" y="4" width="18" height="15" rx="2" /><path d="m8 14 3-3 3 2 3-5" /></>}</svg>
}
export function TrainingTree({ editor, ready, onZone, onHotspot, onSaveView }: {
  editor: EnvironmentEditorState; ready: boolean; onZone: (id: string) => void; onHotspot: (id: string) => void; onSaveView: () => void
}) {
  const [query, setQuery] = useState('')
  const [collapsed, setCollapsed] = useState<Set<string>>(() => new Set())
  const root = useRef<HTMLDivElement>(null)
  const [focused, setFocused] = useState<string>('overview')
  const matching = searchAreaIds(editor.data, query)
  const searching = !!query.trim()
  const match = (title: string) => title.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase())
  const unassigned = editor.data.hotspots.filter(spot => !spot.zoneId || !editor.data.zones.some(zone => zone.id === spot.zoneId))
  const areaVisible = (id: string) => (!searching || matching.has(id)) && zonePath(editor.data.zones, id).slice(0, -1).every(zone => searching || !collapsed.has(zone.id))
  const pointVisible = (id: string) => {
    const spot = editor.data.hotspots.find(item => item.id === id)
    if (!spot) return false
    const assigned = editor.data.zones.find(zone => zone.id === spot.zoneId)
    return (!searching || match(spot.title) || !!assigned && match(assigned.title)) && (!assigned || areaVisible(assigned.id) && (searching || !collapsed.has(assigned.id)))
  }
  const focusedVisible = focused === 'overview' || editor.data.zones.some(zone => zone.id === focused && areaVisible(zone.id)) || pointVisible(focused)
  const tabStop = (id: string) => id === (focusedVisible ? focused : 'overview') ? 0 : -1
  const toggle = (id: string) => setCollapsed(previous => { const next = new Set(previous); if (next.has(id)) next.delete(id); else next.add(id); return next })
  function keyboard(event: KeyboardEvent<HTMLDivElement>) {
    const row = (event.target as HTMLElement).closest<HTMLElement>('[role=treeitem]')
    if (!row || !root.current) return
    const rows = [...root.current.querySelectorAll<HTMLElement>('[role=treeitem]')]
    const index = rows.indexOf(row)
    let target: HTMLElement | undefined
    if (event.key === 'ArrowDown') target = rows[Math.min(index + 1, rows.length - 1)]
    else if (event.key === 'ArrowUp') target = rows[Math.max(index - 1, 0)]
    else if (event.key === 'Home') target = rows[0]
    else if (event.key === 'End') target = rows.at(-1)
    else if (event.key === 'ArrowRight') {
      if (row.getAttribute('aria-expanded') === 'false') toggle(row.dataset.item!)
      else if (row.getAttribute('aria-expanded') === 'true') target = rows[index + 1]
    } else if (event.key === 'ArrowLeft') {
      if (row.getAttribute('aria-expanded') === 'true') toggle(row.dataset.item!)
      else target = rows.find(item => item.dataset.item === row.dataset.parent)
    } else if (event.key === 'Enter' || event.key === ' ') row.click()
    else return
    event.preventDefault()
    target?.focus()
  }
  const pointRows = (parent: string, depth: number) => editor.data.hotspots.filter(spot => spot.zoneId === parent && (!searching || match(spot.title) || match(editor.data.zones.find(zone => zone.id === parent)?.title ?? ''))).map(spot => <div key={spot.id} role="treeitem" aria-level={depth} aria-selected={editor.selected?.id === spot.id} tabIndex={tabStop(spot.id)} data-item={spot.id} data-parent={parent} onFocus={() => setFocused(spot.id)} onClick={() => ready && onHotspot(spot.id)} className="training-row point-row"><TreeIcon point /><span>{spot.title}</span></div>)
  function areas(parent: string | undefined, depth: number) {
    return childrenOf(editor.data.zones, parent).filter(zone => !searching || matching.has(zone.id)).map(zone => {
      const childCount = childrenOf(editor.data.zones, zone.id).length
      const pointCount = editor.data.hotspots.filter(spot => spot.zoneId === zone.id).length
      const expandable = childCount + pointCount > 0
      const expanded = expandable && (searching || !collapsed.has(zone.id))
      return <div role="none" key={zone.id}>
        <div role="treeitem" aria-label={zone.title} aria-level={depth} aria-expanded={expandable ? expanded : undefined} aria-selected={editor.selectedZoneId === zone.id} aria-current={editor.activeZone === zone.id ? 'location' : undefined} aria-busy={editor.pendingZone === zone.id} aria-disabled={!ready} tabIndex={tabStop(zone.id)} data-item={zone.id} data-parent={parent ?? 'overview'} onFocus={() => setFocused(zone.id)} onClick={() => ready && onZone(zone.id)} className="training-row">
          <button tabIndex={-1} disabled={!expandable} className="tree-toggle" aria-label={`${expanded ? 'Collapse' : 'Expand'} ${zone.title}`} onClick={event => { event.stopPropagation(); toggle(zone.id) }}><svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" aria-hidden="true" style={{ transform: expanded ? 'rotate(90deg)' : undefined }}><path d="m6 3 5 5-5 5" /></svg></button>
          <TreeIcon /><span>{zone.title}{editor.activeZone === zone.id && <small>Current area</small>}{editor.pendingZone === zone.id && <small>Moving…</small>}</span><small className="tree-count" title={`${childCount} sub-areas, ${pointCount} information points`}>{childCount + pointCount || ''}</small>
        </div>
        {expanded && <div role="group" className="training-children">{areas(zone.id, depth + 1)}{pointRows(zone.id, depth + 1)}</div>}
      </div>
    })
  }
  return <>
    <label className="v2-search">Find an area or point<input type="search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Search training content…" /></label>
    <p className="tree-summary">{editor.data.zones.length} areas · {editor.data.hotspots.length} points</p>
    <div ref={root} role="tree" aria-label="Training areas and information points" onKeyDown={keyboard} className="training-tree">
      <div role="treeitem" aria-level={1} aria-selected={false} tabIndex={tabStop('overview')} data-item="overview" className="training-row tree-overview" onFocus={() => setFocused('overview')} onClick={() => ready && editor.overview()}><TreeIcon /><span>Environment overview</span></div>
      {areas(undefined, 1)}
      {unassigned.length > 0 && <div role="none"><p className="tree-unassigned-label">Unassigned points · {unassigned.length}</p><div role="group">{unassigned.filter(spot => !searching || match(spot.title)).map(spot => <div role="treeitem" aria-level={1} aria-selected={editor.selected?.id === spot.id} key={spot.id} tabIndex={tabStop(spot.id)} data-item={spot.id} className="training-row point-row" onFocus={() => setFocused(spot.id)} onClick={() => ready && onHotspot(spot.id)}><TreeIcon point /><span>{spot.title}</span></div>)}</div></div>}
    </div>
    {searching && !matching.size && !unassigned.some(spot => match(spot.title)) && <p className="v2-panel-note">No matching areas or information points.</p>}
    {!editor.data.zones.length && <div className="v2-list-empty"><p>{editor.editable ? 'Frame a panel or piece of equipment, then save its view as your first area.' : 'No training areas yet. Switch to Editor to prepare this environment.'}</p>{editor.editable && <button disabled={!ready} onClick={onSaveView}>Save first area</button>}</div>}
    <p className="v2-panel-note">{editor.editable ? 'Select an area to organise it. Open its saved view from Inspector.' : 'Choose an area to explore. Unassigned points open here without moving the camera.'}</p>
  </>
}
