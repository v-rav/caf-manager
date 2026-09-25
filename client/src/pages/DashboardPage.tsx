import { Text } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { Button } from '@fluentui/react-components'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { BarChart, DoughnutChart } from '../components/charts'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'

export function DashboardPage() {
  const { region } = useRegion()
  const navigate = useNavigate()
  const { data, loading, error, reload } = useAsync(() => api.dashboard(region), [region])

  if (loading) return <Loading label="Loading executive dashboard…" />
  if (error) return <ErrorText error={error} onRetry={reload} />
  if (!data) return null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <Text size={600} weight="bold">
          Executive Dashboard
        </Text>
        <Button as="a" href={api.exportUrl('summary', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>
          Executive summary
        </Button>
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Total Resources" value={data.totalResources} tone="brand" onClick={() => navigate('/resources')} />
        <KpiCard label="Active Accounts" value={data.activeAccounts} onClick={() => navigate('/accounts')} />
        <KpiCard label="Available" value={data.availableResources} tone="success" onClick={() => navigate('/resources?capacity=Available')} />
        <KpiCard label="Partially Utilized" value={data.partiallyUtilizedResources} tone="warning" onClick={() => navigate('/resources?capacity=Partially%20Utilized')} />
        <KpiCard label="Fully Utilized" value={data.fullyUtilizedResources} tone="warning" onClick={() => navigate('/resources?capacity=Fully%20Utilized')} />
        <KpiCard label="Overloaded" value={data.overloadedResources} tone="danger" onClick={() => navigate('/capacity?status=Overloaded')} />
        <KpiCard label="On Leave" value={data.resourcesOnLeave} onClick={() => navigate('/leave')} />
        <KpiCard label="Strategic Accounts" value={data.strategicAccounts} tone="brand" />
        <KpiCard label="Open Nominations" value={data.openNominations} onClick={() => navigate('/nominations')} />
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
