import { useEffect, useRef, useState } from 'react'
import type { EnvironmentEditorState } from './useEnvironmentEditor'
import { HotspotInfoCard } from './HotspotInfoCard'
import { zonePath } from './zoneHierarchy'

export function HotspotPanel({ editor }: { editor: EnvironmentEditorState }) {
  const heading = useRef<HTMLHeadingElement>(null)
  const item = editor.draft ?? editor.selected
  const itemId = item?.id
  useEffect(() => { if (itemId) heading.current?.focus() }, [itemId])
  if (!item) return null
  if (editor.mode === 'preview') return <HotspotInfoCard hotspot={item} zoneTitle={item.zoneId ? zonePath(editor.data.zones, item.zoneId).map(zone => zone.title).join(' / ') || 'Unavailable area (saved reference)' : undefined} onClose={editor.closePanel} />
  return <section className="hotspot-panel" aria-labelledby="hotspot-panel-title">
    <div className="hotspot-panel-heading">
      <h2 id="hotspot-panel-title" tabIndex={-1} ref={heading}>{editor.draft ? (editor.selected ? 'Edit hotspot' : 'New hotspot · draft') : item.title}</h2>
      <button className="text-button" onClick={editor.closePanel} aria-label="Close hotspot information">Close ×</button>
    </div>
    {editor.draft && editor.editable ? <form onSubmit={event => { event.preventDefault(); editor.saveHotspot() }}>
      <label>Hotspot title<input autoComplete="off" maxLength={100} required value={editor.draft.title} onChange={event => editor.setDraft({ ...editor.draft!, title: event.target.value })} />
      </label>
      <label>Training information<textarea maxLength={2000} rows={4} value={editor.draft.description} onChange={event => editor.setDraft({ ...editor.draft!, description: event.target.value })} />
      </label>
      <p className="editor-help">Area: {zonePath(editor.data.zones, editor.draft.zoneId ?? null).map(zone => zone.title).join(' / ') || 'Unassigned · no camera navigation'}</p>
      <label>Move to area<select value={editor.draft.zoneId ?? ''} onChange={event => {
        const { zoneId: _zoneId, ...rest } = editor.draft!
        editor.setDraft(event.target.value ? { ...rest, zoneId: event.target.value } : rest)
      }}>
        <option value="">Unassigned · keep current view</option>{editor.draft.zoneId && !editor.data.zones.some(zone => zone.id === editor.draft?.zoneId) && <option value={editor.draft.zoneId}>Unavailable view (saved reference)</option>}{editor.data.zones.map(zone => <option key={zone.id} value={zone.id}>{zonePath(editor.data.zones, zone.id).map(item => item.title).join(' / ')}</option>)}</select>
      </label>
      <p className="editor-help">Save keeps this hotspot and its surface position. Cancel discards your changes.</p>
      <div className="hotspot-panel-actions">
        <button className="primary-button" type="submit" disabled={!editor.draft.title.trim()}>Save hotspot</button>
        <button className="secondary-button" type="button" onClick={editor.closePanel}>Cancel</button></div>{editor.selected && <details className="v2-danger-section"><summary>Delete this hotspot</summary><p>Permanently removes this hotspot and its training information. This cannot be undone.</p><button className="danger-button" type="button" onClick={editor.deleteHotspot}>Delete hotspot</button></details>}
    </form> : <>
      <p className="hotspot-description">{item.description || 'No training information has been added.'}</p>{item.zoneId && <p className="editor-help">Zone: {editor.data.zones.find(zone => zone.id === item.zoneId)?.title ?? 'Unavailable zone (saved reference)'}</p>}</>}
  </section>
}

export function ZoneCalibration({ editor, ready, transitioning, expanded = false, parentId }: { editor: EnvironmentEditorState; ready: boolean; transitioning: boolean; expanded?: boolean; parentId?: string }) {
  const [title, setTitle] = useState('')
  const [reviewed, setReviewed] = useState(false)
  const [parent, setParent] = useState(parentId ?? '')
  if (editor.mode !== 'editor') return null
  return <details className="zone-calibration" open={expanded || undefined}>
    <summary>{parent ? 'Add sub-area' : 'Save an area'}</summary>
    <p>Frame the equipment with drag and zoom, or use Spectator to move. Save captures your current camera angle.</p>
    <form onSubmit={event => { event.preventDefault(); if (editor.saveZone(title, reviewed, parent || undefined)) { setTitle(''); setReviewed(false) } }}>
      <label>Parent area<select value={parent} onChange={event => setParent(event.target.value)}><option value="">Environment · root area</option>{editor.data.zones.map(zone => <option value={zone.id} key={zone.id}>{zonePath(editor.data.zones, zone.id).map(item => item.title).join(' / ')}</option>)}</select></label>
      <label>View title<input maxLength={100} required value={title} onChange={event => setTitle(event.target.value)} placeholder="Name this camera view" />
      </label>
      <label className="checkbox-label">
        <input type="checkbox" checked={reviewed} onChange={event => setReviewed(event.target.checked)} />I visually reviewed this camera pose. I will test the full transition before training use.</label>
      <button className="secondary-button" type="submit" disabled={!editor.editable || !ready || transitioning || !reviewed || !title.trim()}>Save this view</button>
    </form>
    <details className="v2-review-note"><summary>Before training use</summary><p>Test the destination and the complete camera transition. Paths have limited geometry checks, not full collision protection. Camera scale is provisional.</p></details>
  </details>
}
