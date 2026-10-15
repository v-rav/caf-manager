import { Badge, Button, Link, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { useCallback, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { downloadCsv } from '../export'
import type { Nomination } from '../types'


function tierTone(t: string): 'success' | 'warning' | 'danger' | 'brand' {
  return t === 'Exec' || t === 'Red' ? 'danger' : t === 'Amber' ? 'warning' : t === 'Green' ? 'success' : 'brand'
}

const TIERS = ['Green', 'Amber', 'Red', 'Exec']
type Dim = 'tier' | 'cls' | 'sa' | 'status'
const EMPTY: Record<Dim, string> = { tier: '', cls: '', sa: '', status: '' }
const pickers: Record<Dim, (n: Nomination) => string> = {
  tier: (n) => n.strategicTier ?? '',
  cls: (n) => n.classification ?? '',
  sa: (n) => n.solutionArchitect?.trim() ?? '',
  status: (n) => n.status ?? '',
}

export function StrategicRegisterPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const navigate = useNavigate()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const { data: vocab } = useAsync(() => api.nominationVocab(), [])
  const [f, setF] = useState<Record<Dim, string>>(EMPTY)
  const setDim = (d: Dim) => (v: string) => setF((s) => ({ ...s, [d]: v }))

  const strategic = useMemo(() => (data ?? []).filter((n) => n.isStrategic && nominationInFy(n, fy)), [data, fy])
  const match = useCallback((n: Nomination, skip?: Dim) =>
    (Object.keys(f) as Dim[]).every((d) => d === skip || !f[d] || pickers[d](n).toLowerCase() === f[d].toLowerCase()), [f])
  const rows = useMemo(() => strategic.filter((n) => match(n)), [strategic, match])
  const options = useMemo(() => {
    const out = {} as Record<Dim, string[]>
    for (const d of Object.keys(f) as Dim[]) {
      const vals = new Set(strategic.filter((n) => match(n, d)).map(pickers[d]).filter(Boolean))
      out[d] = d === 'tier' ? TIERS.filter((t) => vals.has(t)) : [...vals].sort((a, b) => a.localeCompare(b))
    }
    return out
  }, [strategic, match, f])
  const activeAll = useMemo(() => (data ?? []).filter((n) => nominationInFy(n, fy) && !n.isSettled), [data, fy])

  const exportRows = () =>
    downloadCsv<Nomination>(
      `strategic-register-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: 'Account', value: (n) => n.accountName ?? '' },
        { header: 'TPID', value: (n) => n.tpid ?? '' },
        { header: 'Classification', value: (n) => n.classification },
        { header: 'Velocity % (target ÷ actual)', value: (n) => (n.velocity != null ? Math.round(n.velocity * 100) : '') },
        { header: 'SA', value: (n) => n.solutionArchitect ?? '' },
        { header: 'Status', value: (n) => n.status },
        { header: 'Days in flight', value: (n) => n.daysInFlight },
        { header: 'Threshold', value: (n) => n.strategicTier },
        { header: 'Region', value: (n) => n.region },
      ],
      rows,
    )

  const kpis = useMemo(() => {
    const active = rows.filter((n) => !n.isSettled)
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
  if (vocab && !vocab.classificationEnabled)
    return (
      <Panel title="Strategic Investment Register — parked">
        <div style={{ fontSize: 13, lineHeight: 1.6 }}>
          Classification is currently <b>off</b>, so every nomination counts as <b>Standard Factory</b> and nothing is
          excluded from SLA or velocity. An Admin can turn it on in <b>Configuration → Operational thresholds →
          Features → Classification &amp; Strategic Register</b> (1 = on). See Help → Classification for what it does.
        </div>
      </Panel>
    )

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ fontSize: 22, fontWeight: 700 }}>Strategic Investment Register</div>
          <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>Pilots &amp; strategic engagements · time-threshold governance (60/90/120) · excluded from Standard velocity</div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportRows} disabled={!rows.length}>Export</Button>
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <FilterSelect label="Classification" value={f.cls} options={options.cls} onChange={setDim('cls')} minWidth={180} />
        <FilterSelect label="Threshold" value={f.tier} options={options.tier} onChange={setDim('tier')} minWidth={120} />
        <FilterSelect label="Status" value={f.status} options={options.status} onChange={setDim('status')} minWidth={140} />
        <FilterSelect label="SA" value={f.sa} options={options.sa} onChange={setDim('sa')} minWidth={180} />
        {Object.values(f).some(Boolean) && <Button size="small" onClick={() => setF(EMPTY)}>Reset</Button>}
      </div>

      {strategic.length === 0 && (
        <Panel title="No strategic engagements yet">
          <div style={{ fontSize: 13, lineHeight: 1.6 }}>
            Nothing in this scope is classified as non-Standard. To track a pilot here, open the nomination → <b>Manage</b> and set
            <b> Classification</b> (Strategic Pilot, Lighthouse Engagement, Innovation / POC, Recovery Engagement). It then leaves
            Standard SLA/velocity and is governed here by days in flight.
          </div>
        </Panel>
      )}

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
          emptyMessage="No strategic nominations match these filters."
          columns={[
            { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => <Link onClick={() => navigate(`/nominations/${n.id}`)}>{n.accountName ?? `Nomination ${n.id}`}</Link> },
            { key: 'class', header: 'Classification', sortValue: (n) => n.classification },
            { key: 'velocity', header: 'Velocity', align: 'center', sortValue: (n) => n.velocity ?? -1, render: (n) => (n.velocity != null ? <Tooltip relationship="description" content={`${n.velocityTargetDays} target ÷ ${n.velocityDays} actual business days (FDO) · 100% = on target, higher = faster`}><Badge appearance="tint" color={n.velocityTone ?? 'informative'}>{Math.round(n.velocity * 100)}%</Badge></Tooltip> : '—') },
            { key: 'sa', header: 'SA', sortValue: (n) => n.solutionArchitect ?? '', render: (n) => n.solutionArchitect ?? '—' },
            { key: 'status', header: 'Status', sortValue: (n) => n.status },
            { key: 'days', header: 'Days in flight', align: 'end', sortValue: (n) => n.daysInFlight, render: (n) => <Tooltip relationship="description" content={n.daysInFlightSource === 'FDO Total Days' ? 'FDO Total Days (business days)' : 'From dates: actual start → actual end (or today), calendar days'}><span>{n.daysInFlight}d</span></Tooltip> },
            { key: 'tier', header: 'Threshold', sortValue: (n) => n.strategicTier, render: (n) => (n.strategicTier ? <Badge appearance="filled" color={tierTone(n.strategicTier)}>{n.strategicTier}</Badge> : '—') },
          ]}
        />
      </Panel>
    </div>
  )
}
