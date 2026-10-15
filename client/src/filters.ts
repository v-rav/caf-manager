import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

/** Filter values kept in the URL query string (bookmarkable, survive back/forward). Empty = not set. */
export function useUrlFilters<K extends string>(keys: readonly K[]) {
  const [params, setParams] = useSearchParams()
  const f = useMemo(() => Object.fromEntries(keys.map((k) => [k, params.get(k) ?? ''])) as Record<K, string>,
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [params, keys.join('|')])
  const set = useCallback((k: K) => (v: string) => setParams((p) => {
    const next = new URLSearchParams(p)
    if (v) next.set(k, v); else next.delete(k)
    return next
  }, { replace: true }), [setParams])
  const reset = useCallback(() => setParams((p) => {
    const next = new URLSearchParams(p)
    keys.forEach((k) => next.delete(k))
    return next
  }, { replace: true }),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  [setParams, keys.join('|')])
  const any = keys.some((k) => !!f[k])
  return { f, set, reset, any }
}

/** Client-side faceted filtering: rows matching every active filter, and per-filter options limited to values that
 *  still exist under the other filters. `order` fixes the option order for a dimension (e.g. SLA tiers). */
export function useFacets<T, K extends string>(
  rows: T[],
  pickers: Record<K, (row: T) => string>,
  f: Record<K, string>,
  order: Partial<Record<K, string[]>> = {},
) {
  const keys = Object.keys(pickers) as K[]
  const match = useCallback((r: T, skip?: K) =>
    keys.every((k) => k === skip || !f[k] || pickers[k](r).toLowerCase() === f[k].toLowerCase()),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [f])
  const filtered = useMemo(() => rows.filter((r) => match(r)), [rows, match])
  const options = useMemo(() => {
    const out = {} as Record<K, string[]>
    for (const k of keys) {
      const vals = new Set(rows.filter((r) => match(r, k)).map(pickers[k]).filter(Boolean))
      const fixed = order[k]
      out[k] = fixed ? fixed.filter((v) => vals.has(v)) : [...vals].sort((a, b) => a.localeCompare(b))
    }
    return out
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, match])
  return { rows: filtered, options }
}
