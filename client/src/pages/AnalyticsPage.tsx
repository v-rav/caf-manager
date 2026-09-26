import { Button, Dropdown, Link, Option, Text } from '@fluentui/react-components'
import { useState } from 'react'
import { api } from '../api'
import { BarChart, DoughnutChart, TimeSeriesChart } from '../components/charts'
import { DataTable } from '../components/DataTable'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import type { Nomination, TimeBucket } from '../types'

// Heat-band colours reused for the health doughnut (On Track → Blocked → Other).
const HEALTH_COLORS = ['#107c10', '#eaa300', '#c50f1f', '#8a8886']

const money = (n: number) =>
  n >= 1e9 ? `$${(n / 1e9).toFixed(1)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`
const num = (n: number) => n.toLocaleString()
const pct = (a: number, b: number) => (b > 0 ? `${Math.round((a / b) * 100)}%` : '—')

// A titled section with a responsive grid of chart panels.
function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      <Text size={400} weight="semibold">{title}</Text>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 16 }}>
        {children}
      </div>
    </div>
  )
}

const BASIS_OPTS = [{ v: 'nominated', t: 'Nominated' }, { v: 'approved', t: 'Approved' }, { v: 'started', t: 'Started' }, { v: 'completed', t: 'Completed' }]
const GRAN_OPTS = [{ v: 'week', t: 'Weekly' }, { v: 'month', t: 'Monthly' }, { v: 'quarter', t: 'Fiscal Qtr' }, { v: 'year', t: 'Fiscal Year' }]
const MEASURE_OPTS = [{ v: 'count', t: 'Count' }, { v: 'acr', t: 'Total ACR' }, { v: 'nnr', t: 'NNR ACR' }, { v: 'cores', t: 'Cores' }]
const SPLIT_OPTS = [{ v: 'none', t: 'None' }, { v: 'region', t: 'Region' }, { v: 'segment', t: 'Segment' }, { v: 'path', t: 'Migration Path' }, { v: 'stage', t: 'Stage' }, { v: 'status', t: 'Status' }]
const BASIS_NOUN: Record<string, string> = { nominated: 'Nominations created', approved: 'Approvals', started: 'Migrations started', completed: 'Completions' }

// One labelled dropdown control for the Trends section.
function Pick({ label, value, options, onChange }: { label: string; value: string; options: { v: string; t: string }[]; onChange: (v: string) => void }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Text size={100} style={{ color: 'var(--colorNeutralForeground3)', textTransform: 'uppercase', letterSpacing: 0.4 }}>{label}</Text>
      <Dropdown
        size="small"
        value={options.find((o) => o.v === value)?.t ?? value}
        selectedOptions={[value]}
        onOptionSelect={(_, d) => onChange(d.optionValue ?? value)}
        style={{ minWidth: 130 }}
      >
        {options.map((o) => <Option key={o.v} value={o.v}>{o.t}</Option>)}
      </Dropdown>
    </div>
  )
}

// Configurable time-series: basis × granularity × measure × split, with an aggregated table and click-to-drill detail.
// Filter state is owned by the page so the page-level Export button can capture the live view.
type TrendState = {
  basis: string; setBasis: (v: string) => void
  granularity: string; setGranularity: (v: string) => void
  measure: string; setMeasure: (v: string) => void
  splitBy: string; setSplitBy: (v: string) => void
  fy: string; setFy: (v: string) => void
}
function TrendsSection({ region, basis, setBasis, granularity, setGranularity, measure, setMeasure, splitBy, setSplitBy, fy, setFy }: { region?: string } & TrendState) {
  const { data, loading } = useAsync(
    () => api.timeseries({ region, basis, granularity, measure, splitBy: splitBy === 'none' ? undefined : splitBy, fy: fy === 'all' ? undefined : fy }),
    [region, basis, granularity, measure, splitBy, fy],
  )
  const [detail, setDetail] = useState<{ label: string; rows: Nomination[] } | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)

  const fmt = (v: number) => (measure === 'acr' || measure === 'nnr' ? money(v) : num(v))
  const measureLabel = MEASURE_OPTS.find((o) => o.v === measure)?.t ?? 'Value'

  const openDetail = async (b: TimeBucket, series?: string) => {
    const label = b.label + (series && series !== 'All' ? ` · ${series}` : '')
    setDetailLoading(true)
    setDetail({ label, rows: [] })
    try {
      const rows = await api.timeseriesDetail({
        region, basis, granularity, bucket: b.key,
        splitBy: splitBy === 'none' ? undefined : splitBy,
        series: series && series !== 'All' ? series : undefined,
      })
      setDetail({ label, rows })
    } finally {
      setDetailLoading(false)
    }
  }

  const single = !data || data.series.length <= 1
  const fyOpts = [{ v: 'all', t: 'All FY' }, ...(data?.fiscalYears ?? []).map((y) => ({ v: String(y), t: `FY${String(y % 100).padStart(2, '0')}` }))]
  const bucketCols = single
    ? [
        { key: 'period', header: 'Period', sortValue: (b: TimeBucket) => b.key, render: (b: TimeBucket) => <Link onClick={() => openDetail(b)}>{b.label}</Link> },
        { key: 'value', header: measureLabel, align: 'end' as const, sortValue: (b: TimeBucket) => b.total, render: (b: TimeBucket) => fmt(b.total) },
      ]
    : [
        { key: 'period', header: 'Period', sortValue: (b: TimeBucket) => b.key, render: (b: TimeBucket) => <Link onClick={() => openDetail(b)}>{b.label}</Link> },
        ...(data?.series ?? []).map((s: string) => ({ key: s, header: s, align: 'end' as const, sortValue: (b: TimeBucket) => b.values.find((v) => v.name === s)?.value ?? 0, render: (b: TimeBucket) => fmt(b.values.find((v) => v.name === s)?.value ?? 0) })),
        { key: 'total', header: 'Total', align: 'end' as const, sortValue: (b: TimeBucket) => b.total, render: (b: TimeBucket) => fmt(b.total) },
      ]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <Text size={400} weight="semibold">Trends over time</Text>
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <Pick label="Date basis" value={basis} options={BASIS_OPTS} onChange={setBasis} />
          <Pick label="Fiscal year" value={fy} options={fyOpts} onChange={setFy} />
          <Pick label="Granularity" value={granularity} options={GRAN_OPTS} onChange={setGranularity} />
          <Pick label="Measure" value={measure} options={MEASURE_OPTS} onChange={setMeasure} />
          <Pick label="Split by" value={splitBy} options={SPLIT_OPTS} onChange={setSplitBy} />
        </div>
      </div>
      <Panel title={data ? `${BASIS_NOUN[basis] ?? basis} · ${measureLabel} · ${data.buckets.length} buckets · total ${fmt(data.total)}${data.recordsWithoutDate ? ` · ${data.recordsWithoutDate} without a ${basis} date` : ''}` : 'Trend'}>
        {loading ? <Loading /> : !data ? null : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <TimeSeriesChart buckets={data.buckets} series={data.series} />
            <DataTable<TimeBucket> ariaLabel="Trend buckets" rows={data.buckets} rowKey={(b) => b.key} defaultSort={{ key: 'period', dir: 'asc' }} emptyMessage="No data for this basis." columns={bucketCols} />
          </div>
        )}
      </Panel>
      {detail && (
        <Panel
          title={`Detail — ${detail.label} (${detail.rows.length})`}
          action={<Button size="small" appearance="subtle" onClick={() => setDetail(null)}>Close</Button>}
        >
          {detailLoading ? <Loading /> : (
            <DataTable<Nomination>
              ariaLabel="Bucket detail"
              rows={detail.rows}
              rowKey={(n) => n.id}
              defaultSort={{ key: 'end', dir: 'desc' }}
              emptyMessage="No records."
              columns={[
                { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => n.accountName ?? '\u2014' },
                { key: 'tpid', header: 'TPID', sortValue: (n) => n.tpid ?? '', render: (n) => n.tpid ?? '\u2014' },
                { key: 'status', header: 'Status', sortValue: (n) => n.status },
                { key: 'nominated', header: 'Nominated', sortValue: (n) => n.nominatedDate ?? '', render: (n) => n.nominatedDate ?? '\u2014' },
                { key: 'end', header: 'Completed', sortValue: (n) => n.actualEndDate ?? '', render: (n) => n.actualEndDate ?? '\u2014' },
                { key: 'acr', header: 'ACR', align: 'end', sortValue: (n) => n.totalAcr ?? 0, render: (n) => (n.totalAcr != null ? money(n.totalAcr) : '\u2014') },
              ]}
            />
          )}
        </Panel>
      )}
    </div>
  )
}

export function AnalyticsPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.analytics(region), [region])

  const [basis, setBasis] = useState('completed')
  const [granularity, setGranularity] = useState('month')
  const [measure, setMeasure] = useState('count')
  const [splitBy, setSplitBy] = useState('none')
  // Default the FY filter to the current fiscal year (Jul 1 start, end-year label).
  const [fy, setFy] = useState(() => {
    const d = new Date()
    return String(d.getMonth() + 1 >= 7 ? d.getFullYear() + 1 : d.getFullYear())
  })
  const exportHref = api.exportUrl('analytics', region, {
    basis, granularity, measure,
    splitBy: splitBy === 'none' ? undefined : splitBy,
    fy: fy === 'all' ? undefined : fy,
  })

  if (loading) return <Loading label="Crunching migration analytics\u2026" />
  if (error) return <ErrorText error={error} onRetry={reload} />
  if (!data) return null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <Text size={600} weight="bold">Migration Analytics</Text>
          <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
            Approved nominations{region ? ` \u00b7 ${region}` : ' \u00b7 all regions'} \u00b7 {data.totalApproved} total ({data.inFlight} in-flight,
            {' '}{data.completed} completed) \u00b7 ACR/Cores from {data.withAcr} enriched records
          </Text>
        </div>
        <Button as="a" appearance="primary" href={exportHref}>Export</Button>
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="In-flight" value={num(data.inFlight)} tone="brand" />
        <KpiCard label="Total ACR (approved)" value={money(data.totalAcr)} tone="success" />
        <KpiCard label="NNR ACR (landed)" value={money(data.nnrAcr)} tone="success" />
        <KpiCard label="Total Cores" value={num(data.totalCores)} tone="neutral" />
        <KpiCard label="Tool attached" value={`${data.toolAttached} · ${pct(data.toolAttached, data.toolFlagDenom)}`} tone="brand" />
        <KpiCard label="Automation used" value={`${data.automationUsed} · ${pct(data.automationUsed, data.toolFlagDenom)}`} tone="brand" />
      </div>

      <TrendsSection
        region={region}
        basis={basis} setBasis={setBasis}
        granularity={granularity} setGranularity={setGranularity}
        measure={measure} setMeasure={setMeasure}
        splitBy={splitBy} setSplitBy={setSplitBy}
        fy={fy} setFy={setFy}
      />

      <Section title="Pipeline health">
        <Panel title="Nominations by stage"><BarChart data={data.byStage} label="Nominations" /></Panel>
        <Panel title="Current-state health"><DoughnutChart data={data.byHealth} colors={HEALTH_COLORS} /></Panel>
        <Panel title="SLA stale tiers (stage 2–4)"><BarChart data={data.bySla} label="Nominations" /></Panel>
      </Section>

      <Section title="Value & scale">
        <Panel title="ACR by region"><BarChart data={data.acrByRegion} label="ACR ($)" /></Panel>
        <Panel title="ACR by segment"><BarChart data={data.acrBySegment} label="ACR ($)" /></Panel>
        <Panel title="Top migration paths by ACR"><BarChart data={data.acrByMigrationPath} label="ACR ($)" /></Panel>
        <Panel title="Cores by stage"><BarChart data={data.coresByStage} label="Cores" /></Panel>
      </Section>

      <Section title="Adoption & coverage">
        <Panel title="Migration path mix"><DoughnutChart data={data.byMigrationPath} /></Panel>
        <Panel title="Mode of access"><DoughnutChart data={data.byModeOfAccess} /></Panel>
        <Panel title="Wave linkage"><BarChart data={data.waveLinkage} label="Nominations" /></Panel>
        <Panel title="Top partners by ACR"><BarChart data={data.topPartnersByAcr} label="ACR ($)" /></Panel>
      </Section>

      <Section title="Distribution">
        <Panel title="Nominations by region"><DoughnutChart data={data.byRegion} /></Panel>
        <Panel title="Nominations by segment"><DoughnutChart data={data.bySegment} /></Panel>
      </Section>
    </div>
  )
}
