import { useEffect, useRef, useState } from 'react'
import type { SubmitEvent } from 'react'
import type { Project } from './types'

interface ProjectDialogProps {
  mode: 'create' | 'rename' | 'delete'
  project?: Project
  onSubmit: (name: string, description: string) => boolean
  onClose: () => void
}

export function ProjectDialog({ mode, project, onSubmit, onClose }: ProjectDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [name, setName] = useState(project?.name ?? '')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const deleting = mode === 'delete'
  const title = deleting ? 'Delete this space?' : mode === 'rename' ? 'Rename space' : 'Create a new space'

  useEffect(() => {
    const dialog = dialogRef.current!
    const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
    dialog.showModal()
    return () => {
      dialog.close()
      if (trigger?.isConnected) trigger.focus()
      else document.querySelector<HTMLElement>('#dashboard-heading')?.focus()
    }
  }, [])

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!deleting && !name.trim()) {
      setError('Enter a space name, not only spaces.')
      return
    }
    if (onSubmit(name.trim(), description.trim())) onClose()
    else setError('Unable to save this change. Check browser storage and try again.')
  }

  return (
    <dialog ref={dialogRef} className="project-dialog" aria-labelledby="dialog-title" aria-describedby="dialog-description" onCancel={onClose}>
      <form onSubmit={handleSubmit}>
        <p className="eyebrow">Workplace Studio</p>
        <h2 id="dialog-title">{title}</h2>
        <p id="dialog-description">{deleting ? `“${project?.name}” will be permanently removed from this browser. This cannot be undone.` : mode === 'rename' ? 'Confirm a new name for this project.' : 'Start with a name. You can rename your space later.'}</p>
        {!deleting && <div className="form-field"><label htmlFor="space-name">Space name <span>(required)</span></label><input id="space-name" value={name} onChange={(event) => setName(event.target.value)} required maxLength={100} autoFocus aria-describedby={error ? 'form-error' : undefined} /></div>}
        {mode === 'create' && <div className="form-field"><label htmlFor="space-description">Description <span>(optional)</span></label><textarea id="space-description" value={description} onChange={(event) => setDescription(event.target.value)} maxLength={500} rows={3} /></div>}
        {error && <p id="form-error" className="error-message" role="alert">{error}</p>}
        <div className="dialog-actions"><button type="button" className="secondary-button" onClick={onClose} autoFocus={deleting}>Cancel</button><button className={deleting ? 'danger-button' : 'primary-button'} type="submit" disabled={!deleting && !name.trim()}>{deleting ? 'Delete space' : mode === 'rename' ? 'Confirm rename' : 'Create space'}</button></div>
      </form>
    </dialog>
  )
}
