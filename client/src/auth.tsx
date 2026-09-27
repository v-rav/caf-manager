import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from './api'
import type { AppUser } from './types'

interface AuthState {
  user: AppUser | null
  loading: boolean
  refresh: () => Promise<void>
  login: (username: string, password: string) => Promise<AppUser>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthState>(null!)
export const useAuth = () => useContext(AuthContext)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AppUser | null>(null)
  const [loading, setLoading] = useState(true)

  const refresh = async () => {
    try {
      setUser(await api.me())
    } catch {
      setUser(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void refresh()
  }, [])

  const login = async (username: string, password: string) => {
    const u = await api.login(username, password)
    setUser(u)
    return u
  }

  const logout = async () => {
    await api.logout()
    setUser(null)
  }

  return <AuthContext.Provider value={{ user, loading, refresh, login, logout }}>{children}</AuthContext.Provider>
}
