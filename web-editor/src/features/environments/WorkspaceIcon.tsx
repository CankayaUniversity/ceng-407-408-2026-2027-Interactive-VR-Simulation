const paths = {
  select: 'M5 3v17l5-5 4 6 3-2-4-6 7-1Z',
  hotspot: 'M12 8v8M8 12h8M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
  view: 'M3 5h18v14H3ZM8 3v4M16 3v4M9 12h6M12 9v6',
  move: 'M12 3v18M3 12h18M9 6l3-3 3 3M9 18l3 3 3-3M6 9l-3 3 3 3M18 9l3 3-3 3',
  close: 'm6 6 12 12M6 18 18 6',
  back: 'M20 12H4m6-6-6 6 6 6',
  expand: 'M8 3H3v5M16 3h5v5M3 16v5h5M21 16v5h-5',
} as const

export function WorkspaceIcon({ name }: { name: keyof typeof paths }) {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>
}
