import { Badge, Button, Input, Text, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular, ArrowClockwiseRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { ReconciliationRow, SeedAssignmentResult, UnmatchedPerson } from '../types'

const MATCH_TONE: Record<string, 'success' | 'warning' | 'danger'> = {
  Master: 'success',
  SuggestMerge: 'warning',
  Orphan: 'danger',
}
const MATCH_LABEL: Record<string, string> = {
  Master: 'Master',
  SuggestMerge: 'Suggest merge',
  Orphan: 'Orphan',
}

export function ReconciliationPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.reconciliation(region), [region])
  const { data: gaps } = useAsync(() => api.unmatchedPeople(region), [region])
  const [matchFilter, setMatchFilter] = useState('')
  const [utilFilter, setUtilFilter] = useState('')
  const [search, setSearch] = useState('')
  const debounced = useDebounced(search)
  // Seed flow: preview (read-only) then apply.
  const [seedPreview, setSeedPreview] = useState<SeedAssignmentResult | null>(null)
  const [seeding, setSeeding] = useState(false)

  const previewSeed = async () => {
    setSeeding(true)
    try {
      setSeedPreview(await api.seedAssignments(region, false))
    } finally {
      setSeeding(false)
    }
  }
  const applySeed = async () => {
    setSeeding(true)
    try {
      await api.seedAssignments(region, true)
      setSeedPreview(null)
      await reload()
    } finally {
      setSeeding(false)
    }
  }

  const summary = data?.summary
  const rows = useMemo(() => {
    const all = data?.rows ?? []
    const q = debounced.toLowerCase()
    return all.filter(
      (r) =>
        (!matchFilter || r.matchState === matchFilter) &&
        (!utilFilter || r.utilizationEffect === utilFilter) &&
        (!q || r.resourceName.toLowerCase().includes(q) || r.accountName.toLowerCase().includes(q)),
    )
  }, [data, matchFilter, utilFilter, debounced])

  if (loading) return <Loading />
  if (error) return <ErrorText error={String(error)} />

  const toggleMatch = (m: string) => setMatchFilter(matchFilter === m ? '' : m)
  const toggleUtil = (u: string) => setUtilFilter(utilFilter === u ? '' : u)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
        <div>
          <Text weight="bold" size={600} style={{ display: 'block' }}>Resource ↔ Account Reconciliation</Text>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            Read-only data-quality view of the mappings that feed Capacity. Nothing here changes data — use it to
            decide which links to keep before cleansing. In-flight = an Approved nomination that isn't
            Closed/Completed/Withdrawn/Deferred.
          </Text>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <Button appearance="secondary" icon={<ArrowClockwiseRegular />} onClick={reload}>Refresh</Button>
          <Button appearance="secondary" onClick={previewSeed} disabled={seeding}>Seed assignments</Button>
          <Button as="a" href={api.exportUrl('reconciliation', region)} appearance="primary" icon={<ArrowDownloadRegular />}>
            Export
          </Button>
        </div>
      </div>

      {seedPreview && (
        <div
          style={{
            border: '1px solid var(--colorNeutralStroke2)',
            background: 'var(--colorNeutralBackground2)',
            borderRadius: 6,
            padding: '10px 14px',
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            flexWrap: 'wrap',
          }}
        >
          <Text size={300}>
            Will create <b>{seedPreview.wouldCreate}</b> assignment(s) — <b>{seedPreview.saCreated}</b> SA (from the FDO field, one per wave) and{' '}
            <b>{seedPreview.engineerCreated}</b> engineer(s) — and replace <b>{seedPreview.removedSeed}</b> previously seeded row(s).{' '}
            {seedPreview.saUnmatchedWaves > 0 && (
              <>
                <b>{seedPreview.saUnmatchedWaves}</b> wave(s) have an FDO SA with no matching resource (no SA seeded).{' '}
              </>
            )}
            Manual (Portal) assignments and all nominations are left untouched.
          </Text>
          <div style={{ display: 'flex', gap: 8, marginLeft: 'auto' }}>
            <Button appearance="secondary" onClick={() => setSeedPreview(null)} disabled={seeding}>Cancel</Button>
            <Button appearance="primary" onClick={applySeed} disabled={seeding || (seedPreview.wouldCreate === 0 && seedPreview.removedSeed === 0)}>
              {seeding ? 'Applying…' : 'Apply'}
            </Button>
          </div>
        </div>
      )}

      {summary && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 12 }}>
          <KpiCard label="Total links" value={summary.totalLinks} tone="neutral" />
          <KpiCard label="Orphan (no TPID match)" value={summary.orphan} tone="danger"
            onClick={() => toggleMatch('Orphan')} />
          <KpiCard label="Suggest merge" value={summary.suggestMerge} tone="warning"
            onClick={() => toggleMatch('SuggestMerge')} />
          <KpiCard label="Master (has TPID)" value={summary.master} tone="success"
            onClick={() => toggleMatch('Master')} />
          <KpiCard label="Keep (in-flight)" value={summary.keep} tone="success"
            onClick={() => toggleUtil('Keep')} />
          <KpiCard label="Drop (not in-flight)" value={summary.drop} tone="danger"
            onClick={() => toggleUtil('Drop')} />
        </div>
      )}

      <Panel title="Mappings">
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center', marginBottom: 10 }}>
          <FilterSelect label="Match" value={matchFilter} options={['Orphan', 'SuggestMerge', 'Master']} onChange={setMatchFilter} />
          <FilterSelect label="Utilization" value={utilFilter} options={['Keep', 'Drop']} onChange={setUtilFilter} />
          <Input placeholder="Search resource or account…" value={search} onChange={(_, d) => setSearch(d.value)} style={{ minWidth: 240 }} />
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', marginLeft: 'auto' }}>
            {rows.length} of {summary?.totalLinks ?? 0} links
          </Text>
        </div>

        <DataTable<ReconciliationRow>
          ariaLabel="Reconciliation"
          rows={rows}
          rowKey={(r) => `${r.resourceId}-${r.accountId}`}
          pageSize={50}
          columns={[
            { key: 'resource', header: 'Resource', sortValue: (r) => r.resourceName, render: (r) => r.resourceName },
            { key: 'region', header: 'Region', sortValue: (r) => r.resourceRegion, render: (r) => r.resourceRegion },
            { key: 'account', header: 'Account (linked)', sortValue: (r) => r.accountName, render: (r) => r.accountName },
            { key: 'tpid', header: 'TPID', sortValue: (r) => r.tpid ?? '', render: (r) => r.tpid ?? '—' },
            { key: 'segment', header: 'Segment', sortValue: (r) => r.segment ?? '', render: (r) => r.segment ?? '—' },
            {
              key: 'inflight',
              header: 'In-flight',
              sortValue: (r) => (r.inFlight ? 1 : 0),
              align: 'center',
              render: (r) => (
                <Badge appearance="tint" color={r.inFlight ? 'success' : 'informative'} size="small">
                  {r.inFlight ? 'Yes' : 'No'}
                </Badge>
              ),
            },
            {
              key: 'match',
              header: 'Match',
              sortValue: (r) => r.matchState,
              render: (r) => (
                <Badge appearance="tint" color={MATCH_TONE[r.matchState] ?? 'informative'} size="small">
                  {MATCH_LABEL[r.matchState] ?? r.matchState}
                </Badge>
              ),
            },
            {
              key: 'suggested',
              header: 'Suggested master',
              sortValue: (r) => r.suggestedAccountName ?? '',
              render: (r) =>
                r.suggestedAccountName ? (
                  <Tooltip relationship="description" content={`TPID ${r.suggestedTpid ?? '—'}`}>
                    <span style={{ cursor: 'help' }}>{r.suggestedAccountName}</span>
                  </Tooltip>
                ) : (
                  '—'
                ),
            },
            {
              key: 'util',
              header: 'Utilization',
              sortValue: (r) => r.utilizationEffect,
              align: 'center',
              render: (r) => (
                <Badge appearance="filled" color={r.utilizationEffect === 'Keep' ? 'success' : 'danger'} size="small">
                  {r.utilizationEffect}
                </Badge>
              ),
            },
          ]}
        />
      </Panel>

      <Panel title="FDO people not in the roster">
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 10 }}>
          Named in an FDO ownership field (SA / PM / CFTL) on in-flight waves but not a portal resource, so their
          work isn't tracked. Add them on the Resource Hub (use an alias if the spelling differs) to close the gap.
          {gaps ? ` ${gaps.totalPeople} people across ${gaps.totalReferences} references.` : ''}
        </Text>
        <DataTable<UnmatchedPerson>
          ariaLabel="FDO people not in the roster"
          rows={gaps?.people ?? []}
          rowKey={(p) => `${p.role}-${p.name}`}
          pageSize={25}
          columns={[
            { key: 'name', header: 'Name (FDO)', sortValue: (p) => p.name, render: (p) => p.name },
            {
              key: 'role',
              header: 'Role',
              sortValue: (p) => p.role,
              render: (p) => (
                <Badge appearance="tint" color={p.role.startsWith('Solution') ? 'brand' : 'informative'} size="small">
                  {p.role}
                </Badge>
              ),
            },
            { key: 'waves', header: 'Waves', sortValue: (p) => p.waveCount, align: 'center', render: (p) => p.waveCount },
            { key: 'regions', header: 'Regions', sortValue: (p) => p.regions, render: (p) => p.regions || '—' },
            {
              key: 'accounts',
              header: 'Sample accounts',
              sortValue: (p) => p.sampleAccounts,
              render: (p) => p.sampleAccounts || '—',
            },
          ]}
        />
      </Panel>
    </div>
  )
}
