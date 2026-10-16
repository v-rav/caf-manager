import { Badge, Button, Combobox, Dropdown, Input, Option, Text, Textarea } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import { useState, type ReactNode } from 'react'
import { api } from '../../api'
import { ADOPTION_LEVELS } from '../../adoption'
import { Modal } from '../../components/Modal'
import type { Nomination, NominationUpdate, Resource, StatusRulesResponse } from '../../types'
import { LinkChip, presentWaves, ReadField } from './NominationDetail'

// Canonical Stage (Migration Status) values written back when the stage is edited here.
const STAGE_OPTIONS = [
  { value: '1 - Validating & Initial Scope', label: '1 — Validating' },
  { value: '2 - Executing Pre-Requisites', label: '2 — Pre-Requisites' },
  { value: '3 - Finalize Scope', label: '3 — Finalize Scope' },
  { value: '4 - Executing Migration', label: '4 — Executing Migration' },
]
const STAGE_MATCH = ['validating', 'pre-requisite', 'finalize', 'executing migration']

// Draft-only: which stage the edited Migration Status text points at (the grid uses server `stageIndex`).
function draftStage(migrationStatus?: string): number | null {
  const t = (migrationStatus ?? '').toLowerCase()
  const i = STAGE_MATCH.findIndex((m) => t.includes(m))
  return i >= 0 ? i + 1 : null
}

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

// Delivery roles a resource can hold on a nomination (operational staffing, not the capacity model).
const ASSIGN_ROLES = ['Solution Architect', 'Migration Engineer', 'DevOps Engineer']

// Which resource Role values are eligible for each delivery role (SMEs are sometimes allocated as SAs).
function eligibleForRole(assignRole: string, resourceRole?: string): boolean {
  const r = (resourceRole ?? '').toLowerCase()
  switch (assignRole) {
    case 'Solution Architect':
      return r.includes('architect') || r.includes('sme')
    case 'Migration Engineer':
      return (r.includes('engineer') && !r.includes('devops')) || r.includes('migration') || r.includes('sme')
    case 'DevOps Engineer':
      return r.includes('devops')
    default:
      return true
  }
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground2)' }}>
        {label}
      </Text>
      {children}
    </div>
  )
}

interface Props {
  nomination: Nomination
  region?: string
  resources: Resource[]
  classOn: boolean
  classifications: string[]
  statusInfo: StatusRulesResponse
  onClose: () => void
  /** Reload the grid after any change. */
  onChanged: () => void
}

/** Manage dialog: portal-owned fields, resource assignment and wave links for one nomination. */
export function ManageNominationDialog({ nomination, region, resources, classOn, classifications, statusInfo, onClose, onChanged }: Props) {
  const [editing, setEditing] = useState<Nomination>(nomination)
  const [form, setForm] = useState<NominationUpdate>(() => ({
    status: nomination.status,
    migrationStatus: nomination.migrationStatus ?? '',
    blockedReason: nomination.blockedReason ?? nomination.suggestedBlockerReason ?? undefined,
    blockedSince: nomination.blockedSince,
    followUpDate: nomination.followUpDate,
    remarks: nomination.remarks ?? '',
    projectCoordinator: nomination.projectCoordinator ?? '',
    cftlPrimary: nomination.cftlPrimary ?? '',
    classification: nomination.classification ?? 'Standard Factory',
    ghcpAdoptionLevel: nomination.ghcpAdoptionLevel ?? 0,
    shortName: nomination.shortName ?? '',
  }))
  const [waveType, setWaveType] = useState('App')
  const [waveRef, setWaveRef] = useState('')
  // Seed the resource picker with the current SA so the dropdown reflects it.
  const currentSa = (nomination.solutionArchitect ?? '').trim()
  const saMatch = currentSa ? resources.find((r) => r.name.trim().toLowerCase() === currentSa.toLowerCase()) : undefined
  const [assignResourceId, setAssignResourceId] = useState<number | null>(saMatch?.resourceId ?? null)
  const [assignRole, setAssignRole] = useState('Solution Architect')
  const [resourceQuery, setResourceQuery] = useState(currentSa)
  // True only while the user is typing, so a seeded/selected value doesn't filter the list.
  const [resourceTyping, setResourceTyping] = useState(false)
  const [busy, setBusy] = useState(false)

  const isBlocked = !!statusInfo.rules.find((r) => r.label === form.status)?.isBlocked
  const statusOptions = [
    ...statusInfo.rules.filter((r) => r.selectable).map((r) => r.label),
    ...(form.status && !statusInfo.rules.some((r) => r.selectable && r.label === form.status) ? [form.status] : []),
  ]
  const stageNo = draftStage(form.migrationStatus)

  // Run a wave/resource change, then refresh both the grid and this dialog's copy.
  const mutate = async (op: () => Promise<unknown>, after?: () => void) => {
    setBusy(true)
    try {
      await op()
      const fresh = await api.nominations(region)
      onChanged()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
      after?.()
    } finally {
      setBusy(false)
    }
  }

  const save = async () => {
    setBusy(true)
    try {
      await api.updateNomination(editing.id, {
        ...form,
        // Classification is parked while the feature flag is off — never overwrite a stored value from here.
        classification: classOn ? form.classification : undefined,
        blockedReason: isBlocked ? form.blockedReason : undefined,
        blockedSince: isBlocked ? form.blockedSince : undefined,
      })
      onChanged()
      onClose()
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal open title={`Manage — ${editing.accountName ?? 'Nomination'}`} onClose={onClose} onSubmit={save} submitLabel="Save" busy={busy} maxWidth={640}>
      <div
        style={{
          background: 'var(--colorNeutralBackground2)',
          border: '1px solid var(--colorNeutralStroke2)',
          borderRadius: 6,
          padding: 12,
          display: 'flex',
          flexDirection: 'column',
          gap: 8,
        }}
      >
        <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground3)' }}>
          From FDO · refreshed on import (read-only)
        </Text>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 16px' }}>
          <ReadField label="Current State">
            {editing.currentState ? <Badge appearance="tint" color={editing.healthTone ?? 'informative'}>{editing.currentState}</Badge> : '—'}
          </ReadField>
          <ReadField label="Age in stage">{editing.stageAgeDays != null ? `${editing.stageAgeDays}d` : '—'}</ReadField>
          <ReadField label="Offering">{editing.technology ?? '—'}</ReadField>
          <ReadField label="TPID · Region">{`${editing.tpid ?? '—'} · ${editing.region ?? '—'}`}</ReadField>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        <Field label="Stage (Migration Status)">
          <Dropdown
            style={{ width: '100%', minWidth: 0 }}
            value={stageNo ? STAGE_OPTIONS[stageNo - 1].label : form.migrationStatus ?? ''}
            selectedOptions={stageNo ? [STAGE_OPTIONS[stageNo - 1].value] : []}
            placeholder="Select stage"
            onOptionSelect={(_, d) => setForm((f) => ({ ...f, migrationStatus: d.optionValue ?? f.migrationStatus }))}
          >
            {STAGE_OPTIONS.map((o) => <Option key={o.value} value={o.value}>{o.label}</Option>)}
          </Dropdown>
        </Field>
        <Field label="Workflow status">
          <Dropdown
            style={{ width: '100%', minWidth: 0 }}
            value={form.status}
            selectedOptions={[form.status]}
            onOptionSelect={(_, d) => setForm((f) => ({ ...f, status: d.optionValue ?? f.status }))}
          >
            {statusOptions.map((s) => <Option key={s} value={s}>{s}</Option>)}
          </Dropdown>
        </Field>
      </div>

      {isBlocked && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Blocker reason">
            <Dropdown
              style={{ width: '100%', minWidth: 0 }}
              value={form.blockedReason ?? ''}
              selectedOptions={form.blockedReason ? [form.blockedReason] : []}
              placeholder="Select reason"
              onOptionSelect={(_, d) => setForm((f) => ({ ...f, blockedReason: d.optionValue }))}
            >
              {BLOCKER_OPTIONS.map((b) => <Option key={b} value={b}>{b}</Option>)}
            </Dropdown>
          </Field>
          <Field label="Blocked since">
            <Input type="date" style={{ width: '100%' }} value={form.blockedSince ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, blockedSince: d.value || undefined }))} />
          </Field>
        </div>
      )}

      <Field label="Follow-up date">
        <Input type="date" style={{ width: '100%' }} value={form.followUpDate ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, followUpDate: d.value || undefined }))} />
      </Field>

      <Field label="Remarks">
        <Textarea style={{ width: '100%' }} value={form.remarks ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, remarks: d.value }))} rows={2} />
      </Field>

      <Field label="App / short name">
        <Input style={{ width: '100%' }} value={form.shortName ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, shortName: d.value }))} placeholder="app or system name (shown as Account · Name)" />
      </Field>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        <Field label="PM">
          <Input style={{ width: '100%' }} value={form.projectCoordinator ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, projectCoordinator: d.value }))} />
        </Field>
        <Field label="CFTL">
          <Input style={{ width: '100%' }} value={form.cftlPrimary ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, cftlPrimary: d.value }))} />
        </Field>
      </div>

      {classOn && (
        <Field label="Classification">
          <Dropdown
            style={{ width: '100%' }}
            value={form.classification ?? 'Standard Factory'}
            selectedOptions={[form.classification ?? 'Standard Factory']}
            onOptionSelect={(_, d) => setForm((f) => ({ ...f, classification: d.optionValue ?? 'Standard Factory' }))}
          >
            {(classifications.length ? classifications : ['Standard Factory']).map((c) => <Option key={c} value={c}>{c}</Option>)}
          </Dropdown>
        </Field>
      )}

      <Field label="GHCP adoption level">
        <Dropdown
          style={{ width: '100%' }}
          value={ADOPTION_LEVELS[form.ghcpAdoptionLevel ?? 0].label}
          selectedOptions={[String(form.ghcpAdoptionLevel ?? 0)]}
          onOptionSelect={(_, d) => setForm((f) => ({ ...f, ghcpAdoptionLevel: Number(d.optionValue ?? 0) }))}
        >
          {ADOPTION_LEVELS.map((a) => <Option key={a.level} value={String(a.level)}>{`${a.level} · ${a.label}`}</Option>)}
        </Dropdown>
      </Field>

      <div style={{ borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 10 }}>
        <Text weight="semibold">Assigned resources</Text>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', margin: '2px 0 6px' }}>
          Staff this migration with a delivery role. The Solution Architect assigned here drives the SA column. Operational only — this does not affect account capacity.
        </Text>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: '8px 0' }}>
          {editing.assignedResources.length === 0 && (
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No resources assigned yet.</Text>
          )}
          {editing.assignedResources.map((a) => (
            <div key={a.resourceId} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <Badge appearance="outline" color="brand" size="small">{a.role ?? 'Role TBD'}</Badge>
              <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {a.name}
                {a.region ? ` · ${a.region}` : ''}
              </span>
              <Button
                appearance="subtle"
                size="small"
                icon={<DeleteRegular />}
                aria-label="Unassign resource"
                onClick={() => void mutate(() => api.unassignNominationResource(editing.id, a.resourceId))}
                disabled={busy}
              />
            </div>
          ))}
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <Combobox
            placeholder="Search resource…"
            value={resourceQuery}
            selectedOptions={assignResourceId ? [String(assignResourceId)] : []}
            onChange={(e) => {
              setResourceQuery(e.target.value)
              setResourceTyping(true)
              setAssignResourceId(null)
            }}
            onOptionSelect={(_, d) => {
              const id = Number(d.optionValue)
              setAssignResourceId(id)
              setResourceQuery(resources.find((x) => x.resourceId === id)?.name ?? '')
              setResourceTyping(false)
            }}
            style={{ flex: 1, minWidth: 180 }}
          >
            {resources
              .filter((r) => eligibleForRole(assignRole, r.role))
              .filter((r) => !resourceTyping || !resourceQuery || r.name.toLowerCase().includes(resourceQuery.toLowerCase()))
              .slice(0, 50)
              .map((r) => (
                <Option key={r.resourceId} value={String(r.resourceId)} text={r.name}>
                  {r.name} · {r.role}
                </Option>
              ))}
          </Combobox>
          <Dropdown
            value={assignRole}
            selectedOptions={[assignRole]}
            onOptionSelect={(_, d) => {
              const next = d.optionValue ?? assignRole
              setAssignRole(next)
              // Clear a selection that isn't eligible for the newly chosen role.
              const sel = resources.find((x) => x.resourceId === assignResourceId)
              if (sel && !eligibleForRole(next, sel.role)) {
                setAssignResourceId(null)
                setResourceQuery('')
              }
              setResourceTyping(false)
            }}
            style={{ minWidth: 170 }}
          >
            {ASSIGN_ROLES.map((r) => <Option key={r} value={r}>{r}</Option>)}
          </Dropdown>
          <Button
            appearance="secondary"
            icon={<AddRegular />}
            disabled={busy || !assignResourceId}
            onClick={() => void mutate(() => api.assignNominationResource(editing.id, assignResourceId!, assignRole), () => {
              setAssignResourceId(null)
              setResourceQuery('')
              setResourceTyping(false)
            })}
          >
            Assign
          </Button>
        </div>
      </div>

      <div style={{ borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 10 }}>
        <Text weight="semibold">Related waves</Text>
        <div style={{ display: 'flex', gap: 6, alignItems: 'center', margin: '6px 0' }}>
          <LinkChip label="DB" linked={editing.dbLinked} />
          <LinkChip label="ALZ" linked={editing.alzLinked} />
          <LinkChip label="Sec" linked={editing.securityLinked} />
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            {editing.noWavesLinked ? 'No waves linked yet — add any associated waves below.' : `Linked: ${presentWaves(editing).join(', ')}`}
          </Text>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: '8px 0' }}>
          {editing.waves.length === 0 && (
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No linked waves yet.</Text>
          )}
          {editing.waves.map((w) => (
            <div key={w.id} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <Badge appearance="outline" size="small">{w.waveType}</Badge>
              <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{w.reference}</span>
              <Button
                appearance="subtle"
                size="small"
                icon={<DeleteRegular />}
                aria-label="Remove wave link"
                onClick={() => void mutate(() => api.deleteWave(editing.id, w.id))}
                disabled={busy}
              />
            </div>
          ))}
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <Dropdown value={waveType} selectedOptions={[waveType]} onOptionSelect={(_, d) => setWaveType(d.optionValue ?? waveType)} style={{ minWidth: 140 }}>
            {WAVE_OPTIONS.map((w) => <Option key={w} value={w}>{w}</Option>)}
          </Dropdown>
          <Input placeholder="Wave reference / ID" value={waveRef} onChange={(_, d) => setWaveRef(d.value)} style={{ flex: 1 }} />
          <Button
            appearance="secondary"
            icon={<AddRegular />}
            disabled={busy || !waveRef.trim()}
            onClick={() => void mutate(() => api.addWave(editing.id, { waveType, reference: waveRef.trim() }), () => setWaveRef(''))}
          >
            Link
          </Button>
        </div>
      </div>
    </Modal>
  )
}
