import { Badge, Button, Text } from '@fluentui/react-components'
import { ArrowClockwiseRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { ImportChange, ImportRun } from '../types'

function fmt(iso: string) {
  return new Date(iso).toLocaleString()
}

const typeTone: Record<string, 'success' | 'informative' | 'warning'> = {
  Added: 'success',
  Updated: 'informative',
  Withdrawn: 'warning',
}

function Stat({ label, value, tone }: { label: string; value: number; tone?: 'success' | 'informative' | 'warning' }) {
  const color =
    tone === 'success' ? 'var(--colorPaletteGreenForeground1)'
    : tone === 'warning' ? 'var(--colorPaletteDarkOrangeForeground1)'
    : tone === 'informative' ? 'var(--colorBrandForeground1)'
    : 'var(--colorNeutralForeground1)'
  return (
    <div style={{ minWidth: 96, padding: '8px 14px', border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6 }}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block' }}>{label}</Text>
      <Text size={600} weight="bold" style={{ color }}>{value}</Text>
    </div>
  )
}

export function HistoryPage() {
  const { data: runs, loading, reload } = useAsync(() => api.imports(100), [])
  const [selected, setSelected] = useState<ImportRun | null>(null)
  const { data: changes } = useAsync(
    () => (selected ? api.importChanges(selected.id) : Promise.resolve([])),
    [selected?.id],
  )

  if (loading) return <Loading />

  const latest = runs?.[0]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
        <div>
          <Text weight="bold" size={600} style={{ display: 'block' }}>Import History</Text>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            What each data upload changed. Nominations are never hard-deleted — removed rows are marked Withdrawn.
          </Text>
        </div>
        <Button appearance="secondary" icon={<ArrowClockwiseRegular />} onClick={reload}>Refresh</Button>
      </div>

      {latest && (
        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
          <Stat label="Last import" value={latest.added + latest.updated + latest.withdrawn} />
          <Stat label="Added" value={latest.added} tone="success" />
          <Stat label="Updated" value={latest.updated} tone="informative" />
          <Stat label="Withdrawn" value={latest.withdrawn} tone="warning" />
          <Stat label="Unchanged" value={latest.unchanged} />
        </div>
      )}

      <Panel title="Imports">
        <DataTable<ImportRun>
          ariaLabel="Import runs"
          rows={runs ?? []}
          rowKey={(r) => r.id}
          emptyMessage="No imports yet. Upload an FDO file from the Nominations page."
          rowStyle={(r) => (selected?.id === r.id ? { background: 'var(--colorBrandBackground2)' } : undefined)}
          columns={[
            { key: 'when', header: 'When', sortValue: (r) => r.startedUtc, render: (r) => fmt(r.startedUtc) },
            { key: 'source', header: 'Source', sortValue: (r) => r.source },
            { key: 'file', header: 'File', sortValue: (r) => r.fileName ?? '', render: (r) => r.fileName ?? '—' },
            { key: 'added', header: 'Added', align: 'end', sortValue: (r) => r.added },
            { key: 'updated', header: 'Updated', align: 'end', sortValue: (r) => r.updated },
            { key: 'withdrawn', header: 'Withdrawn', align: 'end', sortValue: (r) => r.withdrawn },
            { key: 'unchanged', header: 'Unchanged', align: 'end', sortValue: (r) => r.unchanged },
            {
              key: 'view',
              header: '',
              align: 'end',
              render: (r) => (
                <Button size="small" appearance={selected?.id === r.id ? 'primary' : 'secondary'} onClick={() => setSelected(r)}>
                  View changes
                </Button>
              ),
            },
          ]}
        />
      </Panel>

      {selected && (
        <Panel title={`Changes — ${fmt(selected.startedUtc)}`}>
          <DataTable<ImportChange>
            ariaLabel="Import changes"
            rows={changes ?? []}
            rowKey={(c) => c.id}
            emptyMessage="No row-level changes recorded for this import."
            columns={[
              {
                key: 'type',
                header: 'Change',
                sortValue: (c) => c.changeType,
                render: (c) => (
                  <Badge appearance="filled" color={typeTone[c.changeType] ?? 'informative'}>{c.changeType}</Badge>
                ),
              },
              { key: 'label', header: 'Record', sortValue: (c) => c.label ?? '', render: (c) => c.label ?? '—' },
              { key: 'key', header: 'Task Id', sortValue: (c) => c.externalKey ?? '', render: (c) => c.externalKey ?? '—' },
              {
                key: 'details',
                header: 'Details',
                render: (c) =>
                  c.changes.length === 0 ? (
                    <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>—</Text>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                      {c.changes.map((f, i) => (
                        <Text key={i} size={200}>
                          <b>{f.field}:</b> {f.from ?? '∅'} → {f.to ?? '∅'}
                        </Text>
                      ))}
                    </div>
                  ),
              },
            ]}
          />
        </Panel>
      )}
    </div>
  )
}
