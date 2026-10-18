import { useState } from 'react'
import type { Project } from './types'

export const STORAGE_KEY = 'workplace-studio.projects.v1'

function isProject(value: unknown): value is Project {
  if (typeof value !== 'object' || value === null) return false
  const item = value as Record<string, unknown>
  return typeof item.id === 'string' && item.id.length > 0 &&
    typeof item.name === 'string' && item.name.trim().length > 0 && item.name.length <= 100 &&
    typeof item.description === 'string' && item.description.length <= 500 &&
    typeof item.createdAt === 'string' && Number.isFinite(Date.parse(item.createdAt)) &&
    typeof item.updatedAt === 'string' && Number.isFinite(Date.parse(item.updatedAt))
}

function load(): { projects: Project[]; error: string; writable: boolean } {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw === null) return { projects: [], error: '', writable: true }
    const data: unknown = JSON.parse(raw)
    if (!Array.isArray(data) || !data.every(isProject) || new Set(data.map((item) => item.id)).size !== data.length) throw new Error('Invalid data')
    return { projects: data, error: '', writable: true }
  } catch {
    return { projects: [], error: 'Saved projects could not be read. Existing storage has been preserved. Check browser storage permissions or recover the data before reloading.', writable: false }
  }
}

export function useProjects() {
  const [initial] = useState(load)
  const [projects, setProjects] = useState(initial.projects)
  const [error, setError] = useState(initial.error)
  function save(next: Project[]): boolean {
    if (!initial.writable) return false
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
      setProjects(next)
      setError('')
      return true
    } catch {
      setError('Changes could not be saved. Browser storage may be full or unavailable. Your previous projects have been kept; please try again.')
      return false
    }
  }
  return { projects, error, save }
}
