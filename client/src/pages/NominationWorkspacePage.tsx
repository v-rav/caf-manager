import { Badge, Button, Checkbox, Dropdown, Input, Option, Text, Tooltip } from '@fluentui/react-components'
import {
  CheckmarkCircleFilled,
  CircleHalfFillRegular,
  CircleRegular,
  DocumentLinkRegular,
  ErrorCircleFilled,
  SubtractCircleRegular,
  WarningRegular,
} from '@fluentui/react-icons'
import { Fragment, useEffect, useMemo, useState } from 'react'
import { useParams, Link as RouterLink } from 'react-router-dom'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import type { Blocker, Gate, GateItem, Governance, NominationEvent } from '../types'

const KIND_TONE: Record<string, 'brand' | 'success' | 'warning' | 'informative' | 'subtle'> = {
  Task: 'informative', Prerequisite: 'warning', Deliverable: 'brand', Approval: 'success', Signoff: 'success',
}

function stepIcon(g: Gate, isCurrent: boolean) {
  if (g.status === 'Green') return <CheckmarkCircleFilled style={{ color: '#107c10' }} />
  if (isCurrent) return <ErrorCircleFilled style={{ color: '#c50f1f' }} />
  if (g.status === 'InProgress') return <CircleHalfFillRegular style={{ color: '#0f6cbd' }} />
  return <CircleRegular style={{ color: '#8a8886' }} />
}

function eventLabel(t: string) {
  return t === 'ItemStatus' ? 'Item' : t === 'BlockerRaised' ? 'Blocked' : t === 'BlockerResolved' ? 'Unblocked' : t
}
function eventTone(t: string): 'brand' | 'danger' | 'success' | 'informative' {
  return t === 'BlockerRaised' ? 'danger' : t === 'BlockerResolved' ? 'success' : t === 'ItemStatus' ? 'brand' : 'informative'
}

export function NominationWorkspacePage() {
  const { id } = useParams<{ id: string }>()
  const nominationId = Number(id)
  const [gov, setGov] = useState<Governance | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [categories, setCategories] = useState<string[]>([])
  const [owners, setOwners] = useState<string[]>([])
  const [rc, setRc] = useState<string>('')
  const [rClock, setRClock] = useState(true)
  const [rOwner, setROwner] = useState('')
  const [rNotes, setRNotes] = useState('')
  const [busyBlk, setBusyBlk] = useState(false)
  const [events, setEvents] = useState<NominationEvent[]>([])

  const loadEvents = () => { api.governanceEvents(nominationId).then(setEvents).catch(() => {}) }

  useEffect(() => { api.blockerCategories().then((c) => { setCategories(c); setRc((v) => v || c[0] || '') }).catch(() => {}) }, [])
  useEffect(() => { api.blockerOwners().then(setOwners).catch(() => {}) }, [])

  const load = async () => {
    setLoading(true); setError(null)
    try {
      const g = await api.governance(nominationId)
      setGov(g)
      setSelected((s) => s ?? g.currentGateKey ?? g.gates[0]?.key ?? null)
    } catch {
      setError('Could not load the workspace.')
    } finally {
      setLoading(false)
    }
  }
  useEffect(() => { void load(); loadEvents() }, [nominationId])

  const current = useMemo(() => gov?.gates.find((g) => g.key === selected) ?? gov?.gates[0], [gov, selected])

  const toggle = async (item: GateItem) => {
    if (!gov || saving) return
    setSaving(true)
    try {
      const next = item.status === 'Done' ? 'Pending' : 'Done'
      const updated = await api.updateGovernanceItem(nominationId, item.itemDefId, { status: next, owner: item.owner, ref: item.ref, notes: item.notes })
      setGov(updated)
      loadEvents()
    } finally {
      setSaving(false)
    }
  }

  if (loading && !gov) return <Loading label="Loading workspace…" />
  if (error) return <ErrorText error={error} onRetry={load} />
  if (!gov || !current) return null

  const pending = current.items.filter((i) => i.status !== 'Done').length
  const blockers = gov.blockers ?? []

  const raiseBlocker = async () => {
    if (!rc || busyBlk) return
    setBusyBlk(true)
    try {
      const updated = await api.raiseBlocker(nominationId, { category: rc, clockStopped: rClock, owner: rOwner || null, notes: rNotes || null })
      setGov(updated)
      setROwner(''); setRNotes('')
      loadEvents()
    } finally { setBusyBlk(false) }
  }
  const resolveBlocker = async (b: Blocker) => {
    if (busyBlk) return
    setBusyBlk(true)
    try { setGov(await api.resolveBlocker(nominationId, b.id)); loadEvents() } finally { setBusyBlk(false) }
  }

  const setNA = async (item: GateItem) => {
    if (!gov || saving) return
    setSaving(true)
    try {
      const next = item.status === 'NA' ? 'Pending' : 'NotApplicable'
      setGov(await api.updateGovernanceItem(nominationId, item.itemDefId, { status: next, owner: item.owner, ref: item.ref, notes: item.notes }))
      loadEvents()
    } finally { setSaving(false) }
  }

  const gateIndex = gov.gates.findIndex((g) => g.key === current.key)
  const nextGate = gov.gates[gateIndex + 1]
  const canAdvance = current.status === 'Green' && !!nextGate
  const unmet = current.items.filter((i) => i.status === 'Pending').map((i) => i.label)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
            <Text size={600} weight="bold">{gov.account ?? `Nomination ${gov.nominationId}`}</Text>
            {gov.classification && gov.classification !== 'Standard Factory' && <Badge appearance="tint" color="brand">{gov.classification}</Badge>}
            {gov.stage != null && <Badge appearance="outline">FDO Stage {gov.stage}</Badge>}
            {gov.tpid && <Badge appearance="outline">TPID {gov.tpid}</Badge>}
            <RouterLink to="/nominations" style={{ fontSize: 12 }}>← Nominations</RouterLink>
          </div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 2 }}>
            PM: {gov.pm ?? '—'} · CFTL: {gov.cftl ?? '—'} · SA: {gov.sa ?? '—'}
            {gov.ageDays != null ? ` — Age ${gov.ageDays}d` : ''}
            {gov.clockStoppedDays > 0 ? ` · ${gov.clockStoppedDays}d clock-stopped` : ''}
          </Text>
        </div>
        <div style={{ display: 'flex', gap: 12 }}>
          <KpiCard label="Readiness compliance" value={`${gov.compliancePercent}%`} tone={gov.compliancePercent >= 80 ? 'success' : gov.compliancePercent >= 50 ? 'brand' : 'neutral'} />
          <KpiCard label="Migration Success Index" value={gov.msiScore} tone={gov.msiBand === 'Green' ? 'success' : gov.msiBand === 'Amber' ? 'warning' : 'danger'} />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '250px 1fr 340px', gap: 16, alignItems: 'start' }}>
        <Panel title="Governance gates">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {gov.gates.map((g) => {
              const active = g.key === current.key
              return (
                <button
                  key={g.key}
                  onClick={() => setSelected(g.key)}
                  style={{
                    display: 'flex', alignItems: 'center', gap: 10, padding: '9px 10px', borderRadius: 8, cursor: 'pointer',
                    border: `1px solid ${active ? 'var(--colorBrandStroke1)' : 'transparent'}`,
                    background: active ? 'var(--colorNeutralBackground1Selected)' : 'transparent', textAlign: 'left',
                  }}
                >
                  {stepIcon(g, active)}
                  <div style={{ flex: 1 }}>
                    <Text size={300} weight={active ? 'semibold' : 'regular'}>{g.key} · {g.name}</Text>
                    <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>weight {g.weight}% · {g.percentComplete}%</Text>
                  </div>
                </button>
              )
            })}
          </div>
        </Panel>

        <Panel
          title={`${current.key} · ${current.name}`}
          action={
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>weight {current.weight}% · {pending === 0 ? 'complete' : `${pending} pending`}</Text>
              <Tooltip relationship="label" content={canAdvance ? 'Advance to the next gate' : nextGate ? `Complete first: ${unmet.join(', ') || 'all items'}` : 'Final gate'}>
                <Button appearance="primary" size="small" disabled={!canAdvance} onClick={() => nextGate && setSelected(nextGate.key)}>Advance ▸</Button>
              </Tooltip>
            </div>
          }
        >
          {current.exitCriteria && (
            <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)', marginBottom: 10 }}>Exit: {current.exitCriteria}</Text>
          )}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {current.items.map((i, idx) => {
              const showGroup = i.subStage && i.subStage !== current.items[idx - 1]?.subStage
              return (
                <Fragment key={i.itemDefId}>
                  {showGroup && <Text size={200} weight="semibold" style={{ marginTop: idx ? 8 : 0, color: 'var(--colorNeutralForeground2)' }}>{i.subStage}</Text>}
                  <div style={{
                    display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px', borderRadius: 6,
                    border: '1px solid var(--colorNeutralStroke2)',
                    background: i.status === 'Done' ? 'var(--colorNeutralBackground2)' : 'var(--colorNeutralBackground1)',
                  }}>
                    <input type="checkbox" checked={i.status === 'Done'} disabled={saving || i.status === 'NA'} onChange={() => toggle(i)} aria-label={i.label} />
                    <div style={{ flex: 1, opacity: i.status === 'NA' ? 0.55 : 1 }}>
                      <Text size={300} weight={i.mandatory ? 'semibold' : 'regular'} style={{ textDecoration: i.status === 'NA' ? 'line-through' : 'none' }}>{i.label}{i.mandatory ? ' ⚑' : ''}</Text>
                      <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
                        {i.responsibleRole}{i.ref ? ' · ' : ''}{i.ref && <a href="#" onClick={(e) => e.preventDefault()}><DocumentLinkRegular /> {i.ref}</a>}
                        {i.updatedBy ? ` · by ${i.updatedBy}` : ''}
                      </Text>
                    </div>
                    {i.status === 'NA' && <Badge appearance="tint" color="subtle">N/A</Badge>}
                    <Badge appearance="tint" color={KIND_TONE[i.kind] ?? 'informative'}>{i.kind}</Badge>
                    {i.blocked && <Badge appearance="filled" color="danger" icon={<WarningRegular />}>Blocked</Badge>}
                    <Tooltip relationship="label" content={i.status === 'NA' ? 'Mark applicable' : 'Mark N/A'}>
                      <Button size="small" appearance="subtle" icon={<SubtractCircleRegular />} disabled={saving} onClick={() => void setNA(i)} aria-label="Toggle N/A" />
                    </Tooltip>
                  </div>
                </Fragment>
              )
            })}
          </div>
        </Panel>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <Panel title={`Blockers${blockers.length ? ` · ${blockers.length} open` : ''}`}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: blockers.length ? 12 : 0 }}>
          <div>
            <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Category</Text>
            <Dropdown value={rc} selectedOptions={[rc]} onOptionSelect={(_, d) => setRc(d.optionValue ?? rc)} style={{ minWidth: 0, width: '100%' }}>
              {categories.map((c) => <Option key={c} value={c}>{c}</Option>)}
            </Dropdown>
          </div>
          <div>
            <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Owner</Text>
            <Dropdown value={rOwner} selectedOptions={rOwner ? [rOwner] : []} placeholder="who owns it" onOptionSelect={(_, d) => setROwner(d.optionValue ?? '')} style={{ width: '100%' }}>
              <Option value="">—</Option>
              {owners.map((o) => <Option key={o} value={o}>{o}</Option>)}
            </Dropdown>
          </div>
          <div>
            <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Notes</Text>
            <Input value={rNotes} onChange={(_, d) => setRNotes(d.value)} placeholder="context" style={{ width: '100%' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10 }}>
            <Checkbox checked={rClock} onChange={(_, d) => setRClock(!!d.checked)} label="Stops clock" />
            <Button appearance="primary" disabled={busyBlk || !rc} onClick={() => void raiseBlocker()}>Raise</Button>
          </div>
        </div>
        {blockers.length === 0 ? (
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No open blockers.</Text>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {blockers.map((b) => (
              <div key={b.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px', borderRadius: 6, border: '1px solid var(--colorNeutralStroke2)' }}>
                <Badge appearance="tint" color={b.daysBlocked >= 10 ? 'danger' : b.daysBlocked >= 5 ? 'warning' : 'informative'}>{b.daysBlocked}d</Badge>
                <div style={{ flex: 1 }}>
                  <Text size={300} weight="semibold">{b.category}{b.clockStopped ? '' : ''}</Text>
                  <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
                    {b.owner ? `owner ${b.owner}` : 'no owner'}{b.raisedBy ? ` · by ${b.raisedBy}` : ''}{b.notes ? ` · ${b.notes}` : ''}
                  </Text>
                </div>
                {b.clockStopped && <Badge appearance="tint" color="warning">clock stopped</Badge>}
                <Button size="small" disabled={busyBlk} onClick={() => void resolveBlocker(b)}>Resolve</Button>
              </div>
            ))}
          </div>
        )}
      </Panel>

      <Panel title={`Timeline${events.length ? ` · ${events.length}` : ''}`}>
        {events.length === 0 ? (
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No activity yet.</Text>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {events.map((e) => (
              <div key={e.id} style={{ display: 'flex', alignItems: 'baseline', gap: 10, padding: '4px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                <Badge appearance="tint" color={eventTone(e.type)}>{eventLabel(e.type)}</Badge>
                <div style={{ flex: 1 }}>
                  <Text size={300}>{e.field ?? ''}{e.oldValue && e.newValue ? ` · ${e.oldValue} → ${e.newValue}` : e.newValue ? ` · ${e.newValue}` : ''}</Text>
                  <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
                    {e.byUser ? `${e.byUser} · ` : ''}{new Date(e.atUtc + 'Z').toLocaleString()}
                  </Text>
                </div>
              </div>
            ))}
          </div>
        )}
      </Panel>
        </div>
      </div>
    </div>
  )
}
