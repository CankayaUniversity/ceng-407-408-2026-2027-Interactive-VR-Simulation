import { lazy, Suspense, useRef, useState } from 'react'
import { Dashboard } from '../features/projects/Dashboard'
import { ProjectDialog } from '../features/projects/ProjectDialog'
import { useProjects } from '../features/projects/useProjects'
import type { Project } from '../features/projects/types'
import { environments, type EnvironmentKey } from '../features/environments/registry'
import { EnvironmentBoundary } from './EnvironmentBoundary'

const EnvironmentWorkspace = lazy(() => import('../features/environments/EnvironmentWorkspace'))

type DialogState = { type: 'create' } | { type: 'rename' | 'delete'; project: Project }

function App() {
  const { projects, error, save } = useProjects()
  const [activeId, setActiveId] = useState<string | null>(null)
  const [exploring, setExploring] = useState<EnvironmentKey | null>(null)
  const [dialog, setDialog] = useState<DialogState | null>(null)
  const [notice, setNotice] = useState('')
  const heading = useRef<HTMLHeadingElement>(null)
  const active = projects.find((project) => project.id === activeId)
  const isDashboard = !active && !exploring

  function navigate(id: string | null) {
    setExploring(null)
    setActiveId(id)
    requestAnimationFrame(() => heading.current?.focus())
  }

  function submit(name: string, description: string) {
    if (!dialog) return false
    const now = new Date().toISOString()
    let next: Project[]
    if (dialog.type === 'create') {
      next = [{ id: crypto.randomUUID(), name, description, createdAt: now, updatedAt: now }, ...projects]
    } else if (dialog.type === 'delete') {
      next = projects.filter((project) => project.id !== dialog.project.id)
    } else {
      next = projects.map((project) => project.id === dialog.project.id ? { ...project, name, updatedAt: now } : project)
    }
    if (!save(next)) return false
    setNotice(dialog.type === 'create' ? 'Space created.' : dialog.type === 'delete' ? 'Space deleted.' : 'Space renamed.')
    return true
  }

  return (
    <>
      <a className="skip-link" href="#main">Skip to content</a>
      <header className="topbar" hidden={isDashboard || !!exploring}>
        <a className="identity" href="#dashboard" onClick={() => navigate(null)}><span className="brand-mark" aria-hidden="true">W</span> Workplace Studio</a>
        <nav className="header-nav" aria-label="Main navigation">
          <button className="header-link" aria-current={!active && !exploring ? 'page' : undefined} onClick={() => navigate(null)}>Dashboard</button>
          {!active && !exploring && <a className="header-link" href="#project-directory">Projects</a>}
          <button className="header-link" aria-current={exploring ? 'page' : undefined} onClick={() => setExploring('cockpit')}>Cockpit Explorer</button>
        </nav>
      </header>
      <div className={isDashboard ? 'shell dashboard-shell' : 'shell'}>
        <main id="main" className={isDashboard ? 'main-content dashboard-main' : 'main-content'}>
          {exploring ? <EnvironmentBoundary onBack={() => navigate(null)}><Suspense fallback={<div><button className="text-button back-button" onClick={() => navigate(null)}>← Back to Dashboard</button><p role="status">Loading environment viewer…</p></div>}><EnvironmentWorkspace key={exploring} definition={environments[exploring]} headingRef={heading} onBack={() => navigate(null)} /></Suspense></EnvironmentBoundary> : active ? (
            <>
              <button className="text-button back-button" onClick={() => navigate(null)}>← Back to Dashboard</button>
              <p className="eyebrow">Workspace / Preview only</p>
              <h1 ref={heading} tabIndex={-1}>{active.name}</h1>
              {active.description && <p className="intro-description">{active.description}</p>}
              <section className="workspace-placeholder" aria-labelledby="placeholder-title">
                <span className="space-symbol" aria-hidden="true">⌑</span>
                <p className="eyebrow">Placeholder workspace</p>
                <h2 id="placeholder-title">A space for what comes next.</h2>
                <p>This project is saved. Workspace tools and editing are not available in this iteration.</p>
                <button className="secondary-button" onClick={() => navigate(null)}>Return to Dashboard</button>
              </section>
            </>
          ) : <Dashboard projects={projects} headingRef={heading} onExplore={(environment = 'cockpit') => setExploring(environment)} onCreate={() => setDialog({ type: 'create' })} onOpen={(project) => navigate(project.id)} onRename={(project) => setDialog({ type: 'rename', project })} onDelete={(project) => setDialog({ type: 'delete', project })} />}
          {error && <p className="error-message" role="alert">{error}</p>}
          <p className="sr-only" role="status">{notice}</p>
        </main>
      </div>
      {dialog && <ProjectDialog key={dialog.type === 'create' ? 'create' : `${dialog.type}-${dialog.project.id}`} mode={dialog.type} project={'project' in dialog ? dialog.project : undefined} onSubmit={submit} onClose={() => setDialog(null)} />}
    </>
  )
}

export default App
