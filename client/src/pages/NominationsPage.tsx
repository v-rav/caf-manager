import {
  Badge,
  Button,
  Dropdown,
  Input,
  Option,
  SearchBox,
  Text,
  Textarea,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { Modal } from '../components/Modal'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import { useMemo, useState } from 'react'
import type { Nomination, NominationUpdate } from '../types'

// The migration journey has four stages; the summary counts how many nominations sit in each.
const STAGES = [
  { n: 1, label: 'Validating', match: 'validating', tone: 'neutral' as const },
  { n: 2, label: 'Pre-Requisites', match: 'pre-requisite', tone: 'warning' as const },
  { n: 3, label: 'Finalize Scope', match: 'finalize', tone: 'warning' as const },
  { n: 4, label: 'Executing Migration', match: 'executing migration', tone: 'brand' as const },
]

const STATUS_OPTIONS = [
  'Open',
  'In Progress',
  'Blocked',
  'Waiting for Customer Action',
  'Customer Deferred',
  'Waiting on Follow-up',
  'Completed',
  'Closed',
]

const BLOCKER_OPTIONS = [
  'Waiting for Customer Action',
  'Approval Pending',
  'Access Pending',
  'Landing Zone Pending',
  'Testing/Validation Pending',
  'Dependency Pending',
  'Budget/Priority Hold',
  'Internal Alignment',
]

const WAVE_OPTIONS = ['App', 'DB', 'Security/Defender', 'Landing Zone', 'Dispatch', 'Related']

const BLOCKED_STATES = ['Blocked', 'Waiting for Customer Action', 'Waiting on Follow-up']

// Lightweight keyword → blocker-reason suggestion from free-text (no AI needed).
function suggestBlocker(text?: string): string | undefined {
  const t = (text ?? '').toLowerCase()
  if (!t) return undefined
  if (/(access|permission|rbac)/.test(t)) return 'Access Pending'
  if (/(landing zone|\balz\b|prereq|pre-requisite)/.test(t)) return 'Landing Zone Pending'
  if (/(approv|sign-?off)/.test(t)) return 'Approval Pending'
  if (/(test|uat|validation)/.test(t)) return 'Testing/Validation Pending'
  if (/(budget|priorit|cost)/.test(t)) return 'Budget/Priority Hold'
  if (/(depend|blocked by)/.test(t)) return 'Dependency Pending'
  if (/(align|internal)/.test(t)) return 'Internal Alignment'
  if (/(customer|client|awaiting)/.test(t)) return 'Waiting for Customer Action'
  return undefined
}

const staleTone: Record<string, { label: string; color: string; bg: string }> = {
  Warn: { label: 'Warn', color: '#8a6d00', bg: '#fff4ce' },
  Escalate: { label: 'Escalate', color: '#8a3b00', bg: '#fed9cc' },
  Defer: { label: 'Defer', color: '#a4262c', bg: '#fde7e9' },
}

export function NominationsPage() {
  const { region } = useRegion()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const [currentStateFilter, setCurrentStateFilter] = useState('')
  const [migrationFilter, setMigrationFilter] = useState('')
  const [staleOnly, setStaleOnly] = useState(false)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounced(search)

  // Manage dialog state.
  const [editing, setEditing] = useState<Nomination | null>(null)
  const [form, setForm] = useState<NominationUpdate>({ status: 'Open' })
  const [waveType, setWaveType] = useState('App')
  const [waveRef, setWaveRef] = useState('')
  const [busy, setBusy] = useState(false)

  const stageCount = (match: string) =>
    (data ?? []).filter((n) => (n.migrationStatus ?? '').toLowerCase().includes(match)).length

  const blockedTotal = (data ?? []).filter((n) => BLOCKED_STATES.includes(n.status)).length
  const staleCount = (tier: string) => (data ?? []).filter((n) => n.staleTier === tier).length

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
          (!staleOnly || !!n.staleTier) &&
          (!debouncedSearch || (n.accountName ?? '').toLowerCase().includes(debouncedSearch.toLowerCase())),
      ),
    [data, currentStateFilter, migrationFilter, staleOnly, debouncedSearch],
  )

  const openManage = (n: Nomination) => {
    setEditing(n)
    setForm({
      status: n.status,
      blockedReason: n.blockedReason ?? suggestBlocker(n.remarks ?? n.currentState),
      blockedSince: n.blockedSince,
      followUpDate: n.followUpDate,
      remarks: n.remarks ?? '',
    })
    setWaveType('App')
    setWaveRef('')
  }

  const isBlocked = BLOCKED_STATES.includes(form.status)

  const saveManage = async () => {
    if (!editing) return
    setBusy(true)
    try {
      await api.updateNomination(editing.id, {
        ...form,
        blockedReason: isBlocked ? form.blockedReason : undefined,
        blockedSince: isBlocked ? form.blockedSince : undefined,
      })
      await reload()
      setEditing(null)
    } finally {
      setBusy(false)
    }
  }

  const addWave = async () => {
    if (!editing || !waveRef.trim()) return
    setBusy(true)
    try {
      await api.addWave(editing.id, { waveType, reference: waveRef.trim() })
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
      setWaveRef('')
    } finally {
      setBusy(false)
    }
  }

  const removeWave = async (waveId: number) => {
    if (!editing) return
    setBusy(true)
    try {
      await api.deleteWave(editing.id, waveId)
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
    } finally {
      setBusy(false)
    }
  }

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
              <KpiCard key={s.n} label={`Stage ${s.n} · ${s.label}`} value={stageCount(s.match)} tone={s.tone} />
            ))}
          </div>

          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <KpiCard label="Blocked / Waiting" value={blockedTotal} tone="warning" />
            <KpiCard label="Stale · Warn (3d+)" value={staleCount('Warn')} tone="warning" />
            <KpiCard label="Stale · Escalate (5d+)" value={staleCount('Escalate')} tone="warning" />
            <KpiCard label="Stale · Defer (10d+)" value={staleCount('Defer')} tone="danger" />
          </div>

          <Panel
            title={`Nominations${rows ? ` (${rows.length})` : ''}`}
            action={
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
                <SearchBox
                  placeholder="Search account"
                  value={search}
                  onChange={(_, d) => setSearch(d.value)}
                  style={{ minWidth: 200 }}
                />
                <Button
                  appearance={staleOnly ? 'primary' : 'secondary'}
                  size="small"
                  onClick={() => setStaleOnly((v) => !v)}
                >
                  Needs update
                </Button>
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
                {
                  key: 'status',
                  header: 'Status',
                  sortValue: (n) => n.status,
                  render: (n) => {
                    const blocked = BLOCKED_STATES.includes(n.status)
                    return (
                      <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
                        <span style={{ color: blocked ? 'var(--colorPaletteRedForeground1)' : undefined, fontWeight: blocked ? 600 : undefined }}>
                          {n.status}
                        </span>
                        {n.blockedReason && (
                          <Badge appearance="tint" color="warning" size="small">
                            {n.blockedReason}
                          </Badge>
                        )}
                      </span>
                    )
                  },
                },
                {
                  key: 'age',
                  header: 'Age',
                  sortValue: (n) => n.daysSinceUpdate,
                  render: (n) => {
                    const tone = staleTone[n.staleTier]
                    return (
                      <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                        <span>{n.daysSinceUpdate}d</span>
                        {tone && (
                          <span style={{ background: tone.bg, color: tone.color, borderRadius: 4, padding: '1px 6px', fontSize: 11, fontWeight: 600 }}>
                            {tone.label}
                          </span>
                        )}
                      </span>
                    )
                  },
                },
                { key: 'followup', header: 'Follow-up', sortValue: (n) => n.followUpDate ?? '', render: (n) => n.followUpDate ?? '—' },
                {
                  key: 'waves',
                  header: 'Waves',
                  sortValue: (n) => n.waves.length,
                  render: (n) =>
                    n.waves.length === 0 ? (
                      <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                    ) : (
                      <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
                        {n.waves.map((w) => (
                          <Badge key={w.id} appearance="outline" size="small" title={`${w.waveType}: ${w.reference}`}>
                            {w.waveType}
                          </Badge>
                        ))}
                      </span>
                    ),
                },
                {
                  key: 'actions',
                  header: '',
                  render: (n) => (
                    <Button appearance="subtle" size="small" icon={<EditRegular />} onClick={() => openManage(n)}>
                      Manage
                    </Button>
                  ),
                },
              ]}
            />
          </Panel>
        </>
      )}

      <Modal
        open={!!editing}
        title={editing ? `Manage — ${editing.accountName ?? 'Nomination'}` : ''}
        onClose={() => setEditing(null)}
        onSubmit={saveManage}
        submitLabel="Save"
        busy={busy}
      >
        <label>
          Status
          <Dropdown
            value={form.status}
            selectedOptions={[form.status]}
            onOptionSelect={(_, d) => setForm((f) => ({ ...f, status: d.optionValue ?? f.status }))}
          >
            {STATUS_OPTIONS.map((s) => (
              <Option key={s} value={s}>
                {s}
              </Option>
            ))}
          </Dropdown>
        </label>

        {isBlocked && (
          <>
            <label>
              Blocker reason
              <Dropdown
                value={form.blockedReason ?? ''}
                selectedOptions={form.blockedReason ? [form.blockedReason] : []}
                placeholder="Select reason"
                onOptionSelect={(_, d) => setForm((f) => ({ ...f, blockedReason: d.optionValue }))}
              >
                {BLOCKER_OPTIONS.map((b) => (
                  <Option key={b} value={b}>
                    {b}
                  </Option>
                ))}
              </Dropdown>
            </label>
            <label>
              Blocked since
              <Input
                type="date"
                value={form.blockedSince ?? ''}
                onChange={(_, d) => setForm((f) => ({ ...f, blockedSince: d.value || undefined }))}
              />
            </label>
          </>
        )}

        <label>
          Follow-up date
          <Input
            type="date"
            value={form.followUpDate ?? ''}
            onChange={(_, d) => setForm((f) => ({ ...f, followUpDate: d.value || undefined }))}
          />
        </label>

        <label>
          Remarks
          <Textarea
            value={form.remarks ?? ''}
            onChange={(_, d) => setForm((f) => ({ ...f, remarks: d.value }))}
            rows={2}
          />
        </label>

        <div style={{ borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 10 }}>
          <Text weight="semibold">Related waves</Text>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: '8px 0' }}>
            {(editing?.waves ?? []).length === 0 && (
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                No linked waves yet.
              </Text>
            )}
            {(editing?.waves ?? []).map((w) => (
              <div key={w.id} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <Badge appearance="outline" size="small">
                  {w.waveType}
                </Badge>
                <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{w.reference}</span>
                <Button appearance="subtle" size="small" icon={<DeleteRegular />} onClick={() => removeWave(w.id)} disabled={busy} />
              </div>
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
            <Dropdown
              value={waveType}
              selectedOptions={[waveType]}
              onOptionSelect={(_, d) => setWaveType(d.optionValue ?? waveType)}
              style={{ minWidth: 140 }}
            >
              {WAVE_OPTIONS.map((w) => (
                <Option key={w} value={w}>
                  {w}
                </Option>
              ))}
            </Dropdown>
            <Input
              placeholder="Wave reference / ID"
              value={waveRef}
              onChange={(_, d) => setWaveRef(d.value)}
              style={{ flex: 1 }}
            />
            <Button appearance="secondary" icon={<AddRegular />} onClick={addWave} disabled={busy || !waveRef.trim()}>
              Link
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
