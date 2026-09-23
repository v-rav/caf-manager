import { SearchBox, Text } from '@fluentui/react-components'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import { useMemo, useState } from 'react'
import type { Nomination } from '../types'

// The migration journey has four stages; the summary counts how many nominations sit in each.
const STAGES = [
  { n: 1, label: 'Validating', match: 'validating', tone: 'neutral' as const },
  { n: 2, label: 'Pre-Requisites', match: 'pre-requisite', tone: 'warning' as const },
  { n: 3, label: 'Finalize Scope', match: 'finalize', tone: 'warning' as const },
  { n: 4, label: 'Executing Migration', match: 'executing migration', tone: 'brand' as const },
]

export function NominationsPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const [currentStateFilter, setCurrentStateFilter] = useState('')
  const [migrationFilter, setMigrationFilter] = useState('')
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounced(search)

  const stageCount = (match: string) =>
    (data ?? []).filter((n) => (n.migrationStatus ?? '').toLowerCase().includes(match)).length

  // A nomination is "blocked" when its current state starts with "Blocked - ...".
  const blockedInStage = (match: string) =>
    (data ?? []).filter(
      (n) =>
        (n.migrationStatus ?? '').toLowerCase().includes(match) &&
        (n.currentState ?? '').toLowerCase().includes('blocked'),
    ).length

  const migrationOptions = useMemo(
    () => [...new Set((data ?? []).map((n) => n.migrationStatus).filter((v): v is string => !!v))].sort(),
    [data],
  )
  const currentStateOptions = useMemo(
    () => [...new Set((data ?? []).map((n) => n.currentState).filter((v): v is string => !!v))].sort(),
    [data],
  )
  const rows = useMemo(
    () =>
      (data ?? []).filter(
        (n) =>
          (!currentStateFilter || n.currentState === currentStateFilter) &&
          (!migrationFilter || n.migrationStatus === migrationFilter) &&
          (!debouncedSearch || (n.accountName ?? '').toLowerCase().includes(debouncedSearch.toLowerCase())),
      ),
    [data, currentStateFilter, migrationFilter, debouncedSearch],
  )

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Nomination Pipeline
      </Text>

      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <KpiCard label="Total" value={data?.length ?? 0} tone="brand" />
            {STAGES.map((s) => (
              <KpiCard key={s.n} label={`Stage ${s.n} · ${s.label}`} value={stageCount(s.match)} tone={s.tone} blocked={blockedInStage(s.match)} />
            ))}
          </div>
          <Panel
            title={`Nominations${rows ? ` (${rows.length})` : ''}`}
            action={
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                <SearchBox
                  placeholder="Search account"
                  value={search}
                  onChange={(_, d) => setSearch(d.value)}
                  style={{ minWidth: 200 }}
                />
                <FilterSelect label="Current State" value={currentStateFilter} options={currentStateOptions} onChange={setCurrentStateFilter} minWidth={200} />
                <FilterSelect label="Migration" value={migrationFilter} options={migrationOptions} onChange={setMigrationFilter} minWidth={200} />
              </div>
            }
          >
            <DataTable<Nomination>
              ariaLabel="Nominations"
              rows={rows}
              rowKey={(n) => n.id}
              defaultSort={{ key: 'opened', dir: 'desc' }}
              emptyMessage="No nominations match your filters."
              columns={[
                { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => n.accountName ?? '—' },
                { key: 'offering', header: 'Offering', sortValue: (n) => n.technology ?? '', render: (n) => n.technology ?? '—' },
                { key: 'region', header: 'Region', sortValue: (n) => n.region },
                { key: 'opened', header: 'Opened', sortValue: (n) => n.openedDate },
                { key: 'migration', header: 'Migration Status', sortValue: (n) => n.migrationStatus ?? '', render: (n) => n.migrationStatus ?? '—' },
                { key: 'sa', header: 'SA', sortValue: (n) => n.solutionArchitect ?? '', render: (n) => n.solutionArchitect ?? '—' },
                { key: 'cftl', header: 'CFTL', sortValue: (n) => n.cftlPrimary ?? '', render: (n) => n.cftlPrimary ?? '—' },
                { key: 'pm', header: 'PM', sortValue: (n) => n.projectCoordinator ?? '', render: (n) => n.projectCoordinator ?? '—' },
                {
                  key: 'state',
                  header: 'Current State',
                  sortValue: (n) => n.currentState ?? '',
                  render: (n) => {
                    const blocked = (n.currentState ?? '').toLowerCase().includes('blocked')
                    return (
                      <span
                        title={n.currentState ?? ''}
                        style={{
                          display: 'inline-block',
                          maxWidth: 220,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                          verticalAlign: 'bottom',
                          color: blocked ? 'var(--colorPaletteRedForeground1)' : undefined,
                          fontWeight: blocked ? 600 : undefined,
                        }}
                      >
                        {n.currentState ?? '—'}
                      </span>
                    )
                  },
                },
              ]}
            />
          </Panel>
        </>
      )}
    </div>
  )
}
