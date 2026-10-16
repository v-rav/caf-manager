import { Badge, Button, Text } from '@fluentui/react-components'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable, type Column } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, FilterText, Loading, NominationLink, PageHeader, Panel } from '../components/common'
import { ExportCsvButton, FilterBar, HealthBadge, KpiRow, SlaBadge } from '../components/ui'
import type { CsvColumn } from '../export'
import { useFacets, useUrlFilters } from '../filters'
import { nominationInFy, useFy } from '../fy'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import type { Blocker, Nomination } from '../types'

const TIERS = ['Warn', 'Escalate', 'Defer']

const KEYS = ['sla', 'status', 'stage', 'reason', 'owner'] as const
const pickers = {
  sla: (n: Nomination) => n.staleTier ?? '',
  status: (n: Nomination) => n.healthBucket ?? '',
  stage: (n: Nomination) => (n.stageIndex ? `${n.stageIndex} · ${n.stageLabel ?? ''}` : ''),
  reason: (n: Nomination) => n.suggestedBlockerReason ?? '',
}
const has = (v: string | undefined | null, part: string) => (v ?? '').toLowerCase().includes(part.trim().toLowerCase())

/** Exceptions board: in-flight waves that are blocked, waiting, or past their stage target — all from FDO data. */
export function GovernanceBoardPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const noms = useAsync(() => api.nominations(region), [region])
  const legacy = useAsync(() => api.openBlockers(region), [region])
  const { f, set, reset, any: anyFilter } = useUrlFilters(KEYS)
  const [busy, setBusy] = useState(false)

  // Approved, started, not settled — and either blocked/waiting in FDO or already past the stage target.
  const exceptions = useMemo(() => (noms.data ?? []).filter((n) =>
    nominationInFy(n, fy) && !n.isSettled && !n.isUpcoming
    && n.approvalStatus?.toLowerCase() === 'approved'
    && (n.healthBucket === 'Blocked' || n.healthBucket === 'Waiting' || !!n.staleTier)
    && (!f.owner || has(n.solutionArchitect, f.owner) || has(n.projectCoordinator, f.owner) || has(n.cftlPrimary, f.owner))),
    [noms.data, fy, f.owner])

  const { rows, options } = useFacets<Nomination, keyof typeof pickers>(exceptions, pickers, f, { sla: TIERS })

  const count = (p: (n: Nomination) => boolean) => rows.filter(p).length
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
      render: (n) => <HealthBadge bucket={n.healthBucket} tone={n.healthTone} currentState={n.currentState} /> },
    { key: 'tier', header: 'SLA', sortValue: (n) => TIERS.indexOf(n.staleTier ?? '') + 1,
      render: (n) => (n.staleTier ? <SlaBadge tier={n.staleTier} tone={n.slaTone} /> : '—') },
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

  const csvColumns: CsvColumn<Nomination>[] = [
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
  ]

  if (noms.error) return <ErrorText error={noms.error} onRetry={noms.reload} />
  if (noms.loading && !noms.data) return <Loading label="Loading governance board…" />

  const legacyRows = legacy.data ?? []

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <PageHeader
        title="Governance Board"
        subtitle="In-flight waves that are blocked, waiting, or past their stage target — straight from FDO, worst first."
        actions={<ExportCsvButton name="governance-exceptions" columns={csvColumns} rows={rows} primary />}
      />

      <FilterBar active={anyFilter} onReset={reset}>
        <FilterSelect label="SLA tier" value={f.sla} options={options.sla} onChange={set('sla')} minWidth={120} />
        <FilterSelect label="Status" value={f.status} options={options.status} onChange={set('status')} minWidth={120} />
        <FilterSelect label="Stage" value={f.stage} options={options.stage} onChange={set('stage')} />
        <FilterSelect label="Reason" value={f.reason} options={options.reason} onChange={set('reason')} minWidth={200} />
        <FilterText label="SA / PM / CFTL" value={f.owner} onChange={set('owner')} placeholder="Search owner name" minWidth={190} />
      </FilterBar>

      <KpiRow>
        <KpiCard label="Needs attention" value={String(rows.length)} tone={rows.length ? 'warning' : 'success'} />
        <KpiCard label="Blocked" value={String(count((n) => n.healthBucket === 'Blocked'))} tone="danger" />
        <KpiCard label="Waiting" value={String(count((n) => n.healthBucket === 'Waiting'))} tone="warning" />
        <KpiCard label="Past target" value={String(count((n) => !!n.staleTier))} tone="warning" />
        <KpiCard label="Long-stalled" value={String(count((n) => !!n.isLongStalled))} tone="danger" />
      </KpiRow>

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
