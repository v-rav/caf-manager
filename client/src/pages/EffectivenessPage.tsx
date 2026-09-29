import { Badge, Button, Link, Tab, TabList, Text, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular, ArrowLeftRegular, OpenRegular } from '@fluentui/react-icons'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { DataTable, type Column } from '../components/DataTable'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { downloadCsv } from '../export'
import type { Nomination } from '../types'

type Role = 'PM' | 'SA' | 'CFTL'

// In-flight statuses (not yet settled) — used for the "active" and on-track cuts.
const ACTIVE = new Set(['Open', 'In Progress', 'Blocked', 'Waiting for Customer Action', 'Waiting on Follow-up'])

const ROLE_LABEL: Record<Role, string> = { PM: 'PM (Project Coordinator)', SA: 'SA (Solution Architect)', CFTL: 'CFTL' }

function ownerOf(n: Nomination, role: Role): string | undefined {
  const v = role === 'PM' ? n.projectCoordinator : role === 'SA' ? n.solutionArchitect : n.cftlPrimary
  return (v ?? '').trim() || undefined
}

// Delivery duration for a completed nomination: the offering TotalDays when present,
// else nominated→actual-end. Null when neither is available.
function deliveryDays(n: Nomination): number | null {
  if (n.totalDays && n.totalDays > 0) return n.totalDays
  if (n.nominatedDate && n.actualEndDate) {
    const d = (new Date(n.actualEndDate).getTime() - new Date(n.nominatedDate).getTime()) / 86_400_000
    return d >= 0 ? Math.round(d) : null
  }
  return null
}

type ScoreRow = {
  name: string
  assigned: number
  active: number
  delivered: number
  withdrawn: number
  deferred: number
  blocked: number
  successRate: number | null
  avgDeliveryDays: number | null
  avgMsi: number
  onTrackPct: number | null
  slaBreaches: number
  avgGhcp: number | null
  acrDelivered: number
  effectiveness: number
}

function buildRows(noms: Nomination[], role: Role): ScoreRow[] {
  const groups = new Map<string, Nomination[]>()
  for (const n of noms) {
    const o = ownerOf(n, role)
    if (!o) continue
    let arr = groups.get(o)
    if (!arr) { arr = []; groups.set(o, arr) }
    arr.push(n)
  }
  const rows: ScoreRow[] = []
  for (const [name, list] of groups) {
    const assigned = list.length
    const activeList = list.filter((n) => ACTIVE.has(n.status))
    const active = activeList.length
    const delivered = list.filter((n) => n.status === 'Completed').length
    const withdrawn = list.filter((n) => n.status === 'Withdrawn').length
    const deferred = list.filter((n) => n.status === 'Customer Deferred').length
    const blocked = list.filter((n) => (n.openBlockerCount ?? 0) > 0 || n.status === 'Blocked').length
    const settled = delivered + withdrawn + deferred
    const successRate = settled > 0 ? Math.round((delivered / settled) * 100) : null
    const durs = list.filter((n) => n.status === 'Completed').map(deliveryDays).filter((d): d is number => d != null)
    const avgDeliveryDays = durs.length ? Math.round(durs.reduce((a, b) => a + b, 0) / durs.length) : null
    const avgMsi = Math.round(list.reduce((a, n) => a + (n.msiScore ?? 0), 0) / assigned)
    const onTrackPct = active ? Math.round((activeList.filter((n) => !n.staleTier).length / active) * 100) : null
    const slaBreaches = activeList.filter((n) => !!n.staleTier).length
    const ghcps = list.map((n) => n.ghcpAdoptionLevel).filter((v): v is number => v != null)
    const avgGhcp = ghcps.length ? Math.round((ghcps.reduce((a, b) => a + b, 0) / ghcps.length) * 10) / 10 : null
    const acrDelivered = list.filter((n) => n.status === 'Completed').reduce((a, n) => a + (n.totalAcr ?? 0), 0)

    // Composite effectiveness (0–100): MSI quality always counts; success rate and on-track
    // are added only when there's a track record, and the present weights are renormalised.
    const parts: { w: number; v: number }[] = [{ w: 0.35, v: avgMsi }]
    if (settled > 0) parts.push({ w: 0.4, v: successRate! })
    if (active > 0) parts.push({ w: 0.25, v: onTrackPct! })
    const wsum = parts.reduce((s, p) => s + p.w, 0)
    const effectiveness = Math.round(parts.reduce((s, p) => s + p.w * p.v, 0) / wsum)

    rows.push({ name, assigned, active, delivered, withdrawn, deferred, blocked, successRate, avgDeliveryDays, avgMsi, onTrackPct, slaBreaches, avgGhcp, acrDelivered, effectiveness })
  }
  return rows.sort((a, b) => b.effectiveness - a.effectiveness)
}

const money = (v: number) => (v ? '$' + Math.round(v).toLocaleString() : '—')
const pct = (v: number | null) => (v == null ? '—' : `${v}%`)
const effTone = (v: number): 'success' | 'warning' | 'danger' => (v >= 75 ? 'success' : v >= 55 ? 'warning' : 'danger')

// Migration stage number from the raw status text (matches the grid's 1–4 encoding).
function stageNum(n: Nomination): string {
  const m = (n.migrationStatus ?? '').toLowerCase()
  if (m.includes('executing migration')) return '4'
  if (m.includes('finalize')) return '3'
  if (m.includes('pre-requisite') || m.includes('prerequisite') || m.includes('pre requisite')) return '2'
  if (m.includes('validating')) return '1'
  return '—'
}

const STATUS_TONE: Record<string, 'success' | 'warning' | 'danger' | 'informative'> = {
  Completed: 'success',
  Blocked: 'danger',
  Withdrawn: 'danger',
  'Customer Deferred': 'warning',
  'Waiting for Customer Action': 'warning',
  'Waiting on Follow-up': 'warning',
}

// Compact stat chip for the individual drill-down header.
function Stat({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div style={{ minWidth: 92 }}>
      <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: 0.3, color: 'var(--colorNeutralForeground3)' }}>{label}</div>
      <div style={{ fontSize: 18, fontWeight: 600 }}>{value}</div>
    </div>
  )
}

export function EffectivenessPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const noms = useAsync(() => api.nominations(region), [region])
  const [role, setRole] = useState<Role>('PM')
  const [selected, setSelected] = useState<string | null>(null)
  useEffect(() => setSelected(null), [role]) // a role switch clears the open owner

  // Fiscal-year + region scoped (the global selectors drive this page like the rest of Delivery).
  const scoped = useMemo(() => (noms.data ?? []).filter((n) => nominationInFy(n, fy)), [noms.data, fy])
  const rows = useMemo(() => buildRows(scoped, role), [scoped, role])

  // The selected owner's row + their nominations (in the current role), for the inline drill-down.
  const detail = useMemo(() => {
    if (!selected) return null
    const row = rows.find((r) => r.name === selected)
    if (!row) return null
    const list = scoped
      .filter((n) => ownerOf(n, role) === selected)
      .sort((a, b) => (b.msiScore ?? 0) - (a.msiScore ?? 0))
    return { row, list }
  }, [selected, rows, scoped, role])

  // Pooled totals for the KPI band (computed straight from the scoped set for this role).
  const kpis = useMemo(() => {
    const roleNoms = scoped.filter((n) => ownerOf(n, role))
    const delivered = roleNoms.filter((n) => n.status === 'Completed').length
    const withdrawn = roleNoms.filter((n) => n.status === 'Withdrawn').length
    const deferred = roleNoms.filter((n) => n.status === 'Customer Deferred').length
    const active = roleNoms.filter((n) => ACTIVE.has(n.status)).length
    const settled = delivered + withdrawn + deferred
    const success = settled ? Math.round((delivered / settled) * 100) : null
    const avgMsi = roleNoms.length ? Math.round(roleNoms.reduce((a, n) => a + (n.msiScore ?? 0), 0) / roleNoms.length) : 0
    return { owners: rows.length, assigned: roleNoms.length, delivered, active, settledLost: withdrawn + deferred, success, avgMsi }
  }, [scoped, role, rows.length])

  const exportRows = () =>
    downloadCsv<ScoreRow>(
      `effectiveness-${role.toLowerCase()}-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: ROLE_LABEL[role], value: (r) => r.name },
        { header: 'Assigned', value: (r) => r.assigned },
        { header: 'Active', value: (r) => r.active },
        { header: 'Delivered', value: (r) => r.delivered },
        { header: 'Withdrawn', value: (r) => r.withdrawn },
        { header: 'Deferred', value: (r) => r.deferred },
        { header: 'Blocked', value: (r) => r.blocked },
        { header: 'Success %', value: (r) => r.successRate ?? '' },
        { header: 'Avg delivery days', value: (r) => r.avgDeliveryDays ?? '' },
        { header: 'Avg MSI', value: (r) => r.avgMsi },
        { header: 'On-track %', value: (r) => r.onTrackPct ?? '' },
        { header: 'SLA breaches', value: (r) => r.slaBreaches },
        { header: 'Avg GHCP', value: (r) => r.avgGhcp ?? '' },
        { header: 'ACR delivered', value: (r) => Math.round(r.acrDelivered) },
        { header: 'Effectiveness', value: (r) => r.effectiveness },
      ],
      rows,
    )

  if (noms.loading) return <Loading />
  if (noms.error) return <ErrorText error={noms.error} onRetry={noms.reload} />

  const columns: Column<ScoreRow>[] = [
    {
      key: 'name',
      header: ROLE_LABEL[role],
      minWidth: 200,
      sortValue: (r) => r.name,
      render: (r) => (
        <Link as="button" onClick={() => setSelected(r.name)} style={{ textAlign: 'left' }}>{r.name}</Link>
      ),
    },
    { key: 'assigned', header: 'Assigned', align: 'end', sortValue: (r) => r.assigned, render: (r) => r.assigned },
    { key: 'active', header: 'Active', align: 'end', sortValue: (r) => r.active, render: (r) => r.active },
    { key: 'delivered', header: 'Delivered', align: 'end', sortValue: (r) => r.delivered, render: (r) => r.delivered },
    { key: 'withdrawn', header: 'Withdrawn', align: 'end', sortValue: (r) => r.withdrawn, render: (r) => r.withdrawn || '—' },
    { key: 'deferred', header: 'Deferred', align: 'end', sortValue: (r) => r.deferred, render: (r) => r.deferred || '—' },
    { key: 'blocked', header: 'Blocked', align: 'end', sortValue: (r) => r.blocked, render: (r) => r.blocked || '—' },
    {
      key: 'success',
      header: 'Success %',
      align: 'end',
      sortValue: (r) => r.successRate ?? -1,
      render: (r) => pct(r.successRate),
    },
    {
      key: 'days',
      header: 'Avg days',
      align: 'end',
      sortValue: (r) => r.avgDeliveryDays ?? Number.MAX_SAFE_INTEGER,
      render: (r) => r.avgDeliveryDays ?? '—',
    },
    { key: 'msi', header: 'Avg MSI', align: 'end', sortValue: (r) => r.avgMsi, render: (r) => r.avgMsi },
    {
      key: 'ontrack',
      header: 'On-track %',
      align: 'end',
      sortValue: (r) => r.onTrackPct ?? -1,
      render: (r) => pct(r.onTrackPct),
    },
    {
      key: 'sla',
      header: 'SLA breaches',
      align: 'end',
      sortValue: (r) => r.slaBreaches,
      render: (r) => (r.slaBreaches ? <Badge appearance="tint" color="danger">{r.slaBreaches}</Badge> : '—'),
    },
    { key: 'acr', header: 'ACR delivered', align: 'end', sortValue: (r) => r.acrDelivered, render: (r) => money(r.acrDelivered) },
    {
      key: 'eff',
      header: 'Effectiveness',
      align: 'center',
      sortValue: (r) => r.effectiveness,
      render: (r) => (
        <Tooltip
          relationship="description"
          content={`0.35·MSI (${r.avgMsi})${r.successRate != null ? ` + 0.40·Success (${r.successRate}%)` : ''}${r.onTrackPct != null ? ` + 0.25·On-track (${r.onTrackPct}%)` : ''}, weights renormalised`}
        >
          <Badge appearance="filled" color={effTone(r.effectiveness)}>{r.effectiveness}</Badge>
        </Tooltip>
      ),
    },
  ]

  const detailColumns: Column<Nomination>[] = [
    {
      key: 'account',
      header: 'Account',
      minWidth: 200,
      sortValue: (n) => n.accountName ?? '',
      render: (n) => <RouterLink to={`/nominations/${n.id}`}>{n.accountName ?? '—'}{n.shortName ? ` · ${n.shortName}` : ''}</RouterLink>,
    },
    { key: 'tpid', header: 'TPID', sortValue: (n) => n.tpid ?? '', render: (n) => n.tpid ?? '—' },
    { key: 'stage', header: 'Stage', align: 'center', sortValue: (n) => stageNum(n), render: (n) => stageNum(n) },
    {
      key: 'status',
      header: 'Status',
      sortValue: (n) => n.status,
      render: (n) => <Badge appearance="tint" color={STATUS_TONE[n.status] ?? 'informative'}>{n.status}</Badge>,
    },
    { key: 'msi', header: 'MSI', align: 'end', sortValue: (n) => n.msiScore ?? 0, render: (n) => n.msiScore ?? '—' },
    {
      key: 'days',
      header: 'Delivery days',
      align: 'end',
      sortValue: (n) => deliveryDays(n) ?? Number.MAX_SAFE_INTEGER,
      render: (n) => deliveryDays(n) ?? '—',
    },
    { key: 'acr', header: 'ACR', align: 'end', sortValue: (n) => n.totalAcr ?? 0, render: (n) => money(n.totalAcr ?? 0) },
  ]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text size={600} weight="semibold">Ownership Effectiveness</Text>
        <div style={{ color: 'var(--colorNeutralForeground3)', fontSize: 13, marginTop: 2 }}>
          Delivery scorecard for PM / SA / CFTL owners — book of work (assigned), outcomes (delivered · withdrawn ·
          deferred), delivery speed, quality (MSI) and on-track health, rolled into a composite effectiveness score.
        </div>
      </div>

      <TabList selectedValue={role} onTabSelect={(_, d) => setRole(d.value as Role)}>
        <Tab value="PM">PM · Project Coordinator</Tab>
        <Tab value="SA">SA · Solution Architect</Tab>
        <Tab value="CFTL">CFTL</Tab>
      </TabList>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label={`${role} owners`} value={kpis.owners} tone="brand" />
        <KpiCard label="Assigned (book of work)" value={kpis.assigned} />
        <KpiCard label="Delivered" value={kpis.delivered} tone="success" />
        <KpiCard label="Active in-flight" value={kpis.active} />
        <KpiCard label="Deferred + withdrawn" value={kpis.settledLost} tone="warning" />
        <KpiCard label="Success rate" value={kpis.success == null ? '—' : `${kpis.success}%`} tone={kpis.success != null && kpis.success >= 70 ? 'success' : 'warning'} />
        <KpiCard label="Avg MSI" value={kpis.avgMsi} tone={kpis.avgMsi >= 80 ? 'success' : kpis.avgMsi >= 60 ? 'warning' : 'danger'} />
      </div>

      {detail && (
        <Panel
          title={`${selected} · ${role}`}
          action={
            <div style={{ display: 'flex', gap: 8 }}>
              <Button appearance="secondary" icon={<OpenRegular />} as="a" href={`/nominations?person=${encodeURIComponent(selected!)}`}>
                Open in Nominations
              </Button>
              <Button appearance="secondary" icon={<ArrowLeftRegular />} onClick={() => setSelected(null)}>
                Back to all
              </Button>
            </div>
          }
        >
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 18, marginBottom: 14 }}>
            <Stat label="Assigned" value={detail.row.assigned} />
            <Stat label="Active" value={detail.row.active} />
            <Stat label="Delivered" value={detail.row.delivered} />
            <Stat label="Withdrawn" value={detail.row.withdrawn} />
            <Stat label="Deferred" value={detail.row.deferred} />
            <Stat label="Blocked" value={detail.row.blocked} />
            <Stat label="Success %" value={pct(detail.row.successRate)} />
            <Stat label="Avg days" value={detail.row.avgDeliveryDays ?? '—'} />
            <Stat label="Avg MSI" value={detail.row.avgMsi} />
            <Stat label="On-track %" value={pct(detail.row.onTrackPct)} />
            <Stat label="ACR delivered" value={money(detail.row.acrDelivered)} />
            <Stat label="Effectiveness" value={<Badge appearance="filled" color={effTone(detail.row.effectiveness)}>{detail.row.effectiveness}</Badge>} />
          </div>
          <DataTable<Nomination>
            ariaLabel={`${selected} nominations`}
            rows={detail.list}
            rowKey={(n) => n.id}
            columns={detailColumns}
            defaultSort={{ key: 'status', dir: 'asc' }}
            emptyMessage="No nominations in scope."
            pageSize={15}
          />
        </Panel>
      )}

      <Panel
        title={`${role} scorecard`}
        action={
          <Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportRows} disabled={rows.length === 0}>
            Export CSV
          </Button>
        }
      >
        <DataTable<ScoreRow>
          ariaLabel={`${role} effectiveness scorecard`}
          rows={rows}
          rowKey={(r) => r.name}
          columns={columns}
          defaultSort={{ key: 'eff', dir: 'desc' }}
          emptyMessage={`No ${role} owners in the current scope.`}
          pageSize={25}
        />
      </Panel>
    </div>
  )
}
