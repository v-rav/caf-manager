import { Badge, Button, Text } from '@fluentui/react-components'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { api } from '../api'
import { DataTable, type Column } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { downloadCsv } from '../export'
import { useFy, fyLabel, currentFy } from '../fy'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import type { StageVelocitySet, VelocityPeriod, VelocityWave } from '../types'

const STAGES = ['1 · Validating', '2 · Pre-Requisites', '3 · Finalize Scope', '4 · Executing Migration']
const EXCLUDE_NON_STANDARD = 'Exclude Strategic Pilot (non-Standard)'
const TONE = { success: 'var(--colorPaletteGreenForeground1)', warning: 'var(--colorPaletteDarkOrangeForeground1)', danger: 'var(--colorPaletteRedForeground1)' }

// Velocity (allocated ÷ avg FDO Total Days) of waves completed in each period, by actual end date.
function VelocityTrendPanel({ title, quarters, months }: { title: string; quarters: VelocityPeriod[]; months: VelocityPeriod[] }) {
  const Row = ({ label, items }: { label: string; items: VelocityPeriod[] }) => (
    <div style={{ marginBottom: 10 }}>
      <div style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)', marginBottom: 4 }}>{label}</div>
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${items.length}, minmax(0, 1fr))`, gap: 8 }}>
        {items.map((p) => (
          <div key={p.key} style={{ border: `1px solid ${p.isCurrent ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)'}`, borderRadius: 6, padding: 8, textAlign: 'center' }}>
            <div style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)' }}>{p.label}{p.isCurrent ? ' · current' : ''}</div>
            <div style={{ fontSize: 20, fontWeight: 700, color: p.tone ? TONE[p.tone] : undefined }}>{p.velocityPct != null ? `${p.velocityPct}%` : '—'}</div>
            <div style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)' }}>
              {p.isFuture ? 'upcoming' : p.count ? `${p.count} waves · avg ${p.avgDays}d / ${p.target}d` : 'no completions'}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
  return (
    <Panel title="Average Velocity · completed waves (allocated ÷ avg FDO Total Days)">
      <Row label={title} items={quarters} />
      <Row label="Months" items={months} />
    </Panel>
  )
}

// FDO "Stage Duration vs SLA" cards: per-stage target vs average actual, days over/under, plus overall.
function StageVelocityPanel({ title, set }: { title: string; set: StageVelocitySet }) {
  const cards = [
    ...set.stages.map((s) => ({ key: String(s.stage), label: `${s.stage} · ${s.label}`, target: s.target, actual: s.actual, over: s.over })),
    { key: 'all', label: 'Overall completion', target: set.overallTarget, actual: set.overallActual, over: set.overallOver },
  ]
  return (
    <Panel title={`${title} · ${set.count} waves${set.overallVelocityPct != null ? ` · Average Velocity ${set.overallVelocityPct}%` : ''}`}>
      {set.count === 0 ? (
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No waves with an FDO stage-day breakdown match these filters.</Text>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, minmax(0, 1fr))', gap: 8 }}>
          {cards.map((c) => (
            <div key={c.key} style={{ border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6, padding: 8, textAlign: 'center' }}>
              <div style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)', minHeight: 28 }}>{c.label}</div>
              <div style={{ fontSize: 12 }}>Target <b>{c.target}</b></div>
              <div style={{ fontSize: 20, fontWeight: 700, color: c.over == null ? undefined : c.over > 0 ? TONE.danger : TONE.success }}>
                {c.actual ?? '—'}
              </div>
              {c.over != null && (
                <div style={{ fontSize: 11, color: c.over > 0 ? TONE.danger : TONE.success }}>
                  {c.over > 0 ? `${c.over} days over` : c.over < 0 ? `${-c.over} days under` : 'on target'}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </Panel>
  )
}

export function VelocityPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const [cls, setCls] = useState('')
  const [stage, setStage] = useState('')
  const [segment, setSegment] = useState('')
  const [path, setPath] = useState('')
  const [sa, setSa] = useState('')
  const [pm, setPm] = useState('')
  const [set, setSet] = useState('')

  const stageNo = stage ? STAGES.indexOf(stage) + 1 : undefined
  const excludeStrategic = cls === EXCLUDE_NON_STANDARD
  const classification = excludeStrategic ? '' : cls
  const { data, loading, error, reload } = useAsync(
    () => api.stageVelocity(region, { fy: fy === 'all' ? undefined : fy, excludeStrategic, classification, stage: stageNo, segment, path, sa, pm, set }),
    [region, fy, excludeStrategic, classification, stageNo, segment, path, sa, pm, set],
  )
  const waves = data?.waves ?? []
  const anyFilter = cls || stage || segment || path || sa || pm || set
  const reset = () => { setCls(''); setStage(''); setSegment(''); setPath(''); setSa(''); setPm(''); setSet('') }

  const columns: Column<VelocityWave>[] = [
    { key: 'account', header: 'Account', minWidth: 200, sortValue: (w) => w.accountName ?? '',
      render: (w) => <RouterLink to={`/nominations/${w.nominationId}`}>{w.accountName ?? '—'}</RouterLink> },
    { key: 'set', header: 'Set', sortValue: (w) => w.set },
    { key: 'stage', header: 'Stage', align: 'center', sortValue: (w) => w.stageIndex ?? 0 },
    { key: 'sa', header: 'SA', sortValue: (w) => w.sa ?? '' },
    { key: 'pm', header: 'PM', sortValue: (w) => w.pm ?? '' },
    { key: 'segment', header: 'Segment', sortValue: (w) => w.segment ?? '' },
    { key: 'path', header: 'Path', sortValue: (w) => w.path ?? '' },
    { key: 'days', header: 'S1 · S2 · S3 · S4', render: (w) => w.stageDays.map((d) => d ?? '–').join(' · ') },
    { key: 'total', header: 'Total days', align: 'end', sortValue: (w) => w.totalDays ?? -1 },
    { key: 'target', header: 'Allocated', align: 'end', sortValue: (w) => w.targetDays ?? -1 },
    { key: 'vel', header: 'Velocity', align: 'end', sortValue: (w) => w.velocityPct ?? -1,
      render: (w) => w.velocityPct != null
        ? <Badge appearance="tint" color={w.tone ?? 'informative'}>{w.velocityPct}%</Badge>
        : '—' },
    { key: 'end', header: 'Actual end', sortValue: (w) => w.actualEndDate ?? '' },
  ]

  const exportRows = () =>
    downloadCsv<VelocityWave>(`velocity-${new Date().toISOString().slice(0, 10)}`, [
      { header: 'Account', value: (w) => w.accountName ?? '' },
      { header: 'TPID', value: (w) => w.tpid ?? '' },
      { header: 'Set', value: (w) => w.set },
      { header: 'Stage', value: (w) => w.stageIndex ?? '' },
      { header: 'SA', value: (w) => w.sa ?? '' },
      { header: 'PM', value: (w) => w.pm ?? '' },
      { header: 'Segment', value: (w) => w.segment ?? '' },
      { header: 'Path', value: (w) => w.path ?? '' },
      { header: 'Classification', value: (w) => w.classification },
      { header: 'S1 days', value: (w) => w.stageDays[0] ?? '' },
      { header: 'S2 days', value: (w) => w.stageDays[1] ?? '' },
      { header: 'S3 days', value: (w) => w.stageDays[2] ?? '' },
      { header: 'S4 days', value: (w) => w.stageDays[3] ?? '' },
      { header: 'Total days', value: (w) => w.totalDays ?? '' },
      { header: 'Allocated days', value: (w) => w.targetDays ?? '' },
      { header: 'Velocity %', value: (w) => w.velocityPct ?? '' },
      { header: 'Actual end', value: (w) => w.actualEndDate ?? '' },
    ], waves)

  if (error) return <ErrorText error={error} onRetry={reload} />
  const o = data?.options
  const done = data?.waves.filter((w) => w.set === 'Completed') ?? []
  const atTarget = done.filter((w) => (w.velocityPct ?? 0) >= 100).length

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <Text size={600} weight="bold">Velocity Analytics</Text>
          <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
            Velocity = allocated stage days ÷ FDO Total Days (higher = faster) · {region || 'all regions'} · {fy === 'all' ? 'all FY' : fyLabel(fy)}
          </Text>
        </div>
        <Button appearance="primary" onClick={exportRows} disabled={!waves.length}>Export CSV</Button>
      </div>

      <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <FilterSelect label="Set" value={set} options={['In-flight', 'Completed']} onChange={setSet} minWidth={120} />
        <FilterSelect label="Stage" value={stage} options={(o?.stages ?? [1, 2, 3, 4]).map((s) => STAGES[s - 1])} onChange={setStage} />
        <FilterSelect label="Segment" value={segment} options={o?.segments ?? []} onChange={setSegment} />
        <FilterSelect label="Migration path" value={path} options={o?.paths ?? []} onChange={setPath} minWidth={180} />
        <FilterSelect label="SA" value={sa} options={o?.sas ?? []} onChange={setSa} minWidth={180} />
        <FilterSelect label="PM" value={pm} options={o?.pms ?? []} onChange={setPm} minWidth={180} />
        <FilterSelect
          label={`Classification${o ? ` · ${o.nonStandard} non-Standard` : ''}`}
          value={cls}
          options={[EXCLUDE_NON_STANDARD, ...(o?.classifications ?? [])]}
          onChange={setCls}
          minWidth={220}
        />
        {anyFilter && <Button size="small" onClick={reset}>Reset</Button>}
        {loading && data && <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>Updating…</Text>}
      </div>

      {loading && !data ? <Loading label="Calculating velocity…" /> : data && (
        <>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
            <KpiCard label="Total waves" value={String(data.inFlight.count + data.completed.count)} tone="brand" />
            <KpiCard label="In-flight" value={String(data.inFlight.count)} />
            <KpiCard label="Completed" value={String(data.completed.count)} tone="success" />
            <KpiCard label="Completed · Average Velocity" value={data.completed.overallVelocityPct != null ? `${data.completed.overallVelocityPct}%` : '—'}
              tone={data.completed.overallVelocityTone ?? 'neutral'} />
            <KpiCard label="In-flight · Average Velocity" value={data.inFlight.overallVelocityPct != null ? `${data.inFlight.overallVelocityPct}%` : '—'}
              tone={data.inFlight.overallVelocityTone ?? 'neutral'} />
            <KpiCard label="Completed avg days / allocated" value={data.completed.overallActual != null ? `${data.completed.overallActual}d / ${data.completed.overallTarget}d` : '—'} />
            <KpiCard label="Completed at/above target" value={done.length ? `${atTarget} · ${Math.round((atTarget / done.length) * 100)}%` : '—'} tone="brand" />
          </div>

          {(set === '' || set === 'In-flight') && <StageVelocityPanel title="In-flight" set={data.inFlight} />}
          {(set === '' || set === 'Completed') && <StageVelocityPanel title="Completed" set={data.completed} />}
          <VelocityTrendPanel title={`${fyLabel(fy === 'all' ? currentFy() : fy)} quarters`} quarters={data.byQuarter} months={data.byMonth} />

          <Panel title={`Waves · ${waves.length}`}>
            <DataTable
              columns={columns}
              rows={waves}
              rowKey={(w) => w.nominationId}
              ariaLabel="Wave velocity"
              defaultSort={{ key: 'vel', dir: 'asc' }}
            />
          </Panel>
        </>
      )}
    </div>
  )
}
