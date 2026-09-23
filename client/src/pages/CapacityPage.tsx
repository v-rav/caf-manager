import { SearchBox, Text } from '@fluentui/react-components'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel, StatusBadge, UtilizationBar } from '../components/common'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { CapacityRow } from '../types'

const heat: Record<string, string> = {
  Green: 'var(--colorPaletteGreenBackground2)',
  Amber: 'var(--colorPaletteMarigoldBackground2)',
  Red: 'var(--colorPaletteRedBackground2)',
}

const CAPACITY_STATUSES = ['Available', 'Partially Utilized', 'Fully Utilized', 'Overloaded']

export function CapacityPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.capacity(region), [region])
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const debouncedSearch = useDebounced(search)

  const rows = useMemo(
    () =>
      (data ?? []).filter(
        (c) =>
          (!status || c.capacityStatus === status) &&
          (!debouncedSearch || c.resourceName.toLowerCase().includes(debouncedSearch.toLowerCase())),
      ),
    [data, status, debouncedSearch],
  )

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Capacity Dashboard
      </Text>
      <Panel
        title={`Resource vs Accounts heatmap${rows ? ` (${rows.length})` : ''}`}
        action={
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <SearchBox placeholder="Search resource" value={search} onChange={(_, d) => setSearch(d.value)} style={{ minWidth: 180 }} />
            <FilterSelect label="Status" value={status} options={CAPACITY_STATUSES} onChange={setStatus} minWidth={170} />
          </div>
        }
      >
        {loading ? (
          <Loading />
        ) : error ? (
          <ErrorText error={error} onRetry={reload} />
        ) : (
          <DataTable<CapacityRow>
            ariaLabel="Capacity"
            rows={rows}
            rowKey={(c) => c.resourceId}
            defaultSort={{ key: 'utilization', dir: 'desc' }}
            rowStyle={(c) => (heat[c.heatColor] ? { background: heat[c.heatColor] } : undefined)}
            emptyMessage="No resources match your filters."
            columns={[
              { key: 'resource', header: 'Resource', sortValue: (c) => c.resourceName },
              { key: 'region', header: 'Region', sortValue: (c) => c.region },
              { key: 'role', header: 'Role', sortValue: (c) => c.role },
              { key: 'accounts', header: 'Accounts', align: 'center', sortValue: (c) => c.accountCount },
              { key: 'limit', header: 'Limit', align: 'center', sortValue: (c) => c.capacityLimit },
              { key: 'utilization', header: 'Utilization', minWidth: 140, sortValue: (c) => c.utilizationPercent, render: (c) => <UtilizationBar percent={c.utilizationPercent} /> },
              { key: 'status', header: 'Status', sortValue: (c) => c.capacityStatus, render: (c) => <StatusBadge status={c.capacityStatus} /> },
            ]}
          />
        )}
      </Panel>
    </div>
  )
}
