import { SearchBox, Text } from '@fluentui/react-components'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel, StatusBadge } from '../components/common'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { StrategicAccount } from '../types'

const RISKS = ['Green', 'Amber', 'Red']
const priority = (w: number) => (w >= 3 ? 'Executive Critical' : w === 2 ? 'Strategic' : 'Standard')

export function StrategicAccountsPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.strategicAccounts(region), [region])
  const [search, setSearch] = useState('')
  const [risk, setRisk] = useState('')
  const debouncedSearch = useDebounced(search)

  const rows = useMemo(
    () =>
      (data ?? []).filter(
        (s) =>
          (!risk || s.riskIndicator === risk) &&
          (!debouncedSearch || s.accountName.toLowerCase().includes(debouncedSearch.toLowerCase())),
      ),
    [data, risk, debouncedSearch],
  )

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Strategic Accounts Dashboard
      </Text>
      <Panel
        title={`Coverage & risk${rows ? ` (${rows.length})` : ''}`}
        action={
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <SearchBox placeholder="Search account" value={search} onChange={(_, d) => setSearch(d.value)} style={{ minWidth: 180 }} />
            <FilterSelect label="Risk" value={risk} options={RISKS} onChange={setRisk} minWidth={130} />
          </div>
        }
      >
        {loading ? (
          <Loading />
        ) : error ? (
          <ErrorText error={error} onRetry={reload} />
        ) : (
          <DataTable<StrategicAccount>
            ariaLabel="Strategic accounts"
            rows={rows}
            rowKey={(s) => s.accountId}
            defaultSort={{ key: 'priority', dir: 'desc' }}
            emptyMessage="No strategic accounts match your filters."
            columns={[
              { key: 'account', header: 'Account', sortValue: (s) => s.accountName },
              { key: 'region', header: 'Region', sortValue: (s) => s.region },
              { key: 'priority', header: 'Priority', sortValue: (s) => s.priorityWeight, render: (s) => priority(s.priorityWeight) },
              { key: 'assigned', header: 'Assigned', align: 'center', sortValue: (s) => s.assignedResourceCount },
              { key: 'activity', header: 'Recent Activity', align: 'center', sortValue: (s) => s.recentActivityCount },
              { key: 'last', header: 'Last Activity', sortValue: (s) => s.lastActivityDate ?? '', render: (s) => s.lastActivityDate ?? '—' },
              { key: 'risk', header: 'Risk', sortValue: (s) => s.riskIndicator, render: (s) => <StatusBadge status={s.riskIndicator} /> },
            ]}
          />
        )}
      </Panel>
    </div>
  )
}
