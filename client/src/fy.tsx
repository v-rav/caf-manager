import { createContext, useContext, useState, type ReactNode } from 'react'
import type { Nomination } from './types'

export type Fy = number | 'all'

// Fiscal year starts Jul 1, labelled by its end year (Jul 2026–Jun 2027 = FY27).
export function currentFy(): number {
  const d = new Date()
  return d.getMonth() + 1 >= 7 ? d.getFullYear() + 1 : d.getFullYear()
}

export function fiscalYearOf(dateStr?: string | null): number | null {
  if (!dateStr) return null
  const d = new Date(dateStr)
  if (Number.isNaN(d.getTime())) return null
  return d.getMonth() + 1 >= 7 ? d.getFullYear() + 1 : d.getFullYear()
}

export function fyLabel(fy: number): string {
  return `FY${String(fy % 100).padStart(2, '0')}`
}

const SETTLED = ['Completed', 'Closed', 'Customer Deferred', 'Withdrawn']

/** Current-FY scope: active/in-flight work always shows; settled items only if they closed in the selected FY. */
export function nominationInFy(n: Nomination, fy: Fy): boolean {
  if (fy === 'all') return true
  if (!SETTLED.includes(n.status)) return true
  const closeFy = fiscalYearOf(n.actualEndDate) ?? fiscalYearOf(n.nominatedDate) ?? fiscalYearOf(n.openedDate)
  return closeFy === fy
}

interface FyState {
  fy: Fy
  setFy: (fy: Fy) => void
}

const FyContext = createContext<FyState>({ fy: currentFy(), setFy: () => {} })

/** Shared fiscal-year scope, defaulting to the current FY so views open on this year's work. */
export function FyProvider({ children }: { children: ReactNode }) {
  const [fy, setFy] = useState<Fy>(() => currentFy())
  return <FyContext.Provider value={{ fy, setFy }}>{children}</FyContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useFy() {
  return useContext(FyContext)
}
