import { Badge, Text, Tooltip } from '@fluentui/react-components'
import {
  CheckmarkCircleFilled,
  CircleHalfFillRegular,
  CircleRegular,
  DocumentLinkRegular,
} from '@fluentui/react-icons'
import { Fragment, useEffect, useMemo, useState } from 'react'
import { useParams, Link as RouterLink } from 'react-router-dom'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import type { Gate, GateItem, Governance } from '../types'

const KIND_TONE: Record<string, 'brand' | 'success' | 'warning' | 'informative' | 'subtle'> = {
  Task: 'informative', Prerequisite: 'warning', Deliverable: 'brand', Approval: 'success', Signoff: 'success',
}

function stepIcon(g: Gate) {
  if (g.status === 'Green') return <CheckmarkCircleFilled style={{ color: '#107c10' }} />
  if (g.status === 'InProgress') return <CircleHalfFillRegular style={{ color: '#0f6cbd' }} />
  return <CircleRegular style={{ color: '#8a8886' }} />
}

export function NominationWorkspacePage() {
  const { id } = useParams<{ id: string }>()
  const nominationId = Number(id)
  const [gov, setGov] = useState<Governance | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

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
  useEffect(() => { void load() }, [nominationId])

  const current = useMemo(() => gov?.gates.find((g) => g.key === selected) ?? gov?.gates[0], [gov, selected])

  const toggle = async (item: GateItem) => {
    if (!gov || saving) return
    setSaving(true)
    try {
      const next = item.status === 'Done' ? 'Pending' : 'Done'
      const updated = await api.updateGovernanceItem(nominationId, item.itemDefId, { status: next, owner: item.owner, ref: item.ref, notes: item.notes })
      setGov(updated)
    } finally {
      setSaving(false)
    }
  }

  if (loading && !gov) return <Loading label="Loading workspace…" />
  if (error) return <ErrorText error={error} onRetry={load} />
  if (!gov || !current) return null

  const pending = current.items.filter((i) => i.status !== 'Done').length

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <Text size={600} weight="bold">{gov.account ?? `Nomination ${gov.nominationId}`}</Text>
            {gov.tpid && <Badge appearance="outline">TPID {gov.tpid}</Badge>}
            <RouterLink to="/nominations" style={{ fontSize: 12 }}>← Nominations</RouterLink>
          </div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>SA governance workspace · gated checklist</Text>
        </div>
        <KpiCard label="Readiness compliance" value={`${gov.compliancePercent}%`} tone={gov.compliancePercent >= 80 ? 'success' : gov.compliancePercent >= 50 ? 'brand' : 'neutral'} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '270px 1fr', gap: 16, alignItems: 'start' }}>
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
                  {stepIcon(g)}
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
          action={<Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>{pending === 0 ? 'Complete' : `${pending} pending`} · weight {current.weight}%</Text>}
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
                    <input type="checkbox" checked={i.status === 'Done'} disabled={saving} onChange={() => toggle(i)} aria-label={i.label} />
                    <div style={{ flex: 1 }}>
                      <Text size={300} weight={i.mandatory ? 'semibold' : 'regular'}>{i.label}{i.mandatory ? ' ⚑' : ''}</Text>
                      <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
                        {i.responsibleRole}{i.ref ? ' · ' : ''}{i.ref && <a href="#" onClick={(e) => e.preventDefault()}><DocumentLinkRegular /> {i.ref}</a>}
                        {i.updatedBy ? ` · by ${i.updatedBy}` : ''}
                      </Text>
                    </div>
                    <Badge appearance="tint" color={KIND_TONE[i.kind] ?? 'informative'}>{i.kind}</Badge>
                  </div>
                </Fragment>
              )
            })}
          </div>
        </Panel>
      </div>
      <Text size={100} style={{ color: 'var(--colorNeutralForeground4)' }}>
        <Tooltip relationship="label" content="Blockers, timeline and gate-advance land in P2"><span>P1 · gated checklist</span></Tooltip>
      </Text>
    </div>
  )
}
