import { Badge, Button, Combobox, Dropdown, Input, Option, SearchBox, Text, Textarea } from '@fluentui/react-components'
import { AddRegular, ArrowDownloadRegular, HistoryRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { Modal } from '../components/Modal'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { PerformanceReview, PerformanceReviewUpsert } from '../types'

const DIMS: { key: keyof PerformanceReviewUpsert; label: string }[] = [
  { key: 'communicationVerbal', label: 'Comm. Verbal' },
  { key: 'communicationWritten', label: 'Comm. Written' },
  { key: 'attitude', label: 'Attitude' },
  { key: 'processUnderstanding', label: 'Process' },
  { key: 'offeringUnderstanding', label: 'Offering' },
]

function today() {
  return new Date().toISOString().slice(0, 10)
}

const emptyForm: PerformanceReviewUpsert = { personName: '', reviewDate: today() }

export function PerformancePage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.performance(region), [region])
  const { data: roster } = useAsync(() => api.resources({}), [])
  const { data: roles } = useAsync(() => api.roles(), [])
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounced(search)

  const [form, setForm] = useState<PerformanceReviewUpsert | null>(null)
  const [busy, setBusy] = useState(false)
  const [drafts, setDrafts] = useState<Record<number, Partial<PerformanceReviewUpsert>>>({})
  const [historyOf, setHistoryOf] = useState<PerformanceReview | null>(null)
  const { data: history } = useAsync(
    () => (historyOf ? api.performanceHistory(historyOf.personName) : Promise.resolve([])),
    [historyOf?.personName],
  )

  const rows = useMemo(
    () =>
      (data ?? []).filter(
        (r) =>
          !debouncedSearch ||
          r.personName.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          (r.role ?? '').toLowerCase().includes(debouncedSearch.toLowerCase()),
      ),
    [data, debouncedSearch],
  )

  const reviewed = (data ?? []).filter((r) => !r.pending)
  const avgScore = reviewed.length
    ? Math.round((reviewed.reduce((s, r) => s + (r.score ?? 0), 0) / reviewed.length) * 100) / 100
    : 0
  const needTraining = (data ?? []).filter((r) => r.trainingNeeds.length > 0).length

  // Existing reporting managers power the freeform Combobox (mix of pick-existing + type-new).
  const managers = useMemo(
    () => Array.from(new Set((data ?? []).map((r) => r.reportingManager).filter((m): m is string => !!m))).sort(),
    [data],
  )

  const patch = (p: Partial<PerformanceReviewUpsert>) => setForm((f) => (f ? { ...f, ...p } : f))

  const openAdd = () => setForm({ ...emptyForm, region: region ?? 'EMEA' })
  const openRescore = (r: PerformanceReview) =>
    setForm({
      resourceId: r.resourceId,
      personName: r.personName,
      role: r.role,
      reportingManager: r.reportingManager,
      region: r.region,
      reviewDate: today(),
      communicationVerbal: r.communicationVerbal,
      communicationWritten: r.communicationWritten,
      attitude: r.attitude,
      processUnderstanding: r.processUnderstanding,
      offeringUnderstanding: r.offeringUnderstanding,
    })

  const save = async () => {
    if (!form?.personName.trim()) return
    setBusy(true)
    try {
      await api.addPerformanceReview(form)
      setForm(null)
      reload()
    } finally {
      setBusy(false)
    }
  }

  const setDim = (key: keyof PerformanceReviewUpsert, value: string) => {
    const n = value === '' ? undefined : Math.min(5, Math.max(0, Number(value)))
    patch({ [key]: n } as Partial<PerformanceReviewUpsert>)
  }

  const dimVal = (r: PerformanceReview, key: keyof PerformanceReviewUpsert): number | undefined => {
    const d = drafts[r.id]
    if (d && key in d) return d[key] as number | undefined
    return r[key as keyof PerformanceReview] as number | undefined
  }
  const setDraftDim = (r: PerformanceReview, key: keyof PerformanceReviewUpsert, value: string) => {
    const n = value === '' ? undefined : Math.min(5, Math.max(0, Number(value)))
    setDrafts((prev) => ({ ...prev, [r.id]: { ...prev[r.id], [key]: n } }))
  }
  const isDirty = (r: PerformanceReview) => !!drafts[r.id] && Object.keys(drafts[r.id]).length > 0
  const saveInline = async (r: PerformanceReview) => {
    setBusy(true)
    try {
      await api.addPerformanceReview({
        personName: r.personName,
        role: r.role,
        reportingManager: r.reportingManager,
        region: r.region,
        reviewDate: today(),
        communicationVerbal: dimVal(r, 'communicationVerbal'),
        communicationWritten: dimVal(r, 'communicationWritten'),
        attitude: dimVal(r, 'attitude'),
        processUnderstanding: dimVal(r, 'processUnderstanding'),
        offeringUnderstanding: dimVal(r, 'offeringUnderstanding'),
      })
      setDrafts((prev) => {
        const c = { ...prev }
        delete c[r.id]
        return c
      })
      reload()
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Performance Review
      </Text>

      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <KpiCard label="People" value={data?.length ?? 0} tone="brand" />
            <KpiCard label="Reviewed" value={reviewed.length} tone="success" />
            <KpiCard label="Pending review" value={(data?.length ?? 0) - reviewed.length} tone="warning" />
            <KpiCard label="Avg score" value={avgScore || '—'} tone="neutral" />
            <KpiCard label="Training needs" value={needTraining} tone={needTraining ? 'danger' : 'success'} />
          </div>

          <Panel
            title={`Latest scorecard${rows ? ` (${rows.length})` : ''}`}
            action={
              <div style={{ display: 'flex', gap: 8 }}>
                <SearchBox
                  placeholder="Search name or role"
                  value={search}
                  onChange={(_, d) => setSearch(d.value)}
                  style={{ minWidth: 200 }}
                />
                <Button appearance="primary" icon={<AddRegular />} onClick={openAdd}>
                  Add / re-score
                </Button>
                <Button as="a" href={api.exportUrl('performance', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>
                  Export
                </Button>
              </div>
            }
          >
            <DataTable<PerformanceReview>
              ariaLabel="Performance reviews"
              rows={rows}
              rowKey={(r) => r.id}
              defaultSort={{ key: 'score', dir: 'desc' }}
              emptyMessage="No people match your search."
              columns={[
                { key: 'name', header: 'Name', sortValue: (r) => r.personName },
                { key: 'role', header: 'Role', sortValue: (r) => r.role ?? '', render: (r) => r.role ?? '—' },
                { key: 'manager', header: 'Reporting Manager', sortValue: (r) => r.reportingManager ?? '', render: (r) => r.reportingManager ?? '—' },
                ...DIMS.map((dim) => ({
                  key: dim.key,
                  header: dim.label,
                  align: 'center' as const,
                  sortValue: (r: PerformanceReview) => dimVal(r, dim.key) ?? -1,
                  render: (r: PerformanceReview) => (
                    <Input
                      type="number"
                      min={0}
                      max={5}
                      step={0.5}
                      appearance="filled-lighter"
                      style={{ width: 62 }}
                      value={dimVal(r, dim.key) === undefined || dimVal(r, dim.key) === null ? '' : String(dimVal(r, dim.key))}
                      onChange={(_, d) => setDraftDim(r, dim.key, d.value)}
                    />
                  ),
                })),
                {
                  key: 'score',
                  header: 'Score',
                  align: 'center',
                  sortValue: (r) => (r.pending ? -1 : r.score ?? 0),
                  render: (r) =>
                    r.pending ? (
                      <Badge appearance="tint" color="warning" size="small">
                        Pending
                      </Badge>
                    ) : (
                      <b>{r.score}</b>
                    ),
                },
                {
                  key: 'training',
                  header: 'Training needs',
                  sortValue: (r) => r.trainingNeeds.length,
                  render: (r) =>
                    r.trainingNeeds.length === 0 ? (
                      <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                    ) : (
                      <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
                        {r.trainingNeeds.map((t) => (
                          <Badge key={t} appearance="tint" color="danger" size="small">
                            {t}
                          </Badge>
                        ))}
                      </span>
                    ),
                },
                {
                  key: 'actions',
                  header: '',
                  render: (r) => (
                    <div style={{ display: 'flex', gap: 4 }}>
                      {isDirty(r) && (
                        <Button appearance="primary" size="small" onClick={() => saveInline(r)} disabled={busy}>
                          Save
                        </Button>
                      )}
                      <Button appearance="subtle" size="small" onClick={() => openRescore(r)}>
                        Edit
                      </Button>
                      <Button appearance="subtle" size="small" icon={<HistoryRegular />} title="History" onClick={() => setHistoryOf(r)} disabled={r.reviewCount < 1} />
                    </div>
                  ),
                },
              ]}
            />
          </Panel>
        </>
      )}

      <Modal
        open={!!form}
        title={form?.personName ? `Review — ${form.personName}` : 'Add review'}
        onClose={() => setForm(null)}
        onSubmit={save}
        submitDisabled={!form?.personName.trim()}
        busy={busy}
      >
        {form && (
          <>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Text size={200}>Person (from roster) *</Text>
              <Combobox
                placeholder="Search roster…"
                value={form.personName}
                selectedOptions={form.resourceId ? [String(form.resourceId)] : []}
                onChange={(e) => patch({ personName: e.target.value, resourceId: undefined })}
                onOptionSelect={(_, d) => {
                  const r = (roster ?? []).find((x) => String(x.resourceId) === d.optionValue)
                  if (r) patch({ resourceId: r.resourceId, personName: r.name, role: form.role || r.role, region: form.region || r.region })
                }}
                style={{ width: '100%' }}
              >
                {(roster ?? [])
                  .filter((r) => !form.personName || r.name.toLowerCase().includes(form.personName.toLowerCase()))
                  .slice(0, 50)
                  .map((r) => (
                    <Option key={r.resourceId} value={String(r.resourceId)} text={r.name}>
                      {r.name}{r.role ? ` · ${r.role}` : ''}
                    </Option>
                  ))}
              </Combobox>
              {form.resourceId ? (
                <Text size={200} style={{ color: 'var(--colorPaletteGreenForeground1)' }}>Linked to roster</Text>
              ) : form.personName ? (
                <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>Not in roster (free text — may drift)</Text>
              ) : null}
            </label>
            <div style={{ display: 'flex', gap: 8 }}>
              <Dropdown
                style={{ flex: 1 }}
                placeholder="Role"
                value={form.role ?? ''}
                selectedOptions={form.role ? [form.role] : []}
                onOptionSelect={(_, d) => patch({ role: d.optionValue ?? '' })}
              >
                {(roles ?? []).map((r) => (
                  <Option key={r} value={r}>
                    {r}
                  </Option>
                ))}
              </Dropdown>
              <Combobox
                style={{ flex: 1 }}
                placeholder="Reporting Manager"
                value={form.reportingManager ?? ''}
                onChange={(e) => patch({ reportingManager: e.target.value })}
                onOptionSelect={(_, d) => patch({ reportingManager: d.optionValue ?? '' })}
              >
                {managers
                  .filter((m) => !form.reportingManager || m.toLowerCase().includes((form.reportingManager ?? '').toLowerCase()))
                  .slice(0, 50)
                  .map((m) => (
                    <Option key={m} value={m}>
                      {m}
                    </Option>
                  ))}
              </Combobox>
            </div>
            <Input type="date" value={form.reviewDate ?? today()} onChange={(_, d) => patch({ reviewDate: d.value })} />
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
              {DIMS.map((dim) => (
                <label key={dim.key} style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Text size={200}>{dim.label} (0–5)</Text>
                  <Input
                    type="number"
                    min={0}
                    max={5}
                    step={0.5}
                    value={form[dim.key] === undefined || form[dim.key] === null ? '' : String(form[dim.key])}
                    onChange={(_, d) => setDim(dim.key, d.value)}
                  />
                </label>
              ))}
            </div>
            <label>
              Comments
              <Textarea value={form.comments ?? ''} onChange={(_, d) => patch({ comments: d.value })} rows={2} />
            </label>
          </>
        )}
      </Modal>

      <Modal open={!!historyOf} title={historyOf ? `History — ${historyOf.personName}` : ''} onClose={() => setHistoryOf(null)}>
        {(history ?? []).length === 0 ? (
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            No review history yet.
          </Text>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {(history ?? [])
              .slice()
              .reverse()
              .map((h) => (
                <div key={h.id} style={{ display: 'flex', justifyContent: 'space-between', gap: 12, borderBottom: '1px solid var(--colorNeutralStroke2)', paddingBottom: 4 }}>
                  <Text>{h.reviewDate}</Text>
                  <Text weight="semibold">{h.pending ? 'Pending' : h.score}</Text>
                </div>
              ))}
          </div>
        )}
      </Modal>
    </div>
  )
}
