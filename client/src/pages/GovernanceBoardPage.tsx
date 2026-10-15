import { Badge, Button, Text } from '@fluentui/react-components'
import { useCallback, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { DataTable, type Column } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, NominationLink, PageHeader, Panel } from '../components/common'
import { downloadCsv } from '../export'
import { nominationInFy, useFy } from '../fy'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import type { Blocker, Nomination } from '../types'

const TIERS = ['Warn', 'Escalate', 'Defer']
const TIER_COLOR = { Warn: 'warning', Escalate: 'severe', Defer: 'danger' } as const
const HEALTH_COLOR = { Blocked: 'danger', Waiting: 'warning' } as const
const STAGES = ['1 · Validating', '2 · Pre-Requisites', '3 · Finalize Scope', '4 · Executing Migration']

type Dim = 'tier' | 'health' | 'stage' | 'sa' | 'pm' | 'cftl' | 'reason'
const EMPTY: Record<Dim, string> = { tier: '', health: '', stage: '', sa: '', pm: '', cftl: '', reason: '' }
const pickers: Record<Dim, (n: Nomination) => string> = {
  tier: (n) => n.staleTier ?? '',
  health: (n) => n.healthBucket ?? '',
  stage: (n) => (n.stageIndex ? STAGES[n.stageIndex - 1] : ''),
  sa: (n) => n.solutionArchitect?.trim() ?? '',
  pm: (n) => n.projectCoordinator?.trim() ?? '',
  cftl: (n) => n.cftlPrimary?.trim() ?? '',
  reason: (n) => n.suggestedBlockerReason ?? '',
}

/** Exceptions board: in-flight waves that are blocked, waiting, or past their stage target — all from FDO data. */
export function GovernanceBoardPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const noms = useAsync(() => api.nominations(region), [region])
  const legacy = useAsync(() => api.openBlockers(region), [region])
  const [params] = useSearchParams()
  const [f, setF] = useState<Record<Dim, string>>(() => ({ ...EMPTY, tier: params.get('sla') ?? '', health: params.get('status') ?? '' }))
  const [busy, setBusy] = useState(false)
  const setDim = (d: Dim) => (v: string) => setF((s) => ({ ...s, [d]: v }))

  // Approved, started, not settled — and either blocked/waiting in FDO or already past the stage target.
  const exceptions = useMemo(() => (noms.data ?? []).filter((n) =>
    nominationInFy(n, fy) && !n.isSettled && !n.isUpcoming
    && n.approvalStatus?.toLowerCase() === 'approved'
    && (n.healthBucket === 'Blocked' || n.healthBucket === 'Waiting' || !!n.staleTier)), [noms.data, fy])

  const match = useCallback((n: Nomination, skip?: Dim) =>
    (Object.keys(f) as Dim[]).every((d) => d === skip || !f[d] || pickers[d](n).toLowerCase() === f[d].toLowerCase()), [f])
  const rows = useMemo(() => exceptions.filter((n) => match(n)), [exceptions, match])

  // Each dropdown lists only values that still exist under the other filters.
  const options = useMemo(() => {
    const out = {} as Record<Dim, string[]>
    for (const d of Object.keys(f) as Dim[]) {
      const vals = new Set(exceptions.filter((n) => match(n, d)).map(pickers[d]).filter(Boolean))
      out[d] = d === 'tier' ? TIERS.filter((t) => vals.has(t)) : d === 'stage' ? STAGES.filter((s) => vals.has(s)) : [...vals].sort((a, b) => a.localeCompare(b))
    }
    return out
  }, [exceptions, match, f])

  const count = (p: (n: Nomination) => boolean) => rows.filter(p).length
  const anyFilter = Object.values(f).some(Boolean)
  const resolve = async (b: Blocker) => {
    if (busy) return
    setBusy(true)
    try { await api.resolveBlocker(b.nominationId, b.id); legacy.reload() } finally { setBusy(false) }
  }

  const columns: Column<Nomination>[] = [
    { key: 'account', header: 'Account', minWidth: 190, sortValue: (n) => n.accountName ?? '',
      render: (n) => <NominationLink id={n.id} name={n.accountName} /> },
    { key: 'stage', header: 'Stage', align: 'center', sortValue: (n) => n.stageIndex ?? 0, render: (n) => n.stageIndex ?? '—' },
    { key: 'health', header: 'Status', sortValue: (n) => n.healthBucket ?? '',
      render: (n) => n.healthBucket && n.healthBucket in HEALTH_COLOR
        ? <Badge appearance="tint" color={HEALTH_COLOR[n.healthBucket as keyof typeof HEALTH_COLOR]}>{n.healthBucket}</Badge> : (n.healthBucket ?? '—') },
    { key: 'tier', header: 'SLA', sortValue: (n) => TIERS.indexOf(n.staleTier ?? '') + 1,
      render: (n) => n.staleTier ? <Badge appearance="tint" color={TIER_COLOR[n.staleTier as keyof typeof TIER_COLOR] ?? 'informative'}>{n.staleTier}</Badge> : '—' },
    { key: 'days', header: 'Days in stage', align: 'end', sortValue: (n) => n.effectiveAgeDays,
      render: (n) => `${n.effectiveAgeDays}d${n.stageTargetDays ? ` / ${n.stageTargetDays}d` : ''}` },
    { key: 'over', header: 'Over target', align: 'end', sortValue: (n) => n.daysOverTarget ?? -1,
      render: (n) => n.daysOverTarget ? <b style={{ color: 'var(--colorPaletteRedForeground1)' }}>+{n.daysOverTarget}d</b> : '—' },
    { key: 'reason', header: 'Reason', sortValue: (n) => n.suggestedBlockerReason ?? '', render: (n) => n.suggestedBlockerReason ?? '—' },
    { key: 'sa', header: 'SA', sortValue: (n) => n.solutionArchitect ?? '', render: (n) => n.solutionArchitect ?? '—' },
    { key: 'pm', header: 'PM', sortValue: (n) => n.projectCoordinator ?? '', render: (n) => n.projectCoordinator ?? '—' },
    { key: 'cftl', header: 'CFTL', sortValue: (n) => n.cftlPrimary ?? '', render: (n) => n.cftlPrimary ?? '—' },
    { key: 'next', header: 'Next step', minWidth: 240, sortValue: (n) => n.nextStep ?? '', render: (n) => n.nextStep ?? '—' },
  ]

  const exportRows = () =>
    downloadCsv<Nomination>(`governance-exceptions-${new Date().toISOString().slice(0, 10)}`, [
      { header: 'Task Id', value: (n) => n.externalTaskId ?? '' },
      { header: 'Account', value: (n) => n.accountName ?? '' },
      { header: 'TPID', value: (n) => n.tpid ?? '' },
      { header: 'Stage', value: (n) => n.stageIndex ?? '' },
      { header: 'Status', value: (n) => n.healthBucket ?? '' },
      { header: 'Current state', value: (n) => n.currentState ?? '' },
      { header: 'SLA tier', value: (n) => n.staleTier ?? '' },
      { header: 'Days in stage', value: (n) => n.effectiveAgeDays },
      { header: 'Stage target', value: (n) => n.stageTargetDays ?? '' },
      { header: 'Days over target', value: (n) => n.daysOverTarget ?? '' },
      { header: 'Reason', value: (n) => n.suggestedBlockerReason ?? '' },
      { header: 'SA', value: (n) => n.solutionArchitect ?? '' },
      { header: 'PM', value: (n) => n.projectCoordinator ?? '' },
      { header: 'CFTL', value: (n) => n.cftlPrimary ?? '' },
      { header: 'Next step', value: (n) => n.nextStep ?? '' },
      { header: 'Remarks', value: (n) => n.remarks ?? '' },
    ], rows)

  if (noms.error) return <ErrorText error={noms.error} onRetry={noms.reload} />
  if (noms.loading && !noms.data) return <Loading label="Loading governance board…" />

  const legacyRows = legacy.data ?? []

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <PageHeader
        title="Governance Board"
        subtitle="In-flight waves that are blocked, waiting, or past their stage target — straight from FDO, worst first."
        actions={<Button appearance="primary" onClick={exportRows} disabled={!rows.length}>Export CSV</Button>}
      />

      <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <FilterSelect label="SLA" value={f.tier} options={options.tier} onChange={setDim('tier')} minWidth={120} />
        <FilterSelect label="Status" value={f.health} options={options.health} onChange={setDim('health')} minWidth={120} />
        <FilterSelect label="Stage" value={f.stage} options={options.stage} onChange={setDim('stage')} />
        <FilterSelect label="Reason" value={f.reason} options={options.reason} onChange={setDim('reason')} minWidth={200} />
        <FilterSelect label="SA" value={f.sa} options={options.sa} onChange={setDim('sa')} minWidth={180} />
        <FilterSelect label="PM" value={f.pm} options={options.pm} onChange={setDim('pm')} minWidth={180} />
        <FilterSelect label="CFTL" value={f.cftl} options={options.cftl} onChange={setDim('cftl')} minWidth={180} />
        {anyFilter && <Button size="small" onClick={() => setF(EMPTY)}>Reset</Button>}
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Needs attention" value={String(rows.length)} tone={rows.length ? 'warning' : 'success'} />
        <KpiCard label="Blocked" value={String(count((n) => n.healthBucket === 'Blocked'))} tone="danger" />
        <KpiCard label="Waiting" value={String(count((n) => n.healthBucket === 'Waiting'))} tone="warning" />
        <KpiCard label="Past target" value={String(count((n) => !!n.staleTier))} tone="warning" />
        <KpiCard label="Long-stalled" value={String(count((n) => !!n.isLongStalled))} tone="danger" />
      </div>

      <Panel title={`Exceptions · ${rows.length}`}>
        <DataTable<Nomination>
          ariaLabel="Governance exceptions"
          rows={rows}
          rowKey={(n) => n.id}
          columns={columns}
          defaultSort={{ key: 'over', dir: 'desc' }}
          emptyMessage="Nothing blocked, waiting or past target for these filters."
          expandedContent={(n) => (
            <div style={{ padding: '6px 12px', fontSize: 12 }}>
              <b>Current state:</b> {n.currentState ?? '—'}<br />
              <b>Remarks:</b> {n.remarks?.trim() || '—'}
            </div>
          )}
        />
      </Panel>

      {legacyRows.length > 0 && (
        <Panel title={`Legacy portal blockers · ${legacyRows.length}`}>
          <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)', marginBottom: 8 }}>
            Blockers are now tracked in FDO (Current State). Resolve these older portal entries once cleared.
          </Text>
          <DataTable<Blocker>
            ariaLabel="Legacy portal blockers"
            rows={legacyRows}
            rowKey={(b) => b.id}
            pageSize={10}
            columns={[
              { key: 'account', header: 'Account', sortValue: (b) => b.account ?? '', render: (b) => <NominationLink id={b.nominationId} name={b.account} /> },
              { key: 'category', header: 'Category', sortValue: (b) => b.category },
              { key: 'days', header: 'Days', align: 'end', sortValue: (b) => b.daysBlocked, render: (b) => <Badge appearance="tint" color={b.ageTone ?? 'informative'}>{b.daysBlocked}d</Badge> },
              { key: 'action', header: '', render: (b) => <Button size="small" disabled={busy} onClick={() => void resolve(b)}>Resolve</Button> },
            ]}
          />
        </Panel>
      )}
    </div>
  )
}
