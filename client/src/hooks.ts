import { useCallback, useEffect, useState } from 'react'

/** Debounce a fast-changing value (e.g. a search box) before it drives a request. */
export function useDebounced<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(id)
  }, [value, delay])
  return debounced
}

interface AsyncState<T> {
  data?: T
  loading: boolean
  error?: string
  reload: () => void
}

/** Minimal data-fetch hook with loading/error state and manual reload. */
export function useAsync<T>(factory: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [data, setData] = useState<T>()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>()
  const [tick, setTick] = useState(0)

  const reload = useCallback(() => setTick((t) => t + 1), [])

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(undefined)
    factory()
      .then((result) => {
        if (active) setData(result)
      })
      .catch((err) => {
        if (active) setError(err?.message ?? 'Request failed')
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick])

  return { data, loading, error, reload }
}
