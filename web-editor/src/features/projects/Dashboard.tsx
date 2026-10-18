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
    <div className="home-project-top"><span className="home-project-symbol" aria-hidden="true">▤</span><span className="home-label">Project</span></div>
    <h3><button className="home-project-title" onClick={() => onOpen(project)} aria-label={`Open ${project.name}`}>{project.name}</button></h3>
    <p className="home-project-description">{project.description || 'A space for your next idea.'}</p>
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
        <a href="#dashboard" aria-current="page">Dashboard</a>
        <a href="#environments">Environments</a>
        <a href="#project-directory">Projects</a>
      </nav>
      <span className="home-local"><i aria-hidden="true" /> Local workspace</span>
    </header>
    <div className="home-content">
      <section id="environments" className="home-environments" aria-labelledby="dashboard-heading">
        <div className="home-environments-heading"><p className="home-label">WORKSPACE / OVERVIEW</p><h1 id="dashboard-heading" ref={headingRef} tabIndex={-1}>Your Environments</h1><p>Choose a space. <em>Find a new perspective.</em></p></div>
        <EnvironmentGallery onExplore={onExplore} />
      </section>
      <section id="project-directory" className="home-projects" aria-labelledby="projects-heading" tabIndex={-1}>
        <div className="home-projects-heading"><div><h2 id="projects-heading">Your Projects <span>{projects.length}</span></h2><p>Saved in this browser. Independent from the environments above.</p></div><button className="primary-button" onClick={onCreate}><span aria-hidden="true">＋</span> Create Project</button></div>
        {recent.length ? <ul className="home-project-grid">{recent.map(project => <li key={project.id}><DashboardProjectCard project={project} onOpen={onOpen} onRename={onRename} onDelete={onDelete} /></li>)}</ul> : <div className="home-project-empty"><span aria-hidden="true">＋</span><div><h3>A place for your next idea.</h3><p>No projects yet. Create a project with a name and description to get started.</p></div><span className="home-empty-note">Your work, saved locally.</span></div>}
      </section>
    </div>
    <footer className="home-footer"><span>Workplace Studio <span aria-hidden="true">/</span> Local prototype</span><span>Explore a space. Make it your own.</span></footer>
  </div>
}
