import { Badge } from '@fluentui/react-components'
import { useMemo } from 'react'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { ADOPTION_LEVELS } from '../adoption'
import type { Nomination } from '../types'

const SETTLED = ['Withdrawn']

function Bar({ label, count, max }: { label: string; count: number; max: number }) {
  const pct = max ? Math.round((count / max) * 100) : 0
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '210px 1fr 40px', gap: 10, alignItems: 'center' }}>
      <span style={{ fontSize: 12 }}>{label}</span>
      <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 4, height: 14, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: 'var(--colorBrandBackground)' }} />
      </div>
      <span style={{ fontSize: 12, textAlign: 'right', color: 'var(--colorNeutralForeground3)' }}>{count}</span>
    </div>
  )
}

export function AdoptionPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])

  const rows = useMemo(() => (data ?? []).filter((n) => nominationInFy(n, fy) && !SETTLED.includes(n.status)), [data, fy])

  const kpis = useMemo(() => {
    const total = rows.length
    const lvl = (n: Nomination) => n.ghcpAdoptionLevel ?? 0
    const licensed = rows.filter((n) => lvl(n) >= 4).length
    const awaiting = rows.filter((n) => lvl(n) >= 1 && lvl(n) <= 3).length
    const used = rows.filter((n) => lvl(n) >= 5).length
    const withToolData = rows.filter((n) => n.isToolAttached != null)
    const toolAttached = withToolData.filter((n) => n.isToolAttached).length
    return {
      total,
      licensed,
      awaiting,
      usedPct: total ? Math.round((used / total) * 100) : 0,
      toolPct: withToolData.length ? Math.round((toolAttached / withToolData.length) * 100) : 0,
    }
  }, [rows])

  const dist = useMemo(() => {
    const counts = new Array(8).fill(0)
    for (const n of rows) counts[Math.min(7, Math.max(0, n.ghcpAdoptionLevel ?? 0))]++
    const max = Math.max(1, ...counts)
    return { counts, max }
  }, [rows])

  const byRegion = useMemo(() => {
    const m = new Map<string, { total: number; used: number }>()
    for (const n of rows) {
      const r = n.region || 'Unspecified'
      const e = m.get(r) ?? { total: 0, used: 0 }
      e.total++
      if ((n.ghcpAdoptionLevel ?? 0) >= 5) e.used++
      m.set(r, e)
    }
    return [...m.entries()].sort((a, b) => b[1].total - a[1].total)
  }, [rows])

  if (loading && !data) return <Loading label="Loading adoption…" />
  if (error) return <ErrorText error={error} onRetry={reload} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <div style={{ fontSize: 22, fontWeight: 700 }}>GHCP Adoption</div>
        <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>License readiness · adoption maturity 0–7 · tool usage — across active nominations</div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: 12 }}>
        <KpiCard label="Licensed (L4+)" value={kpis.licensed} tone="success" />
        <KpiCard label="Awaiting license (L1–3)" value={kpis.awaiting} tone={kpis.awaiting ? 'warning' : 'neutral'} />
        <KpiCard label="GHCP-used %" value={`${kpis.usedPct}%`} tone={kpis.usedPct >= 50 ? 'success' : 'brand'} />
        <KpiCard label="Tool-attached %" value={`${kpis.toolPct}%`} tone="brand" />
      </div>

      <Panel title="Adoption-level distribution (0–7)">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {ADOPTION_LEVELS.map((a) => (
            <Bar key={a.level} label={`${a.level} · ${a.label}`} count={dist.counts[a.level]} max={dist.max} />
          ))}
        </div>
      </Panel>

      <Panel title="By region">
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {byRegion.map(([r, e]) => (
            <Badge key={r} appearance="tint" color="informative">
              {r} · {e.total} · used {e.total ? Math.round((e.used / e.total) * 100) : 0}%
            </Badge>
          ))}
        </div>
      </Panel>
    </div>
  )
}
