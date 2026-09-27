import { Badge, Button, Text, Tooltip } from '@fluentui/react-components'
import {
  AddRegular,
  CheckmarkCircleFilled,
  CircleHalfFillRegular,
  CircleRegular,
  ErrorCircleFilled,
  DocumentLinkRegular,
} from '@fluentui/react-icons'
import { Fragment, useMemo, useState } from 'react'
import { Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'

// ── Mock domain (P0 preview — no backend). Mirrors FACTORY-OPERATING-SYSTEM.md gate template. ──
type Kind = 'Task' | 'Prerequisite' | 'Deliverable' | 'Approval' | 'Signoff'
type ItemStatus = 'Pending' | 'Done'
interface Item { id: string; label: string; kind: Kind; mandatory?: boolean; status: ItemStatus; owner?: string; ref?: string; blocked?: boolean; group?: string }
interface Gate { key: string; name: string; weight: number; exit: string; items: Item[] }

const KIND_TONE: Record<Kind, 'brand' | 'success' | 'warning' | 'informative' | 'subtle'> = {
  Task: 'informative', Prerequisite: 'warning', Deliverable: 'brand', Approval: 'success', Signoff: 'success',
}

// Seeded nomination: G1–G3 complete, G4 in progress (with a blocker), G5–G8 pending.
const SEED: Gate[] = [
  {
    key: 'G1', name: 'Discovery & Readiness', weight: 10,
    exit: 'Discovery complete · Dependency map · Risks captured · Readiness updated',
    items: [
      { id: 'g1a', label: 'Customer discovery sessions', kind: 'Task', status: 'Done' },
      { id: 'g1b', label: 'App inventory validated', kind: 'Task', status: 'Done' },
      { id: 'g1c', label: 'Dependencies identified', kind: 'Task', status: 'Done' },
      { id: 'g1d', label: 'Risks & assumptions documented', kind: 'Deliverable', status: 'Done' },
    ],
  },
  {
    key: 'G2', name: 'Prerequisites', weight: 15,
    exit: 'Prereq tracker complete · Owners identified · Open blockers visible',
    items: [
      { id: 'g2a', label: 'GHCP Enterprise License', kind: 'Prerequisite', status: 'Done' },
      { id: 'g2b', label: 'Repository access', kind: 'Prerequisite', status: 'Done' },
      { id: 'g2c', label: 'Environment access', kind: 'Prerequisite', status: 'Done' },
      { id: 'g2d', label: 'Landing zone ready', kind: 'Prerequisite', status: 'Done' },
    ],
  },
  {
    key: 'G3', name: 'Assessment', weight: 15,
    exit: 'Assessment report · Migration strategy approved · Risk register',
    items: [
      { id: 'g3a', label: 'AppCAT / Azure Migrate assessment', kind: 'Task', status: 'Done' },
      { id: 'g3b', label: 'Migration approach finalized', kind: 'Task', status: 'Done' },
      { id: 'g3c', label: 'Effort sizing', kind: 'Task', status: 'Done' },
    ],
  },
  {
    key: 'G4', name: 'Scope Governance', weight: 20,
    exit: 'Signed scope document · No ownership ambiguity · FDO updated',
    items: [
      { id: 'g4a', label: 'Scope document created', kind: 'Deliverable', status: 'Done', ref: 'sharepoint/scope-sgmr.docx' },
      { id: 'g4b', label: 'In-scope defined', kind: 'Task', status: 'Done' },
      { id: 'g4c', label: 'Out-of-scope defined', kind: 'Task', status: 'Pending', owner: 'SA' },
      { id: 'g4d', label: 'Customer responsibilities', kind: 'Task', status: 'Pending', owner: 'Customer', blocked: true },
      { id: 'g4e', label: 'Acceptance criteria', kind: 'Task', status: 'Pending', owner: 'SA' },
      { id: 'g4f', label: 'Signed scope document', kind: 'Signoff', mandatory: true, status: 'Pending', owner: 'Customer' },
    ],
  },
  { key: 'G5', name: 'Architecture', weight: 10, exit: 'TAD approved · Customer approval · Arch risks closed', items: [
    { id: 'g5a', label: 'TAD prepared', kind: 'Deliverable', status: 'Pending' },
    { id: 'g5b', label: 'Security review', kind: 'Approval', status: 'Pending' },
    { id: 'g5c', label: 'Customer signoff', kind: 'Signoff', mandatory: true, status: 'Pending' },
  ] },
  { key: 'G6', name: 'Delivery Readiness', weight: 8, exit: 'Engineering-ready', items: [
    { id: 'g6a', label: 'Scope frozen', kind: 'Task', status: 'Pending' },
    { id: 'g6b', label: 'Environments ready', kind: 'Prerequisite', status: 'Pending' },
    { id: 'g6c', label: 'Rollback strategy defined', kind: 'Task', status: 'Pending' },
  ] },
  { key: 'G7', name: 'Delivery Governance', weight: 7, exit: 'Progress in systems · Issues escalated in time · SA-tracked, engineers execute', items: [
    { id: 'g7m1', label: 'Version upgrade', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Modernization' },
    { id: 'g7m2', label: 'Code remediation', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Modernization' },
    { id: 'g7m3', label: 'Dependency upgrade', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Modernization' },
    { id: 'g7m4', label: 'Security fixes', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Modernization' },
    { id: 'g7c1', label: 'Dockerfile', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'Containerization' },
    { id: 'g7c2', label: 'Container image', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'Containerization' },
    { id: 'g7c3', label: 'Registry push', kind: 'Task', status: 'Pending', owner: 'Eng · S.Rao', group: 'Containerization' },
    { id: 'g7i1', label: 'Bicep', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'IaC' },
    { id: 'g7i2', label: 'Terraform', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'IaC' },
    { id: 'g7i3', label: 'Helm charts', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'IaC' },
    { id: 'g7i4', label: 'AKS manifests', kind: 'Deliverable', status: 'Pending', owner: 'Eng · S.Rao', group: 'IaC' },
    { id: 'g7p1', label: 'Build pipeline', kind: 'Task', status: 'Pending', owner: 'Eng · S.Rao', group: 'CI/CD' },
    { id: 'g7p2', label: 'Release pipeline', kind: 'Task', status: 'Pending', owner: 'Eng · S.Rao', group: 'CI/CD' },
    { id: 'g7p3', label: 'Deployment validation', kind: 'Task', status: 'Pending', owner: 'Eng · S.Rao', group: 'CI/CD' },
    { id: 'g7x1', label: 'Migrate to AKS (target)', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Execution' },
    { id: 'g7x2', label: 'Smoke validation', kind: 'Task', status: 'Pending', owner: 'Eng · R.Kumar', group: 'Execution' },
    { id: 'g7g1', label: 'Weekly status · FDO hygiene', kind: 'Task', status: 'Pending', owner: 'SA', group: 'Governance (SA)' },
  ] },
  { key: 'G8', name: 'Closure', weight: 5, exit: 'Customer signoff · FDO closure · Lessons learned', items: [
    { id: 'g8a', label: 'UAT completed', kind: 'Task', status: 'Pending' },
    { id: 'g8b', label: 'Handover document', kind: 'Deliverable', status: 'Pending' },
    { id: 'g8c', label: 'Closure report', kind: 'Deliverable', mandatory: true, status: 'Pending' },
  ] },
]

interface Blocker { id: string; category: string; owner: string; since: string; clockStopped: boolean; itemId?: string }
const SEED_BLOCKERS: Blocker[] = [
  { id: 'b1', category: 'Awaiting Customer Approval', owner: 'Customer', since: '2026-08-20', clockStopped: true, itemId: 'g4d' },
]

const TIMELINE = [
  { at: '2026-08-20 10:12', by: 'you', text: 'Raised blocker · Awaiting Customer Approval (clock stopped)' },
  { at: '2026-08-12 16:40', by: 'you', text: 'Scope document created → Done · attached sharepoint/scope-sgmr.docx' },
  { at: '2026-08-05 09:03', by: 'you', text: 'Gate G3 Assessment → Green' },
]

const gatePct = (g: Gate) => (g.items.length ? Math.round((g.items.filter((i) => i.status === 'Done').length / g.items.length) * 100) : 0)

export function WorkspaceMockPage() {
  const [gates, setGates] = useState<Gate[]>(SEED)
  const [selected, setSelected] = useState('G4')
  const [blockers, setBlockers] = useState<Blocker[]>(SEED_BLOCKERS)
  const [showBlockerForm, setShowBlockerForm] = useState(false)
  const [advanceWarn, setAdvanceWarn] = useState(false)
  const [pendingItem, setPendingItem] = useState<string | undefined>()

  const current = gates.find((g) => g.key === selected)!
  const compliance = useMemo(() => {
    const totW = gates.reduce((s, g) => s + g.weight, 0)
    return Math.round(gates.reduce((s, g) => s + g.weight * gatePct(g), 0) / totW)
  }, [gates])
  // Mock MSI: readiness-weighted with a fixed risk/adoption contribution for the preview.
  const msi = Math.round(compliance * 0.7 + 30 * 0.3 + 10)
  const msiTone = msi > 80 ? 'success' : msi >= 60 ? 'warning' : 'danger'

  const toggle = (itemId: string) =>
    setGates((gs) => gs.map((g) => g.key !== selected ? g : {
      ...g, items: g.items.map((i) => i.id === itemId ? { ...i, status: i.status === 'Done' ? 'Pending' : 'Done', blocked: false } : i),
    }))

  const openItemBlocker = (itemId: string) => { setShowBlockerForm(true); setPendingItem(itemId) }

  const addBlocker = (category: string, clockStopped: boolean) => {
    setBlockers((b) => [...b, { id: `b${b.length + 1}`, category, owner: 'Customer', since: new Date().toISOString().slice(0, 10), clockStopped, itemId: pendingItem }])
    if (pendingItem) setGates((gs) => gs.map((g) => g.key !== selected ? g : { ...g, items: g.items.map((i) => i.id === pendingItem ? { ...i, blocked: true } : i) }))
    setShowBlockerForm(false); setPendingItem(undefined)
  }
  const resolveBlocker = (id: string) => setBlockers((b) => b.filter((x) => x.id !== id))

  const pendingCount = current.items.filter((i) => i.status !== 'Done').length
  const openBlockers = blockers.length

  const stepIcon = (g: Gate) => {
    const p = gatePct(g)
    if (blockers.some((b) => current.key === g.key && b.itemId && g.items.some((i) => i.id === b.itemId))) return <ErrorCircleFilled style={{ color: '#c50f1f' }} />
    if (p === 100) return <CheckmarkCircleFilled style={{ color: '#107c10' }} />
    if (p > 0) return <CircleHalfFillRegular style={{ color: '#0f6cbd' }} />
    return <CircleRegular style={{ color: '#8a8886' }} />
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <Text size={600} weight="bold">SocGen · SGMR</Text>
            <Badge appearance="tint" color="brand">Strategic Pilot</Badge>
            <Badge appearance="outline">FDO Stage 3</Badge>
            <Badge appearance="outline">TPID 12345</Badge>
          </div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            PM: A. Rana · CFTL: J. Doe · SA: you — Age 84d · 32d clock-stopped · <b>preview / mock data</b>
          </Text>
        </div>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <KpiCard label="Readiness compliance" value={`${compliance}%`} tone="brand" />
          <KpiCard label="Migration Success Index" value={String(msi)} tone={msiTone} />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '260px 1fr 300px', gap: 16, alignItems: 'start' }}>
        {/* Left — gate stepper */}
        <Panel title="Governance gates">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {gates.map((g) => {
              const active = g.key === selected
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
                    <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>weight {g.weight}% · {gatePct(g)}%</Text>
                  </div>
                </button>
              )
            })}
          </div>
        </Panel>

        {/* Center — current gate checklist */}
        <Panel
          title={`${current.key} · ${current.name}`}
          action={
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>weight {current.weight}%</Text>
              <Button appearance="primary" onClick={() => setAdvanceWarn(pendingCount > 0)} disabled={pendingCount === 0}>
                Advance ▸
              </Button>
            </div>
          }
        >
          <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)', marginBottom: 10 }}>
            Exit: {current.exit}
          </Text>
          {advanceWarn && pendingCount > 0 && (
            <div style={{ background: 'var(--colorStatusWarningBackground1)', borderRadius: 6, padding: '6px 10px', marginBottom: 10 }}>
              <Text size={200}>{pendingCount} item(s) still pending — soft gate: advance anyway? (mock)</Text>
            </div>
          )}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {current.items.map((i, idx) => {
              const showGroup = i.group && i.group !== current.items[idx - 1]?.group
              return (
              <Fragment key={i.id}>
                {showGroup && (
                  <Text size={200} weight="semibold" style={{ marginTop: idx ? 8 : 0, color: 'var(--colorNeutralForeground2)' }}>{i.group}</Text>
                )}
              <div
                style={{
                  display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px', borderRadius: 6,
                  border: '1px solid var(--colorNeutralStroke2)',
                  background: i.status === 'Done' ? 'var(--colorNeutralBackground2)' : 'var(--colorNeutralBackground1)',
                }}
              >
                <input type="checkbox" checked={i.status === 'Done'} onChange={() => toggle(i.id)} aria-label={i.label} />
                <div style={{ flex: 1 }}>
                  <Text size={300} weight={i.mandatory ? 'semibold' : 'regular'}>
                    {i.label}{i.mandatory ? ' ⚑' : ''}
                  </Text>
                  {(i.owner || i.ref) && (
                    <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>
                      {i.owner ? `owner: ${i.owner}` : ''}{i.ref ? `  ·  ` : ''}
                      {i.ref && <a href="#" onClick={(e) => e.preventDefault()}><DocumentLinkRegular /> {i.ref}</a>}
                    </Text>
                  )}
                </div>
                <Badge appearance="tint" color={KIND_TONE[i.kind]}>{i.kind}</Badge>
                {i.blocked ? (
                  <Badge appearance="tint" color="danger">blocked</Badge>
                ) : i.status !== 'Done' ? (
                  <Tooltip relationship="label" content="Raise blocker on this item">
                    <Button size="small" appearance="subtle" icon={<ErrorCircleFilled />} onClick={() => openItemBlocker(i.id)} />
                  </Tooltip>
                ) : null}
              </div>
              </Fragment>
              )
            })}
          </div>
        </Panel>

        {/* Right — blockers + timeline */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <Panel
            title={`Blockers (${openBlockers} open)`}
            action={<Button size="small" icon={<AddRegular />} onClick={() => { setPendingItem(undefined); setShowBlockerForm(true) }}>Raise</Button>}
          >
            {showBlockerForm && (
              <div style={{ border: '1px solid var(--colorBrandStroke1)', borderRadius: 8, padding: 10, marginBottom: 10, display: 'flex', flexDirection: 'column', gap: 8 }}>
                <Text size={200} weight="semibold">New blocker {pendingItem ? '(on selected item)' : ''}</Text>
                {['Awaiting Customer Approval', 'Awaiting Repository Access', 'Awaiting Security Review'].map((c) => (
                  <Button key={c} size="small" appearance="secondary" onClick={() => addBlocker(c, true)}>{c} · clock-stop</Button>
                ))}
                <Button size="small" appearance="subtle" onClick={() => setShowBlockerForm(false)}>Cancel</Button>
              </div>
            )}
            {blockers.length === 0 ? (
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>No open blockers.</Text>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {blockers.map((b) => (
                  <div key={b.id} style={{ border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6, padding: 8 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 6 }}>
                      <Text size={200} weight="semibold">{b.category}</Text>
                      {b.clockStopped && <Badge appearance="tint" color="warning">⏸ clock-stopped</Badge>}
                    </div>
                    <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>owner: {b.owner} · since {b.since}</Text>
                    <Button size="small" appearance="subtle" onClick={() => resolveBlocker(b.id)}>Resolve</Button>
                  </div>
                ))}
              </div>
            )}
          </Panel>

          <Panel title="Timeline">
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {TIMELINE.map((t, idx) => (
                <div key={idx}>
                  <Text size={200}>{t.text}</Text>
                  <Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>{t.by} · {t.at}</Text>
                </div>
              ))}
            </div>
          </Panel>
        </div>
      </div>
    </div>
  )
}
