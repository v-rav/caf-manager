import { Button, SearchBox, Text } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
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

const BANDS: { status: string; bg: string }[] = [
  { status: 'Available', bg: 'var(--colorPaletteGreenBackground2)' },
  { status: 'Partially Utilized', bg: 'var(--colorPaletteMarigoldBackground2)' },
  { status: 'Fully Utilized', bg: 'var(--colorPaletteDarkOrangeBackground2)' },
  { status: 'Overloaded', bg: 'var(--colorPaletteRedBackground2)' },
]

export function CapacityPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.capacity(region), [region])
  const { data: clashes } = useAsync(() => api.leaveClashes(region), [region])
  const [searchParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState(searchParams.get('status') ?? '')
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

      {data && data.length > 0 && (
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {BANDS.map((b) => {
            const count = data.filter((c) => c.capacityStatus === b.status).length
            const active = status === b.status
            const toggle = () => setStatus(active ? '' : b.status)
            return (
              <div
                key={b.status}
                onClick={toggle}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle() } }}
                title={`Filter to ${b.status}`}
                style={{
                  cursor: 'pointer',
                  flex: '1 1 150px',
                  minWidth: 140,
                  borderRadius: 8,
                  padding: '10px 14px',
                  background: b.bg,
                  border: active ? '2px solid var(--colorNeutralForeground1)' : '1px solid var(--colorNeutralStroke2)',
                }}
              >
                <Text size={100} style={{ textTransform: 'uppercase', letterSpacing: 0.5, color: 'var(--colorNeutralForeground2)' }}>
                  {b.status}
                </Text>
                <div>
                  <Text size={700} weight="bold">
                    {count}
                  </Text>
                </div>
              </div>
            )
          })}
        </div>
      )}

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
            <Button as="a" href={api.exportUrl('capacity', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>
              Export
            </Button>
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
