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
import type { EffectivenessRow as ScoreRow, Nomination } from '../types'

type Role = 'PM' | 'SA' | 'CFTL'

const ROLE_LABEL: Record<Role, string> = { PM: 'PM (Project Coordinator)', SA: 'SA (Solution Architect)', CFTL: 'CFTL' }

function ownerOf(n: Nomination, role: Role): string | undefined {
  const v = role === 'PM' ? n.projectCoordinator : role === 'SA' ? n.solutionArchitect : n.cftlPrimary
  return (v ?? '').trim() || undefined
}

const money = (v: number) => (v ? '$' + Math.round(v).toLocaleString() : '—')
const pct = (v: number | null) => (v == null ? '—' : `${v}%`)

const stageNum = (n: Nomination): string => (n.stageIndex ? String(n.stageIndex) : '—')

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
  // Scores are computed on the server (Scoring models settings); the page only presents them.
  const eff = useAsync(() => api.effectiveness(region, role, fy === 'all' ? undefined : fy), [region, role, fy])
  const [selected, setSelected] = useState<string | null>(null)
  useEffect(() => setSelected(null), [role]) // a role switch clears the open owner

  // Drill-down list scope mirrors the server (FY + region, synthetic "Closed" excluded).
  const scoped = useMemo(
    () => (noms.data ?? []).filter((n) => nominationInFy(n, fy) && n.status !== 'Closed'),
    [noms.data, fy],
  )
  const rows = useMemo(() => eff.data?.rows ?? [], [eff.data])

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

  const kpis = {
    owners: eff.data?.owners ?? 0,
    assigned: eff.data?.assigned ?? 0,
    delivered: eff.data?.delivered ?? 0,
    active: eff.data?.active ?? 0,
    deliveredPct: eff.data?.deliveredPct ?? 0,
    avgDays: eff.data?.avgDeliveryDays ?? null,
    avgAge: eff.data?.avgActiveAge ?? null,
  }

  const exportRows = () =>
    downloadCsv<ScoreRow>(
      `effectiveness-${role.toLowerCase()}-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: ROLE_LABEL[role], value: (r) => r.name },
        { header: 'Assigned', value: (r) => r.assigned },
        { header: 'Active', value: (r) => r.active },
        { header: 'Delivered', value: (r) => r.delivered },
        { header: 'Delivered %', value: (r) => r.deliveredPct },
        { header: 'Withdrawn', value: (r) => r.withdrawn },
        { header: 'Deferred', value: (r) => r.deferred },
        { header: 'Avg delivery days', value: (r) => r.avgDeliveryDays ?? '' },
        { header: 'Avg age (active)', value: (r) => r.avgActiveAge ?? '' },
        { header: 'On-track %', value: (r) => r.onTrackPct ?? '' },
        { header: 'SLA breaches', value: (r) => r.slaBreaches },
        { header: 'Avg MSI', value: (r) => r.avgMsi },
        { header: 'Avg GHCP', value: (r) => r.avgGhcp ?? '' },
        { header: 'ACR delivered', value: (r) => Math.round(r.acrDelivered) },
        { header: 'Effectiveness', value: (r) => r.effectiveness },
      ],
      rows,
    )

  if ((noms.loading && !noms.data) || (eff.loading && !eff.data)) return <Loading />
  if (noms.error) return <ErrorText error={noms.error} onRetry={noms.reload} />
  if (eff.error) return <ErrorText error={eff.error} onRetry={eff.reload} />

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
    { key: 'deliveredPct', header: 'Delivered %', align: 'end', sortValue: (r) => r.deliveredPct, render: (r) => `${r.deliveredPct}%` },
    {
      key: 'days',
      header: 'Avg days',
      align: 'end',
      sortValue: (r) => r.avgDeliveryDays ?? Number.MAX_SAFE_INTEGER,
      render: (r) => r.avgDeliveryDays ?? '—',
    },
    {
      key: 'age',
      header: 'Avg age',
      align: 'end',
      sortValue: (r) => r.avgActiveAge ?? Number.MAX_SAFE_INTEGER,
      render: (r) => (r.avgActiveAge == null ? '—' : <span style={{ color: r.avgActiveAge > 45 ? 'var(--colorPaletteDarkOrangeForeground1)' : undefined }}>{r.avgActiveAge}</span>),
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
        <Tooltip relationship="description" content={r.explain}>
          <Badge appearance="filled" color={r.tone}>{r.effectiveness}</Badge>
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
      render: (n) => <Badge appearance="tint" color={n.statusTone ?? 'informative'}>{n.status}</Badge>,
    },
    { key: 'msi', header: 'MSI', align: 'end', sortValue: (n) => n.msiScore ?? 0, render: (n) => n.msiScore ?? '—' },
    {
      key: 'days',
      header: 'Delivery days',
      align: 'end',
      sortValue: (n) => n.deliveryDays ?? Number.MAX_SAFE_INTEGER,
      render: (n) => n.deliveryDays ?? '—',
    },
    { key: 'acr', header: 'ACR', align: 'end', sortValue: (n) => n.totalAcr ?? 0, render: (n) => money(n.totalAcr ?? 0) },
  ]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text size={600} weight="bold">Ownership Effectiveness</Text>
        <div style={{ color: 'var(--colorNeutralForeground3)', fontSize: 13, marginTop: 2 }}>
          Delivery scorecard for PM / SA / CFTL owners. Effectiveness is <strong>flow-led for PM/CFTL</strong> (on-track
          health · momentum = days-in-stage · delivery speed · delivered share) and <strong>quality-led for SA</strong>
          {' '}(MSI + on-track + speed + momentum) — gate-driven MSI is an SA lever, and success rate is omitted (with
          almost no withdrawn/deferred outcomes it reads ~100% for everyone).
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
        <KpiCard label="Delivered %" value={`${kpis.deliveredPct}%`} />
        <KpiCard label="Avg delivery days" value={kpis.avgDays ?? '—'} />
        <KpiCard label="Avg age (active)" value={kpis.avgAge ?? '—'} tone={kpis.avgAge != null && kpis.avgAge > 45 ? 'warning' : 'neutral'} />
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
            <Stat label="Delivered %" value={`${detail.row.deliveredPct}%`} />
            <Stat label="Withdrawn" value={detail.row.withdrawn} />
            <Stat label="Deferred" value={detail.row.deferred} />
            <Stat label="Avg days" value={detail.row.avgDeliveryDays ?? '—'} />
            <Stat label="Avg age" value={detail.row.avgActiveAge ?? '—'} />
            <Stat label="On-track %" value={pct(detail.row.onTrackPct)} />
            <Stat label="SLA breaches" value={detail.row.slaBreaches} />
            <Stat label="Avg MSI" value={detail.row.avgMsi} />
            <Stat label="ACR delivered" value={money(detail.row.acrDelivered)} />
            <Stat label="Effectiveness" value={<Badge appearance="filled" color={detail.row.tone}>{detail.row.effectiveness}</Badge>} />
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
