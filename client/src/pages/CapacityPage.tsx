import { SearchBox, Text } from '@fluentui/react-components'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel, StatusBadge, UtilizationBar } from '../components/common'
import { KpiCard } from '../components/KpiCard'
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
  const { data: clashes } = useAsync(() => api.leaveClashes(region), [region])
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const debouncedSearch = useDebounced(search)

  const overloaded = (data ?? []).filter((c) => c.capacityStatus === 'Overloaded')

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

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
        <KpiCard label="Overloaded resources" value={overloaded.length} tone={overloaded.length ? 'danger' : 'success'} />
        <KpiCard label="Leave-clash risks" value={clashes?.length ?? 0} tone={(clashes?.length ?? 0) ? 'warning' : 'success'} />
      </div>

      {overloaded.length > 0 && (
        <Panel title={`Overloaded — needs rebalancing (${overloaded.length})`}>
          <DataTable<CapacityRow>
            ariaLabel="Overloaded resources"
            rows={overloaded}
            rowKey={(c) => c.resourceId}
            defaultSort={{ key: 'utilization', dir: 'desc' }}
            emptyMessage="None."
            columns={[
              { key: 'resource', header: 'Resource', sortValue: (c) => c.resourceName },
              { key: 'region', header: 'Region', sortValue: (c) => c.region },
              { key: 'role', header: 'Role', sortValue: (c) => c.role },
              { key: 'accounts', header: 'Accounts', align: 'center', sortValue: (c) => c.accountCount },
              { key: 'limit', header: 'Limit', align: 'center', sortValue: (c) => c.capacityLimit },
              { key: 'utilization', header: 'Utilization', minWidth: 140, sortValue: (c) => c.utilizationPercent, render: (c) => <UtilizationBar percent={c.utilizationPercent} /> },
            ]}
          />
        </Panel>
      )}

      {clashes && clashes.length > 0 && (
        <Panel title={`Leave-clash alerts — resources with active accounts on upcoming leave (${clashes.length})`}>
          <DataTable
            ariaLabel="Leave clashes"
            rows={clashes}
            rowKey={(c) => c.resourceId}
            defaultSort={{ key: 'accounts', dir: 'desc' }}
            emptyMessage="No clashes."
            columns={[
              { key: 'resource', header: 'Resource', sortValue: (c) => c.resourceName },
              { key: 'region', header: 'Region', sortValue: (c) => c.region },
              { key: 'accounts', header: 'Active accounts', align: 'center', sortValue: (c) => c.activeAccounts },
              { key: 'start', header: 'Leave from', sortValue: (c) => c.nextLeaveStart },
              { key: 'end', header: 'Leave to', sortValue: (c) => c.nextLeaveEnd },
              { key: 'days', header: 'Days in window', align: 'center', sortValue: (c) => c.leaveDaysInWindow },
            ]}
          />
        </Panel>
      )}

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
