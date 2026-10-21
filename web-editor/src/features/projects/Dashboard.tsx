import type { RefObject } from 'react'
import type { Project } from './types'
import type { EnvironmentKey } from '../environments/registry'
import { EnvironmentGallery } from './EnvironmentGallery'
import '../../styles/dashboard.css'

interface DashboardProps {
  projects: Project[]
  headingRef: RefObject<HTMLHeadingElement | null>
  onCreate: () => void
  onExplore: (environment?: EnvironmentKey) => void
  onOpen: (project: Project) => void
  onRename: (project: Project) => void
  onDelete: (project: Project) => void
}

const dateFormatter = new Intl.DateTimeFormat('en', { day: 'numeric', month: 'short', year: 'numeric' })

type ProjectActions = Pick<DashboardProps, 'onOpen' | 'onRename' | 'onDelete'>

function DashboardProjectCard({ project, onOpen, onRename, onDelete }: ProjectActions & { project: Project }) {
  return <article className="home-project-card">
    <div className="home-project-top"><span className="home-label">Project record</span></div>
    <h3><button className="home-project-title" onClick={() => onOpen(project)} aria-label={`Open ${project.name}`}>{project.name}</button></h3>
    <p className="home-project-description">{project.description || 'No description added.'}</p>
    <div className="home-project-bottom"><time dateTime={project.updatedAt}>Edited {dateFormatter.format(new Date(project.updatedAt))}</time><div>
      <button onClick={() => onRename(project)} aria-label={`Rename ${project.name}`}>Rename</button>
      <button className="home-project-delete" onClick={() => onDelete(project)} aria-label={`Delete ${project.name}`}>Delete</button>
    </div></div>
  </article>
}

export function Dashboard({ projects, headingRef, onCreate, onExplore, onOpen, onRename, onDelete }: DashboardProps) {
  const recent = [...projects].sort((a, b) => Date.parse(b.updatedAt) - Date.parse(a.updatedAt))
  return <div className="home-dashboard" id="dashboard">
    <header className="home-header">
      <a className="home-brand" href="#dashboard"><span className="home-brand-mark" aria-hidden="true">w.</span><span>Workplace <strong>Studio</strong></span></a>
      <nav className="home-nav" aria-label="Workspace navigation">
        <a href="#environments">Environments</a>
        <a href="#project-directory">Project records</a>
      </nav>
      <span className="home-local">Local workspace <span>Saved in this browser</span></span>
    </header>
    <div className="home-content">
      <section id="environments" className="home-environments" aria-labelledby="dashboard-heading">
        <div className="home-environments-heading"><div><h1 id="dashboard-heading" ref={headingRef} tabIndex={-1}>Training environments</h1><p>Open a workplace to prepare views and equipment hotspots.</p></div><span className="home-workflow">Inspect <span aria-hidden="true">/</span> Author <span aria-hidden="true">/</span> Preview</span></div>
        <EnvironmentGallery onExplore={onExplore} />
      </section>
      <section id="project-directory" className="home-projects" aria-labelledby="projects-heading" tabIndex={-1}>
        <div className="home-projects-heading"><div><h2 id="projects-heading">Project records <span>{projects.length}</span></h2><p>Names and notes, separate from environment training. Project workspaces are not available yet.</p></div><button className="secondary-button" onClick={onCreate}>Create project</button></div>
        {recent.length ? <ul className="home-project-grid">{recent.map(project => <li key={project.id}><DashboardProjectCard project={project} onOpen={onOpen} onRename={onRename} onDelete={onDelete} /></li>)}</ul> : <div className="home-project-empty"><div><h3>No project records yet</h3><p>Create a record to keep a project name and notes. To author training, open an environment above.</p></div></div>}
      </section>
    </div>
    <footer className="home-footer"><span>Workplace Studio · Training authoring</span><span>Local records · No cloud sync</span></footer>
  </div>
}
