import { useEffect, type RefObject } from 'react'
import { BabylonViewport } from './BabylonViewport'
import type { SceneDefinition } from './types'
import '../../styles/editor-workspace.css'

interface WorkspaceProps {
  definition: SceneDefinition
  headingRef: RefObject<HTMLHeadingElement | null>
  onBack: () => void
}

export default function EnvironmentWorkspace({ definition, headingRef, onBack }: WorkspaceProps) {
  useEffect(() => { headingRef.current?.focus() }, [headingRef])
  return (
    <BabylonViewport definition={definition} headingRef={headingRef} onBack={onBack} />
  )
}
