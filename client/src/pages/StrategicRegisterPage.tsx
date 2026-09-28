import { Badge, Link } from '@fluentui/react-components'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import type { Nomination } from '../types'

const SETTLED = ['Completed', 'Closed', 'Customer Deferred', 'Withdrawn']

function tierTone(t: string): 'success' | 'warning' | 'danger' | 'brand' {
  return t === 'Exec' || t === 'Red' ? 'danger' : t === 'Amber' ? 'warning' : t === 'Green' ? 'success' : 'brand'
}
function velocityTone(v?: string): 'informative' | 'warning' | 'danger' {
  return v === 'Critical' ? 'danger' : v === 'High' ? 'warning' : 'informative'
}

export function StrategicRegisterPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const navigate = useNavigate()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])

  const rows = useMemo(() => (data ?? []).filter((n) => n.isStrategic && nominationInFy(n, fy)), [data, fy])
  const activeAll = useMemo(() => (data ?? []).filter((n) => nominationInFy(n, fy) && !SETTLED.includes(n.status)), [data, fy])

  const kpis = useMemo(() => {
    const active = rows.filter((n) => !SETTLED.includes(n.status))
    const tier = (t: string) => active.filter((n) => n.strategicTier === t).length
    const pct = activeAll.length ? Math.round((active.length / activeAll.length) * 100) : 0
    return { total: rows.length, active: active.length, pct, green: tier('Green'), amber: tier('Amber'), red: tier('Red'), exec: tier('Exec') }
  }, [rows, activeAll])

  const byClass = useMemo(() => {
    const m = new Map<string, number>()
    for (const n of rows) m.set(n.classification, (m.get(n.classification) ?? 0) + 1)
    return [...m.entries()].sort((a, b) => b[1] - a[1])
  }, [rows])

  if (loading && !data) return <Loading label="Loading strategic register…" />
  if (error) return <ErrorText error={error} onRetry={reload} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <div style={{ fontSize: 22, fontWeight: 700 }}>Strategic Investment Register</div>
        <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>Pilots &amp; strategic engagements · time-threshold governance (60/90/120) · excluded from Standard velocity</div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12 }}>
        <KpiCard label="Strategic (active)" value={kpis.active} tone="brand" />
        <KpiCard label="% of active pipeline" value={`${kpis.pct}%`} tone={kpis.pct >= 30 ? 'warning' : 'neutral'} />
        <KpiCard label="Amber (61–90d)" value={kpis.amber} tone={kpis.amber ? 'warning' : 'neutral'} />
        <KpiCard label="Red (91–120d)" value={kpis.red} tone={kpis.red ? 'danger' : 'neutral'} />
        <KpiCard label="Exec (>120d)" value={kpis.exec} tone={kpis.exec ? 'danger' : 'neutral'} />
      </div>

      {byClass.length > 0 && (
        <Panel title="By classification">
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {byClass.map(([c, n]) => (
              <Badge key={c} appearance="tint" color="brand">{c} · {n}</Badge>
            ))}
          </div>
        </Panel>
      )}

      <Panel title={`Strategic engagements${rows.length ? ` · ${rows.length}` : ''}`}>
        <DataTable<Nomination>
          ariaLabel="Strategic engagements"
          rows={rows}
          rowKey={(n) => n.id}
          defaultSort={{ key: 'days', dir: 'desc' }}
          emptyMessage="No strategic-classified nominations. Set a classification in the Nominations grid."
          columns={[
            { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => <Link onClick={() => navigate(`/nominations/${n.id}`)}>{n.accountName ?? `Nomination ${n.id}`}</Link> },
            { key: 'class', header: 'Classification', sortValue: (n) => n.classification },
            { key: 'velocity', header: 'Velocity', sortValue: (n) => n.velocityImpact ?? '', render: (n) => (n.velocityImpact ? <Badge appearance="tint" color={velocityTone(n.velocityImpact)}>{n.velocityImpact}</Badge> : '—') },
            { key: 'sa', header: 'SA', sortValue: (n) => n.solutionArchitect ?? '', render: (n) => n.solutionArchitect ?? '—' },
            { key: 'status', header: 'Status', sortValue: (n) => n.status },
            { key: 'days', header: 'Days in flight', align: 'end', sortValue: (n) => n.daysInFlight, render: (n) => `${n.daysInFlight}d` },
            { key: 'tier', header: 'Threshold', sortValue: (n) => n.strategicTier, render: (n) => (n.strategicTier ? <Badge appearance="filled" color={tierTone(n.strategicTier)}>{n.strategicTier}</Badge> : '—') },
          ]}
        />
      </Panel>
    </div>
  )
}
