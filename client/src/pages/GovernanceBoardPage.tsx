import { Badge, Button, Link } from '@fluentui/react-components'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, Loading, Panel } from '../components/common'
import { useRegion } from '../region'
import type { Blocker } from '../types'

function agingColor(d: number): 'informative' | 'warning' | 'danger' {
  return d >= 10 ? 'danger' : d >= 5 ? 'warning' : 'informative'
}

export function GovernanceBoardPage() {
  const { region } = useRegion()
  const navigate = useNavigate()
  const [rows, setRows] = useState<Blocker[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoading(true); setError(null)
    try {
      setRows(await api.openBlockers(region))
    } catch {
      setError('Could not load the governance board.')
    } finally {
      setLoading(false)
    }
  }, [region])
  useEffect(() => { void load() }, [load])

  const kpis = useMemo(() => {
    const open = rows.length
    const clock = rows.filter((b) => b.clockStopped).length
    const avg = open ? Math.round(rows.reduce((s, b) => s + b.daysBlocked, 0) / open) : 0
    const cats = new Set(rows.map((b) => b.category)).size
    return { open, clock, avg, cats }
  }, [rows])

  const byCategory = useMemo(() => {
    const m = new Map<string, number>()
    for (const b of rows) m.set(b.category, (m.get(b.category) ?? 0) + 1)
    return [...m.entries()].sort((a, b) => b[1] - a[1])
  }, [rows])

  const resolve = async (b: Blocker) => {
    if (busy) return
    setBusy(true)
    try { await api.resolveBlocker(b.nominationId, b.id); await load() } finally { setBusy(false) }
  }

  if (loading && rows.length === 0) return <Loading label="Loading governance board…" />
  if (error) return <ErrorText error={error} onRetry={load} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <div style={{ fontSize: 22, fontWeight: 700 }}>Governance Board</div>
        <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>Open blockers across the migration factory · resolve to release the clock</div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
        <KpiCard label="Open blockers" value={kpis.open} tone={kpis.open ? 'warning' : 'success'} />
        <KpiCard label="Clock stopped" value={kpis.clock} tone={kpis.clock ? 'warning' : 'neutral'} />
        <KpiCard label="Avg days blocked" value={`${kpis.avg}d`} tone={kpis.avg >= 10 ? 'danger' : kpis.avg >= 5 ? 'warning' : 'neutral'} />
        <KpiCard label="Categories" value={kpis.cats} tone="neutral" />
      </div>

      {byCategory.length > 0 && (
        <Panel title="By category">
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {byCategory.map(([c, n]) => (
              <Badge key={c} appearance="tint" color="informative">{c} · {n}</Badge>
            ))}
          </div>
        </Panel>
      )}

      <Panel title={`Open blockers${kpis.open ? ` · ${kpis.open}` : ''}`}>
        <DataTable<Blocker>
          ariaLabel="Open blockers"
          rows={rows}
          rowKey={(b) => b.id}
          defaultSort={{ key: 'days', dir: 'desc' }}
          emptyMessage="No open blockers. The factory is flowing."
          columns={[
            { key: 'account', header: 'Account', sortValue: (b) => b.account ?? '', render: (b) => <Link onClick={() => navigate(`/nominations/${b.nominationId}`)}>{b.account ?? `Nomination ${b.nominationId}`}</Link> },
            { key: 'category', header: 'Category', sortValue: (b) => b.category },
            { key: 'owner', header: 'Owner', sortValue: (b) => b.owner ?? '', render: (b) => b.owner ?? '—' },
            { key: 'days', header: 'Days', align: 'end', sortValue: (b) => b.daysBlocked, render: (b) => <Badge appearance="tint" color={agingColor(b.daysBlocked)}>{b.daysBlocked}d</Badge> },
            { key: 'clock', header: 'Clock', sortValue: (b) => (b.clockStopped ? 1 : 0), render: (b) => (b.clockStopped ? <Badge appearance="tint" color="warning">stopped</Badge> : <Badge appearance="tint" color="success">running</Badge>) },
            { key: 'eta', header: 'ETA', sortValue: (b) => b.expectedResolutionUtc ?? '', render: (b) => (b.expectedResolutionUtc ? b.expectedResolutionUtc.slice(0, 10) : '—') },
            { key: 'raised', header: 'Raised by', sortValue: (b) => b.raisedBy ?? '', render: (b) => b.raisedBy ?? '—' },
            { key: 'action', header: '', sortValue: () => '', render: (b) => <Button size="small" disabled={busy} onClick={() => void resolve(b)}>Resolve</Button> },
          ]}
        />
      </Panel>
    </div>
  )
}
