import { Text } from '@fluentui/react-components'
import { api } from '../api'
import { BarChart, DoughnutChart } from '../components/charts'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'

export function DashboardPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.dashboard(region), [region])

  if (loading) return <Loading label="Loading executive dashboard…" />
  if (error) return <ErrorText error={error} onRetry={reload} />
  if (!data) return null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <Text size={600} weight="bold">
        Executive Dashboard
      </Text>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Total Resources" value={data.totalResources} tone="brand" />
        <KpiCard label="Active Accounts" value={data.activeAccounts} />
        <KpiCard label="Available" value={data.availableResources} tone="success" />
        <KpiCard label="Partially Utilized" value={data.partiallyUtilizedResources} tone="warning" />
        <KpiCard label="Fully Utilized" value={data.fullyUtilizedResources} tone="warning" />
        <KpiCard label="Overloaded" value={data.overloadedResources} tone="danger" />
        <KpiCard label="On Leave" value={data.resourcesOnLeave} />
        <KpiCard label="Strategic Accounts" value={data.strategicAccounts} tone="brand" />
        <KpiCard label="Open Nominations" value={data.openNominations} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 16 }}>
        <Panel title="Regional Distribution">
          <DoughnutChart data={data.regionDistribution} />
        </Panel>
        <Panel title="Capacity Distribution">
          <DoughnutChart data={data.capacityDistribution} />
        </Panel>
        <Panel title="Strategic Account Coverage">
          <BarChart data={data.strategicAccountCoverage} label="Assigned resources" />
        </Panel>
      </div>
    </div>
  )
}
