import { Component, type ReactNode } from 'react'

export class EnvironmentBoundary extends Component<{ children: ReactNode; onBack: () => void }, { failed: boolean }> {
  state = { failed: false }

  static getDerivedStateFromError() { return { failed: true } }

  render() {
    if (this.state.failed) return (
      <section>
        <button className="text-button back-button" onClick={this.props.onBack}>← Back to Dashboard</button>
        <p className="error-message" role="alert">The 3D viewer could not start. Reload the page to retry. If dependencies were just updated, wait for Vite to finish preparing them.</p>
        <button className="secondary-button" onClick={() => window.location.reload()}>Reload page</button>
      </section>
    )
    return this.props.children
  }
}
