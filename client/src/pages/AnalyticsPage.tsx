import { Text } from '@fluentui/react-components'
import { api } from '../api'
import { BarChart, DoughnutChart } from '../components/charts'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'

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

export function AnalyticsPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.analytics(region), [region])

  if (loading) return <Loading label="Crunching migration analytics…" />
  if (error) return <ErrorText error={error} onRetry={reload} />
  if (!data) return null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <Text size={600} weight="bold">Migration Analytics</Text>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', marginTop: -12 }}>
        Approved nominations{region ? ` · ${region}` : ' · all regions'} · {data.totalApproved} total ({data.inFlight} in-flight,
        {' '}{data.completed} completed) · ACR/Cores from {data.withAcr} enriched records
      </Text>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="In-flight" value={num(data.inFlight)} tone="brand" />
        <KpiCard label="Total ACR (approved)" value={money(data.totalAcr)} tone="success" />
        <KpiCard label="NNR ACR (landed)" value={money(data.nnrAcr)} tone="success" />
        <KpiCard label="Total Cores" value={num(data.totalCores)} tone="neutral" />
        <KpiCard label="Tool attached" value={`${data.toolAttached} · ${pct(data.toolAttached, data.toolFlagDenom)}`} tone="brand" />
        <KpiCard label="Automation used" value={`${data.automationUsed} · ${pct(data.automationUsed, data.toolFlagDenom)}`} tone="brand" />
      </div>

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
