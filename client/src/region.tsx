import { createContext, useContext, useState, type ReactNode } from 'react'

interface RegionState {
  region: string | undefined
  setRegion: (r: string | undefined) => void
}

const RegionContext = createContext<RegionState>({ region: undefined, setRegion: () => {} })

/** Holds the currently selected region so every view shares one Global/Regional scope. */
export function RegionProvider({ children }: { children: ReactNode }) {
  const [region, setRegion] = useState<string | undefined>(undefined)
  return <RegionContext.Provider value={{ region, setRegion }}>{children}</RegionContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useRegion() {
  return useContext(RegionContext)
}
