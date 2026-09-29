import { Badge, Button, Dropdown, Input, Option, Text, Tooltip } from '@fluentui/react-components'
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
import type { AcrCaptureRow } from '../types'

const money = (n: number) =>
  n >= 1e9 ? `$${(n / 1e9).toFixed(1)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`
const num = (n: number) => n.toLocaleString()

const GAP_TONE: Record<string, 'danger' | 'warning' | 'informative'> = {
  'Missing cores': 'danger',
  'Missing ACR': 'danger',
  'Low cores': 'warning',
}

export function AcrRecoveryPage() {
  const { region } = useRegion()
  const { user } = useAuth()
  const canApply = user?.role === 'Admin' || user?.role === 'Lead'
  const { data, loading, error, reload } = useAsync(() => api.acrCapture(region), [region])
  const [applyRow, setApplyRow] = useState<AcrCaptureRow | null>(null)
  const [stage, setStage] = useState('')
  const [gap, setGap] = useState('')
  const [state, setState] = useState('')
  const [status, setStatus] = useState('')

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>ACR Recovery</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Find and recover under-captured ACR on containerized migrations{region ? ` · ${region}` : ' · all regions'}.
          ACR is linear in cores, so capturing the true container-core count is the whole lever.
        </Text>
      </div>

      {loading && !data ? <Loading label="Scanning containerized ACR capture…" /> : error ? <ErrorText error={error} onRetry={reload} /> : data ? (() => {
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
          ...(canApply ? [{ key: 'apply', header: '', align: 'end' as const, render: (r: AcrCaptureRow) => <Tooltip relationship="label" content="Capture corrected cores → re-price ACR at the AKS rate and write it back to this nomination (audited on its timeline)."><Button size="small" appearance="secondary" onClick={() => setApplyRow(r)}>Apply</Button></Tooltip> }] : []),
        ]

        return (
          <>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
              <KpiCard label="Flagged records" value={num(data.flaggedCount)} tone="warning" />
              <KpiCard label="Est. recoverable ACR/yr" value={money(data.estimatedUpside)} tone="success" />
              <KpiCard label="ACR with no core basis" value={money(data.acrAtRisk)} tone="danger" />
              <KpiCard label="Missing cores" value={num(data.missingCoresCount)} tone="neutral" />
              <KpiCard label="Missing ACR" value={num(data.missingAcrCount)} tone="neutral" />
              <KpiCard label="Low cores (≤16)" value={num(data.lowCoresCount)} tone="neutral" />
            </div>

            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              Containers drive {data.containerAcrShare}% of Approved ACR ({money(data.containerAcr)} across {data.containerNoms} nominations).
              These {data.flaggedCount} records look under-captured; estimates assume a conservative floor of {data.coreFloor} cores
              for a containerized workload and the live AKS Linux/Windows core rates.
            </Text>

            <Panel title="Worklist — biggest ACR upside first" action={<Button appearance="secondary" size="small" onClick={exportCsv} disabled={!rows.length}>Export CSV</Button>}>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center', marginBottom: 10 }}>
                <FilterSelect label="Stage" value={stage} options={stages} onChange={setStage} minWidth={170} />
                <FilterSelect label="Status" value={status} options={statuses} onChange={setStatus} minWidth={150} />
                <FilterSelect label="State" value={state} options={states} onChange={setState} minWidth={150} />
                <FilterSelect label="Gap" value={gap} options={gaps} onChange={setGap} minWidth={150} />
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
                columns={columns}
              />
            </Panel>

            <AcrEstimator />

            {applyRow && <ApplyAcrModal row={applyRow} onClose={() => setApplyRow(null)} onApplied={() => { setApplyRow(null); reload() }} />}
          </>
        )
      })() : null}
    </div>
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
