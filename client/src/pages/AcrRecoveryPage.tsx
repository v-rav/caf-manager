import { Badge, Button, Dropdown, Input, Menu, MenuItemCheckbox, MenuList, MenuPopover, MenuTrigger, Option, Text, Tooltip } from '@fluentui/react-components'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { Modal } from '../components/Modal'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useAuth } from '../auth'
import { useRegion } from '../region'
import { downloadCsv } from '../export'
import type { AcrCaptureRow, AcrRecoveryEntry } from '../types'

const money = (n: number) =>
  n >= 1e9 ? `$${(n / 1e9).toFixed(1)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`
const num = (n: number) => n.toLocaleString()
const day = (s?: string | null) => (s ? new Date(s).toLocaleDateString() : '—')

const GAP_TONE: Record<string, 'danger' | 'warning' | 'informative'> = {
  'Missing cores': 'danger',
  'Missing ACR': 'danger',
  'Low cores': 'warning',
}
const STATUS_TONE: Record<string, 'informative' | 'warning' | 'success' | 'subtle'> = {
  Flagged: 'informative',
  Notified: 'warning',
  Realized: 'success',
  Closed: 'subtle',
}

// Toggleable worklist columns (Account + action are always shown). TPID and Status are hidden by default.
const TOGGLE_COLS: [string, string][] = [
  ['tpid', 'TPID'], ['stage', 'Stage'], ['status', 'Status'], ['currentState', 'State'], ['path', 'Path'],
  ['cores', 'Cores'], ['acr', 'ACR'], ['gapType', 'Gap'], ['estimatedCores', 'Est. cores'], ['estimatedAcr', 'Est. ACR'], ['gapAcr', 'Upside/yr'],
]
const DEFAULT_VISIBLE = TOGGLE_COLS.map((c) => c[0]).filter((k) => k !== 'tpid' && k !== 'status')

export function AcrRecoveryPage() {
  const { region } = useRegion()
  const { user } = useAuth()
  const canApply = user?.role === 'Admin' || user?.role === 'Lead'
  const capture = useAsync(() => api.acrCapture(region), [region])
  const recovery = useAsync(() => api.recoveryList(region), [region])
  const summary = useAsync(() => api.recoverySummary(region), [region])
  const [applyRow, setApplyRow] = useState<AcrCaptureRow | null>(null)
  const [closeEntry, setCloseEntry] = useState<AcrRecoveryEntry | null>(null)
  const [stage, setStage] = useState('')
  const [gap, setGap] = useState('')
  const [state, setState] = useState('')
  const [status, setStatus] = useState('')
  const [visibleCols, setVisibleCols] = useState<string[]>(DEFAULT_VISIBLE)
  const [reconciling, setReconciling] = useState(false)

  const data = capture.data
  const openIds = new Set((recovery.data ?? []).filter((e) => e.status === 'Flagged' || e.status === 'Notified').map((e) => e.nominationId))
  const statusByNom = new Map((recovery.data ?? []).map((e) => [e.nominationId, e.status]))
  const reloadRecovery = () => { recovery.reload(); summary.reload() }

  const flag = async (r: AcrCaptureRow) => {
    await api.flagRecovery({ nominationId: r.id, recommendedCores: r.estimatedCores, recommendedAcr: r.estimatedAcr, gapType: r.gapType })
    reloadRecovery()
  }
  const notify = async (id: number) => { await api.markRecoveryNotified(id); reloadRecovery() }
  const reconcile = async () => {
    setReconciling(true)
    try { await api.reconcileRecovery() } finally { setReconciling(false); reloadRecovery() }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <Text size={600} weight="bold" style={{ display: 'block' }}>ACR Recovery</Text>
          <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
            Find and recover under-captured ACR on containerized migrations{region ? ` · ${region}` : ' · all regions'}.
            ACR is linear in cores, so capturing the true container-core count is the whole lever.
          </Text>
        </div>
        {canApply && <Button appearance="secondary" onClick={reconcile} disabled={reconciling}>{reconciling ? 'Reconciling…' : 'Reconcile now'}</Button>}
      </div>

      {/* Recovery funnel — the claim attributable to the analysis. */}
      {summary.data && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
          <KpiCard label="Claims flagged" value={num(summary.data.flaggedCount)} tone="brand" />
          <KpiCard label="SA notified" value={num(summary.data.notifiedCount)} tone="warning" />
          <KpiCard label="Realized" value={num(summary.data.realizedCount)} tone="success" />
          <KpiCard label="ACR recovered (claimed)" value={money(summary.data.recoveredAcrTotal)} tone="success" />
          <KpiCard label="Recovery rate" value={`${summary.data.recoveryRatePct}%`} tone="neutral" />
          {summary.data.avgDaysToRealize != null && <KpiCard label="Avg days to realize" value={`${summary.data.avgDaysToRealize}d`} tone="neutral" />}
        </div>
      )}

      {capture.loading && !data ? <Loading label="Scanning containerized ACR capture…" /> : capture.error ? <ErrorText error={capture.error} onRetry={capture.reload} /> : data ? (() => {
        const uniq = (xs: (string | null | undefined)[]) => [...new Set(xs.filter((x): x is string => !!x))].sort()
        const stages = uniq(data.rows.map((r) => r.stage))
        const gaps = uniq(data.rows.map((r) => r.gapType))
        const states = uniq(data.rows.map((r) => r.currentState))
        const statuses = uniq(data.rows.map((r) => r.status))
        const rows = data.rows.filter((r) =>
          (!stage || r.stage === stage) && (!gap || r.gapType === gap) && (!state || r.currentState === state) && (!status || r.status === status))
        const filtered = rows.length !== data.rows.length
        const filteredUpside = rows.reduce((s, r) => s + r.gapAcr, 0)

        const exportCsv = () => downloadCsv('acr-core-capture', [
          { header: 'Account', value: (r: AcrCaptureRow) => r.account },
          { header: 'TPID', value: (r: AcrCaptureRow) => r.tpid ?? '' },
          { header: 'Region', value: (r: AcrCaptureRow) => r.region ?? '' },
          { header: 'Migration path', value: (r: AcrCaptureRow) => r.path },
          { header: 'Stage', value: (r: AcrCaptureRow) => r.stage },
          { header: 'Status', value: (r: AcrCaptureRow) => r.status ?? '' },
          { header: 'Current state', value: (r: AcrCaptureRow) => r.currentState ?? '' },
          { header: 'Cores', value: (r: AcrCaptureRow) => r.cores },
          { header: 'ACR', value: (r: AcrCaptureRow) => Math.round(r.acr) },
          { header: 'Gap', value: (r: AcrCaptureRow) => r.gapType },
          { header: 'Est. cores', value: (r: AcrCaptureRow) => r.estimatedCores },
          { header: 'Est. ACR', value: (r: AcrCaptureRow) => Math.round(r.estimatedAcr) },
          { header: 'Upside ACR/yr', value: (r: AcrCaptureRow) => Math.round(r.gapAcr) },
        ], rows)

        const rowAction = (r: AcrCaptureRow) => {
          const st = statusByNom.get(r.id)
          if (openIds.has(r.id)) return <Badge appearance="tint" color={st === 'Notified' ? 'warning' : 'informative'}>{st}</Badge>
          if (st === 'Realized') return <Badge appearance="tint" color="success">Realized</Badge>
          return (
            <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
              <Tooltip relationship="label" content="Freeze this baseline as an ACR recovery claim (before the SA updates FDO).">
                <Button size="small" appearance="primary" onClick={() => void flag(r)}>Flag</Button>
              </Tooltip>
              <Tooltip relationship="label" content="Capture corrected cores → re-price ACR and write it back to this nomination now (audited).">
                <Button size="small" appearance="secondary" onClick={() => setApplyRow(r)}>Apply</Button>
              </Tooltip>
            </div>
          )
        }

        const columns = [
          { key: 'account', header: 'Account', sortValue: (r: AcrCaptureRow) => r.account, render: (r: AcrCaptureRow) => <RouterLink to={`/nominations/${r.id}`}>{r.account}</RouterLink> },
          { key: 'tpid', header: 'TPID', sortValue: (r: AcrCaptureRow) => r.tpid ?? '', render: (r: AcrCaptureRow) => r.tpid ?? '—' },
          { key: 'stage', header: 'Stage', sortValue: (r: AcrCaptureRow) => r.stage, render: (r: AcrCaptureRow) => <span style={{ fontSize: 12 }}>{r.stage}</span> },
          { key: 'status', header: 'Status', sortValue: (r: AcrCaptureRow) => r.status ?? '', render: (r: AcrCaptureRow) => <span style={{ fontSize: 12 }}>{r.status ?? '—'}</span> },
          { key: 'currentState', header: 'State', sortValue: (r: AcrCaptureRow) => r.currentState ?? '', render: (r: AcrCaptureRow) => <span style={{ fontSize: 12 }}>{r.currentState ?? '—'}</span> },
          { key: 'path', header: 'Path', sortValue: (r: AcrCaptureRow) => r.path, render: (r: AcrCaptureRow) => <span style={{ fontSize: 12 }}>{r.path}</span> },
          { key: 'cores', header: 'Cores', align: 'end' as const, sortValue: (r: AcrCaptureRow) => r.cores },
          { key: 'acr', header: 'ACR', align: 'end' as const, sortValue: (r: AcrCaptureRow) => r.acr, render: (r: AcrCaptureRow) => money(r.acr) },
          { key: 'gapType', header: 'Gap', sortValue: (r: AcrCaptureRow) => r.gapType, render: (r: AcrCaptureRow) => <Badge appearance="tint" color={GAP_TONE[r.gapType] ?? 'informative'}>{r.gapType}</Badge> },
          { key: 'estimatedCores', header: 'Est. cores', align: 'end' as const, sortValue: (r: AcrCaptureRow) => r.estimatedCores },
          { key: 'estimatedAcr', header: 'Est. ACR', align: 'end' as const, sortValue: (r: AcrCaptureRow) => r.estimatedAcr, render: (r: AcrCaptureRow) => money(r.estimatedAcr) },
          { key: 'gapAcr', header: 'Upside/yr', align: 'end' as const, sortValue: (r: AcrCaptureRow) => r.gapAcr, render: (r: AcrCaptureRow) => <b>{money(r.gapAcr)}</b> },
          ...(canApply ? [{ key: 'action', header: '', align: 'end' as const, render: rowAction }] : []),
        ]
        // Account + action always shown; the rest respect the Columns menu.
        const shownColumns = columns.filter((c) => c.key === 'account' || c.key === 'action' || visibleCols.includes(c.key))

        return (
          <>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
              <KpiCard label="Under-captured records" value={num(data.flaggedCount)} tone="warning" />
              <KpiCard label="Est. recoverable ACR/yr" value={money(data.estimatedUpside)} tone="success" />
              <KpiCard label="ACR with no core basis" value={money(data.acrAtRisk)} tone="danger" />
              <KpiCard label="Missing cores" value={num(data.missingCoresCount)} tone="neutral" />
              <KpiCard label="Missing ACR" value={num(data.missingAcrCount)} tone="neutral" />
              <KpiCard label="Low cores (≤16)" value={num(data.lowCoresCount)} tone="neutral" />
            </div>

            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Containers drive {data.containerAcrShare}% of Approved ACR ({money(data.containerAcr)} across {data.containerNoms} nominations).
              These {data.flaggedCount} records look under-captured; estimates assume a conservative floor of {data.coreFloor} cores
              for a containerized workload and the live AKS Linux/Windows core rates. <b>Flag</b> freezes a baseline for recovery tracking;
              <b> Apply</b> writes a correction straight to the portal now.
            </Text>

            <Panel title="Worklist — biggest ACR upside first" action={<Button appearance="secondary" size="small" onClick={exportCsv} disabled={!rows.length}>Export CSV</Button>}>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center', marginBottom: 10 }}>
                <FilterSelect label="Stage" value={stage} options={stages} onChange={setStage} minWidth={170} />
                <FilterSelect label="Status" value={status} options={statuses} onChange={setStatus} minWidth={150} />
                <FilterSelect label="State" value={state} options={states} onChange={setState} minWidth={150} />
                <FilterSelect label="Gap" value={gap} options={gaps} onChange={setGap} minWidth={150} />
                <Menu checkedValues={{ columns: visibleCols }} onCheckedValueChange={(_, d) => setVisibleCols(d.checkedItems)}>
                  <MenuTrigger disableButtonEnhancement>
                    <Button appearance="secondary" size="small">Columns</Button>
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      {TOGGLE_COLS.map(([k, l]) => <MenuItemCheckbox key={k} name="columns" value={k}>{l}</MenuItemCheckbox>)}
                    </MenuList>
                  </MenuPopover>
                </Menu>
                {filtered && (
                  <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                    {rows.length} of {data.rows.length} · {money(filteredUpside)}/yr upside
                  </Text>
                )}
              </div>
              <DataTable
                ariaLabel="ACR core-capture worklist"
                rows={rows}
                rowKey={(r) => r.id}
                defaultSort={{ key: 'gapAcr', dir: 'desc' }}
                columns={shownColumns}
              />
            </Panel>

            <RecoveryLedger
              entries={recovery.data ?? []}
              loading={recovery.loading}
              error={recovery.error}
              onRetry={recovery.reload}
              canAct={canApply}
              onNotify={notify}
              onClose={setCloseEntry}
            />

            <AcrEstimator />

            {applyRow && <ApplyAcrModal row={applyRow} onClose={() => setApplyRow(null)} onApplied={() => { setApplyRow(null); capture.reload() }} />}
            {closeEntry && <CloseRecoveryModal entry={closeEntry} onClose={() => setCloseEntry(null)} onClosed={() => { setCloseEntry(null); reloadRecovery() }} />}
          </>
        )
      })() : null}
    </div>
  )
}

// The recovery ledger: baseline → realized per claim, with the funnel actions.
function RecoveryLedger({ entries, loading, error, onRetry, canAct, onNotify, onClose }: {
  entries: AcrRecoveryEntry[]; loading: boolean; error: string | undefined; onRetry: () => void; canAct: boolean;
  onNotify: (id: number) => void; onClose: (e: AcrRecoveryEntry) => void
}) {
  const exportCsv = () => downloadCsv('acr-recovery-ledger', [
    { header: 'Account', value: (e: AcrRecoveryEntry) => e.account },
    { header: 'TPID', value: (e: AcrRecoveryEntry) => e.tpid ?? '' },
    { header: 'Gap', value: (e: AcrRecoveryEntry) => e.gapType ?? '' },
    { header: 'Baseline cores', value: (e: AcrRecoveryEntry) => e.baselineCores ?? '' },
    { header: 'Baseline ACR', value: (e: AcrRecoveryEntry) => e.baselineAcr != null ? Math.round(e.baselineAcr) : '' },
    { header: 'Recommended ACR', value: (e: AcrRecoveryEntry) => e.recommendedAcr != null ? Math.round(e.recommendedAcr) : '' },
    { header: 'Current ACR', value: (e: AcrRecoveryEntry) => e.currentAcr != null ? Math.round(e.currentAcr) : '' },
    { header: 'Recovered ACR', value: (e: AcrRecoveryEntry) => e.recoveredAcr != null ? Math.round(e.recoveredAcr) : '' },
    { header: 'Status', value: (e: AcrRecoveryEntry) => e.status },
    { header: 'Flagged', value: (e: AcrRecoveryEntry) => day(e.flaggedUtc) },
    { header: 'Notified', value: (e: AcrRecoveryEntry) => day(e.notifiedUtc) },
    { header: 'Realized', value: (e: AcrRecoveryEntry) => day(e.realizedUtc) },
  ], entries)

  const columns = [
    { key: 'account', header: 'Account', sortValue: (e: AcrRecoveryEntry) => e.account, render: (e: AcrRecoveryEntry) => <RouterLink to={`/nominations/${e.nominationId}`}>{e.account}</RouterLink> },
    { key: 'tpid', header: 'TPID', sortValue: (e: AcrRecoveryEntry) => e.tpid ?? '', render: (e: AcrRecoveryEntry) => e.tpid ?? '—' },
    { key: 'gapType', header: 'Gap', sortValue: (e: AcrRecoveryEntry) => e.gapType ?? '', render: (e: AcrRecoveryEntry) => e.gapType ? <Badge appearance="tint" color={GAP_TONE[e.gapType] ?? 'informative'}>{e.gapType}</Badge> : '—' },
    { key: 'baselineAcr', header: 'Baseline', align: 'end' as const, sortValue: (e: AcrRecoveryEntry) => e.baselineAcr ?? 0, render: (e: AcrRecoveryEntry) => money(e.baselineAcr ?? 0) },
    { key: 'recommendedAcr', header: 'Recommended', align: 'end' as const, sortValue: (e: AcrRecoveryEntry) => e.recommendedAcr ?? 0, render: (e: AcrRecoveryEntry) => e.recommendedAcr != null ? money(e.recommendedAcr) : '—' },
    { key: 'currentAcr', header: 'Current', align: 'end' as const, sortValue: (e: AcrRecoveryEntry) => e.currentAcr ?? 0, render: (e: AcrRecoveryEntry) => e.currentAcr != null ? money(e.currentAcr) : '—' },
    { key: 'recoveredAcr', header: 'Recovered', align: 'end' as const, sortValue: (e: AcrRecoveryEntry) => e.recoveredAcr ?? 0, render: (e: AcrRecoveryEntry) => e.recoveredAcr != null ? <b style={{ color: 'var(--colorPaletteGreenForeground2)' }}>{money(e.recoveredAcr)}</b> : '—' },
    { key: 'status', header: 'Status', sortValue: (e: AcrRecoveryEntry) => e.status, render: (e: AcrRecoveryEntry) => <Badge appearance="tint" color={STATUS_TONE[e.status] ?? 'informative'}>{e.status}</Badge> },
    { key: 'flaggedUtc', header: 'Flagged', sortValue: (e: AcrRecoveryEntry) => e.flaggedUtc, render: (e: AcrRecoveryEntry) => <span style={{ fontSize: 12 }}>{day(e.flaggedUtc)}</span> },
    ...(canAct ? [{ key: 'act', header: '', align: 'end' as const, render: (e: AcrRecoveryEntry) => (
      <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
        {e.status === 'Flagged' && <Button size="small" appearance="secondary" onClick={() => onNotify(e.id)}>Notify</Button>}
        {(e.status === 'Flagged' || e.status === 'Notified') && <Button size="small" appearance="subtle" onClick={() => onClose(e)}>Close</Button>}
      </div>
    ) }] : []),
  ]

  return (
    <Panel title="Recovery ledger" action={<Button appearance="secondary" size="small" onClick={exportCsv} disabled={!entries.length}>Export CSV</Button>}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 10 }}>
        Each claim freezes the ACR baseline when flagged. After the SA updates FDO and the next import lands, reconciliation
        books the rise above baseline as <b>recovered ACR</b> — the value attributable to this analysis. Realized only counts
        an increase, so it never over-claims.
      </Text>
      {loading && !entries.length ? <Loading /> : error ? <ErrorText error={error} onRetry={onRetry} /> : (
        <DataTable
          ariaLabel="ACR recovery ledger"
          rows={entries}
          rowKey={(e) => e.id}
          defaultSort={{ key: 'flaggedUtc', dir: 'desc' }}
          emptyMessage="No recovery claims yet — Flag a record in the worklist above to start tracking."
          columns={columns}
        />
      )}
    </Panel>
  )
}

// Close a recovery claim (false positive / no longer relevant) with an optional note.
function CloseRecoveryModal({ entry, onClose, onClosed }: { entry: AcrRecoveryEntry; onClose: () => void; onClosed: () => void }) {
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const submit = async () => {
    setBusy(true)
    try { await api.closeRecovery(entry.id, note || null); onClosed() } finally { setBusy(false) }
  }
  return (
    <Modal open title={`Close claim — ${entry.account}`} onClose={onClose} onSubmit={submit} submitLabel="Close claim" busy={busy} maxWidth={420}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        Closing removes this claim from the open funnel (e.g. a false positive or handled outside the portal). It won't be
        reconciled further.
      </Text>
      <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
        Note (optional)
        <Input value={note} onChange={(_, d) => setNote(d.value)} placeholder="e.g. duplicate / not containerized" />
      </label>
    </Modal>
  )
}

// Guarded, audited write-back of corrected cores/ACR onto a nomination (Admin/Lead only). FDO-owned —
// the note warns this updates the portal copy and can be superseded by the next FDO drop.
function ApplyAcrModal({ row, onClose, onApplied }: { row: AcrCaptureRow; onClose: () => void; onApplied: () => void }) {
  const win = /windows/i.test(row.path)
  const svc = win ? 'AksWindows' : 'AksLinux'
  const [cores, setCores] = useState(String(row.estimatedCores))
  const [reason, setReason] = useState('')
  const [acr, setAcr] = useState<number | null>(row.estimatedAcr)
  const [busy, setBusy] = useState(false)

  const reprice = async (c: string) => {
    setCores(c)
    const n = Number(c)
    if (!n) { setAcr(null); return }
    try { const r = await api.acrEstimate({ targetService: svc, cores: n }); setAcr(r.annualAcr) } catch { /* keep last */ }
  }
  const apply = async () => {
    setBusy(true)
    try { await api.applyAcr(row.id, { cores: Number(cores), acr, reason: reason || null }); onApplied() } finally { setBusy(false) }
  }

  return (
    <Modal open title={`Capture cores — ${row.account}`} onClose={onClose} onSubmit={apply} submitLabel="Apply" submitDisabled={!Number(cores)} busy={busy} maxWidth={460}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        {row.path} · current {row.cores} cores · {money(row.acr)}. Enter the true container-derived core count; ACR re-prices
        at the {win ? 'AKS Windows' : 'AKS Linux'} rate.
      </Text>
      <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
        Captured cores
        <Input type="number" value={cores} onChange={(_, d) => reprice(d.value)} />
      </label>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
        <Badge appearance="tint" color="brand">{cores || 0} cores</Badge>
        <Badge appearance="filled" color="success">{acr != null ? `${money(acr)}/yr` : '—'}</Badge>
      </div>
      <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
        Reason / basis (optional)
        <Input value={reason} onChange={(_, d) => setReason(d.value)} placeholder="e.g. 5 containers × 4 cores, from TAD" />
      </label>
      <Text size={200} style={{ color: 'var(--colorPaletteDarkOrangeForeground1)' }}>
        Cores/ACR are FDO-owned. This updates the portal copy (audited on the nomination timeline) and can be superseded by
        the next FDO drop — update FDO itself for a durable change.
      </Text>
    </Modal>
  )
}

// Inline per-nomination ACR estimator (container count / cores → annual ACR) using the live rate master.
function AcrEstimator() {
  const [svc, setSvc] = useState('AksLinux')
  const [apps, setApps] = useState('')
  const [cores, setCores] = useState('')
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<{ cores: number; annualAcr: number; formula: string } | null>(null)

  const run = async () => {
    setBusy(true)
    try {
      const r = await api.acrEstimate({ targetService: svc, apps: apps ? Number(apps) : null, cores: cores ? Number(cores) : null })
      setResult({ cores: r.cores, annualAcr: r.annualAcr, formula: r.formula })
    } finally { setBusy(false) }
  }

  return (
    <Panel title="Estimate ACR for a nomination">
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
          Target service
          <Dropdown value={svc} selectedOptions={[svc]} onOptionSelect={(_, d) => setSvc(d.optionValue || 'AksLinux')} style={{ minWidth: 150 }}>
            <Option value="AksLinux">AKS (Linux)</Option>
            <Option value="AksWindows">AKS (Windows)</Option>
            <Option value="Aca">Azure Container Apps</Option>
            <Option value="AppService">App Service</Option>
          </Dropdown>
        </label>
        <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
          Apps / containers
          <Input type="number" value={apps} onChange={(_, d) => setApps(d.value)} placeholder="e.g. 5" style={{ width: 120 }} />
        </label>
        <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12 }}>
          or Cores
          <Input type="number" value={cores} onChange={(_, d) => setCores(d.value)} placeholder="e.g. 40" style={{ width: 120 }} />
        </label>
        <Button appearance="primary" onClick={run} disabled={busy || (!apps && !cores)}>Estimate</Button>
        {result && (
          <div style={{ display: 'flex', flexDirection: 'column' }}>
            <Text size={500} weight="bold" style={{ color: 'var(--colorPaletteGreenForeground2)' }}>{money(result.annualAcr)}/yr</Text>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>{result.cores} cores · {result.formula}</Text>
          </div>
        )}
      </div>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 8 }}>
        Apps convert to cores via the rate master (App Service 2, containerized 4 cores/app); or enter cores directly.
        Use this to feed a defensible ACR back into FDO — count each independently deployable container as an app.
      </Text>
    </Panel>
  )
}
