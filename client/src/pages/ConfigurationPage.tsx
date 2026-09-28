import { Badge, Button, Dropdown, Input, Option, SpinButton, Text } from '@fluentui/react-components'
import { AddRegular, CheckmarkCircleRegular, DeleteRegular, EditRegular, SaveRegular } from '@fluentui/react-icons'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { RoleCapacity, AcrRates, AcrEstimate } from '../types'

export function ConfigurationPage() {
  const { data, loading, error, reload } = useAsync(() => api.roleCapacity(), [])

  // Local editable copy keyed by role → limit.
  const [edits, setEdits] = useState<Record<string, number>>({})
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (data) setEdits(Object.fromEntries(data.map((r) => [r.roleName, r.capacityLimit])))
  }, [data])

  const dirty = useMemo(
    () => (data ?? []).some((r) => edits[r.roleName] !== r.capacityLimit),
    [data, edits],
  )

  const setLimit = (role: string, value: number) => {
    setSaved(false)
    setEdits((e) => ({ ...e, [role]: Math.max(1, Math.round(value || 1)) }))
  }

  const save = async () => {
    if (!data) return
    setSaving(true)
    try {
      const updates = data
        .filter((r) => edits[r.roleName] !== r.capacityLimit)
        .map((r) => ({ roleName: r.roleName, capacityLimit: edits[r.roleName] }))
      await api.updateRoleCapacity(updates)
      setSaved(true)
      reload()
    } finally {
      setSaving(false)
    }
  }

  const reset = () => {
    if (data) setEdits(Object.fromEntries(data.map((r) => [r.roleName, r.capacityLimit])))
    setSaved(false)
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>
          Configuration
        </Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Tune the governance rules that drive capacity and utilization across the portal.
        </Text>
      </div>

      <Panel
        title="Optimal accounts per role"
        action={
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            {saved && !dirty && (
              <Text size={200} style={{ color: 'var(--colorPaletteGreenForeground1)', display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                <CheckmarkCircleRegular /> Saved
              </Text>
            )}
            <Button appearance="secondary" onClick={reset} disabled={!dirty || saving}>
              Reset
            </Button>
            <Button appearance="primary" icon={<SaveRegular />} onClick={save} disabled={!dirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          </div>
        }
      >
        {loading ? (
          <Loading />
        ) : error ? (
          <ErrorText error={error} onRetry={reload} />
        ) : (
          <>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
              This sets the target number of active accounts a resource of each role can handle. Utilization = active
              accounts ÷ this limit. Saving recalculates every resource's capacity status.
            </Text>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 12 }}>
              {data?.map((r) => (
                <RoleCard key={r.roleName} role={r} value={edits[r.roleName] ?? r.capacityLimit} onChange={(v) => setLimit(r.roleName, v)} />
              ))}
            </div>
          </>
        )}
      </Panel>

      <VocabPanel
        title="Account segments"
        placeholder="New segment name"
        hint="Segments available when tagging accounts (Account Hub). Renaming here does not retag existing accounts."
        load={() => api.segments()}
        add={(n) => api.addSegment(n)}
        update={(id, n) => api.updateSegment(id, n)}
      />
      <VocabPanel
        title="Tool names"
        placeholder="New tool name"
        hint="Controlled tool vocabulary — replaces free-text entry (the biggest source of reporting drift)."
        load={() => api.tools()}
        add={(n) => api.addTool(n)}
        update={(id, n) => api.updateTool(id, n)}
      />
      <VocabPanel
        title="Skills"
        placeholder="New skill name"
        hint="Controlled skill vocabulary used across resources and matching."
        load={() => api.skills()}
        add={(n) => api.addSkill(n)}
        update={(id, n) => api.updateSkill(id, n)}
      />
      <LookupPanel title="Blocker categories" category="BlockerCategory" placeholder="New category" hint="Reasons a nomination can be blocked (Workspace → Raise blocker, Governance Board)." />
      <LookupPanel title="Blocker owners" category="BlockerOwner" placeholder="New owner" hint="Parties that can own a blocker (the Owner dropdown when raising a blocker)." />
      <LookupPanel title="Classifications" category="Classification" placeholder="New classification" hint="Operating-model classifications on a nomination (Standard Factory, Strategic Pilot, …)." />
      <LookupPanel title="Velocity impact" category="VelocityImpact" placeholder="New level" hint="Strategic-account velocity-impact levels." />
      <LookupPanel title="Milestone types" category="Milestone" placeholder="New milestone" hint="Dated app-factory events captured in the workspace (kick-off, runbook shared, actual migration start, …)." />
      <AcrRatesPanel />
      <OperationsSettingsPanel />
      <FiscalTargetsPanel />
    </div>
  )
}

function RoleCard({ role, value, onChange }: { role: RoleCapacity; value: number; onChange: (v: number) => void }) {
  const changed = value !== role.capacityLimit
  return (
    <div
      style={{
        border: `1px solid ${changed ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)'}`,
        borderRadius: 8,
        padding: 14,
        background: 'var(--colorNeutralBackground1)',
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 }}>
        <Text weight="semibold">{role.roleName}</Text>
        {!role.configured && (
          <Text size={100} style={{ color: 'var(--colorNeutralForeground4)', textTransform: 'uppercase', letterSpacing: 0.5 }}>
            default
          </Text>
        )}
      </div>
      {role.description && (
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
          {role.description}
        </Text>
      )}
      <SpinButton
        min={1}
        max={50}
        value={value}
        onChange={(_, d) => onChange(d.value ?? Number(d.displayValue) ?? value)}
        aria-label={`Optimal accounts for ${role.roleName}`}
      />
      <Text size={100} style={{ color: 'var(--colorNeutralForeground4)' }}>
        active accounts / resource
      </Text>
    </div>
  )
}

interface VocabPanelProps {
  title: string
  placeholder: string
  hint: string
  load: () => Promise<{ id: number; name: string; sortOrder: number }[]>
  add: (name: string) => Promise<unknown>
  update: (id: number, name: string) => Promise<unknown>
}

function VocabPanel({ title, placeholder, hint, load, add, update }: VocabPanelProps) {
  const { data, loading, error, reload } = useAsync(load, [])
  const [newName, setNewName] = useState('')
  const [editId, setEditId] = useState<number | null>(null)
  const [editName, setEditName] = useState('')
  const [busy, setBusy] = useState(false)

  const doAdd = async () => {
    const name = newName.trim()
    if (!name) return
    setBusy(true)
    try {
      await add(name)
      setNewName('')
      reload()
    } finally {
      setBusy(false)
    }
  }

  const saveEdit = async () => {
    if (editId == null) return
    const name = editName.trim()
    if (!name) return
    setBusy(true)
    try {
      await update(editId, name)
      setEditId(null)
      setEditName('')
      reload()
    } finally {
      setBusy(false)
    }
  }

  return (
    <Panel
      title={title}
      action={
        <div style={{ display: 'flex', gap: 8 }}>
          <Input
            placeholder={placeholder}
            value={newName}
            onChange={(_, d) => setNewName(d.value)}
            onKeyDown={(e) => e.key === 'Enter' && doAdd()}
            style={{ minWidth: 200 }}
          />
          <Button appearance="primary" icon={<AddRegular />} onClick={doAdd} disabled={!newName.trim() || busy}>
            Add
          </Button>
        </div>
      }
    >
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            {hint}
          </Text>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
            {data?.map((s) =>
              editId === s.id ? (
                <div key={s.id} style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <Input
                    value={editName}
                    onChange={(_, d) => setEditName(d.value)}
                    onKeyDown={(e) => e.key === 'Enter' && saveEdit()}
                    style={{ minWidth: 160 }}
                  />
                  <Button size="small" appearance="primary" icon={<SaveRegular />} onClick={saveEdit} disabled={!editName.trim() || busy} />
                  <Button size="small" appearance="subtle" onClick={() => setEditId(null)} disabled={busy}>
                    Cancel
                  </Button>
                </div>
              ) : (
                <div
                  key={s.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 6,
                    border: '1px solid var(--colorNeutralStroke2)',
                    borderRadius: 999,
                    padding: '4px 6px 4px 12px',
                    background: 'var(--colorNeutralBackground1)',
                  }}
                >
                  <Text>{s.name}</Text>
                  <Button
                    size="small"
                    appearance="subtle"
                    icon={<EditRegular />}
                    title="Rename"
                    onClick={() => {
                      setEditId(s.id)
                      setEditName(s.name)
                    }}
                  />
                </div>
              ),
            )}
          </div>
        </>
      )}
    </Panel>
  )
}

function LookupPanel({ title, category, placeholder, hint }: { title: string; category: string; placeholder: string; hint: string }) {
  const { data, loading, error, reload } = useAsync(() => api.lookups(category), [category])
  const [newVal, setNewVal] = useState('')
  const [editId, setEditId] = useState<number | null>(null)
  const [editVal, setEditVal] = useState('')
  const [busy, setBusy] = useState(false)

  const doAdd = async () => {
    const v = newVal.trim()
    if (!v) return
    setBusy(true)
    try { await api.addLookup(category, v); setNewVal(''); reload() } finally { setBusy(false) }
  }
  const saveEdit = async () => {
    if (editId == null) return
    const v = editVal.trim()
    if (!v) return
    setBusy(true)
    try { await api.updateLookup(editId, v); setEditId(null); setEditVal(''); reload() } finally { setBusy(false) }
  }
  const doDelete = async (id: number, value: string) => {
    if (!confirm(`Delete "${value}" from ${title}?`)) return
    setBusy(true)
    try { await api.deleteLookup(id); reload() } finally { setBusy(false) }
  }

  return (
    <Panel
      title={title}
      action={
        <div style={{ display: 'flex', gap: 8 }}>
          <Input placeholder={placeholder} value={newVal} onChange={(_, d) => setNewVal(d.value)} onKeyDown={(e) => e.key === 'Enter' && doAdd()} style={{ minWidth: 200 }} />
          <Button appearance="primary" icon={<AddRegular />} onClick={doAdd} disabled={!newVal.trim() || busy}>Add</Button>
        </div>
      }
    >
      {loading ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>{hint}</Text>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
            {data?.map((s) =>
              editId === s.id ? (
                <div key={s.id} style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <Input value={editVal} onChange={(_, d) => setEditVal(d.value)} onKeyDown={(e) => e.key === 'Enter' && saveEdit()} style={{ minWidth: 160 }} />
                  <Button size="small" appearance="primary" icon={<SaveRegular />} onClick={saveEdit} disabled={!editVal.trim() || busy} />
                  <Button size="small" appearance="subtle" onClick={() => setEditId(null)} disabled={busy}>Cancel</Button>
                </div>
              ) : (
                <div key={s.id} style={{ display: 'flex', alignItems: 'center', gap: 4, border: '1px solid var(--colorNeutralStroke2)', borderRadius: 999, padding: '4px 4px 4px 12px', background: 'var(--colorNeutralBackground1)' }}>
                  <Text>{s.value}</Text>
                  <Button size="small" appearance="subtle" icon={<EditRegular />} title="Rename" onClick={() => { setEditId(s.id); setEditVal(s.value) }} />
                  <Button size="small" appearance="subtle" icon={<DeleteRegular />} title="Delete" onClick={() => void doDelete(s.id, s.value)} disabled={busy} />
                </div>
              ),
            )}
          </div>
        </>
      )}
    </Panel>
  )
}

// Editable FDO ACR calculation rates (App Factory thumb-rules): rate master + live estimator.
function AcrRatesPanel() {
  const { data, loading, error, reload } = useAsync(() => api.acrRates(), [])
  const [edits, setEdits] = useState<AcrRates | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [svc, setSvc] = useState('AppService')
  const [apps, setApps] = useState('10')
  const [cores, setCores] = useState('')
  const [est, setEst] = useState<AcrEstimate | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => { if (data) setEdits(data) }, [data])
  const set = (k: keyof AcrRates, v: string) => setEdits((e) => (e ? { ...e, [k]: Number(v) } : e))
  const save = async () => {
    if (!edits) return
    setSaving(true)
    try { await api.saveAcrRates(edits); setSaved(true); setTimeout(() => setSaved(false), 2500) } finally { setSaving(false) }
  }
  const estimate = async () => {
    setBusy(true)
    try { setEst(await api.acrEstimate({ targetService: svc, apps: apps ? Number(apps) : null, cores: cores ? Number(cores) : null })) } finally { setBusy(false) }
  }
  const money = (n: number) => `$${Math.round(n).toLocaleString()}`

  const num = (label: string, k: keyof AcrRates) => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Text size={100} style={{ color: 'var(--colorNeutralForeground3)' }}>{label}</Text>
      <Input type="number" value={String(edits?.[k] ?? '')} onChange={(_, d) => set(k, d.value)} style={{ width: 140 }} />
    </div>
  )

  return (
    <Panel
      title="ACR calculation rates"
      action={
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {saved && <Text size={200} style={{ color: 'var(--colorPaletteGreenForeground1)', display: 'inline-flex', alignItems: 'center', gap: 4 }}><CheckmarkCircleRegular /> Saved</Text>}
          <Button appearance="primary" icon={<SaveRegular />} onClick={save} disabled={saving || !edits}>{saving ? 'Saving…' : 'Save rates'}</Button>
        </div>
      }
    >
      {loading ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : edits ? (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            FDO App Factory thumb-rules: Annualized ACR = monthly consumption × months. App Service $98/core/mo (1 app = 2 cores),
            AKS Linux $30, AKS Windows $56 (1 app ≈ 4 cores), ACA = ARPU/core/hr × cores × utilization × hours/mo. Editable — the estimator below uses these.
          </Text>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 18 }}>
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>{num('Annualization (months)', 'annualizationMonths')}</div>
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', paddingLeft: 12, borderLeft: '1px solid var(--colorNeutralStroke2)' }}>
              {num('App Service $/core/mo', 'appServiceArpuPerCoreMonth')}
              {num('App Service cores/app', 'appServiceCoresPerApp')}
            </div>
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', paddingLeft: 12, borderLeft: '1px solid var(--colorNeutralStroke2)' }}>
              {num('AKS Linux $/core/mo', 'aksLinuxArpuPerCoreMonth')}
              {num('AKS Windows $/core/mo', 'aksWindowsArpuPerCoreMonth')}
              {num('AKS cores/app', 'aksCoresPerApp')}
            </div>
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', paddingLeft: 12, borderLeft: '1px solid var(--colorNeutralStroke2)' }}>
              {num('ACA $/core/hr', 'acaArpuPerCoreHour')}
              {num('ACA utilization', 'acaUtilization')}
              {num('ACA hours/mo', 'acaHoursPerMonth')}
            </div>
          </div>

          <div style={{ marginTop: 16, paddingTop: 14, borderTop: '1px solid var(--colorNeutralStroke2)' }}>
            <Text weight="semibold" style={{ display: 'block', marginBottom: 8 }}>Estimator</Text>
            <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end', flexWrap: 'wrap' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Text size={100} style={{ color: 'var(--colorNeutralForeground3)' }}>Target service</Text>
                <Dropdown value={svc} selectedOptions={[svc]} onOptionSelect={(_, d) => setSvc(d.optionValue ?? svc)} style={{ minWidth: 150 }}>
                  <Option value="AppService">App Service</Option>
                  <Option value="AksLinux">AKS Linux</Option>
                  <Option value="AksWindows">AKS Windows</Option>
                  <Option value="Aca">ACA</Option>
                </Dropdown>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Text size={100} style={{ color: 'var(--colorNeutralForeground3)' }}>Apps in scope</Text>
                <Input type="number" value={apps} onChange={(_, d) => setApps(d.value)} style={{ width: 120 }} />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Text size={100} style={{ color: 'var(--colorNeutralForeground3)' }}>Cores (override)</Text>
                <Input type="number" value={cores} onChange={(_, d) => setCores(d.value)} placeholder="auto" style={{ width: 120 }} />
              </div>
              <Button appearance="secondary" onClick={estimate} disabled={busy}>Estimate</Button>
            </div>
            {est && (
              <div style={{ marginTop: 10, display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
                <Badge appearance="tint" color="brand">{est.cores} cores</Badge>
                <Badge appearance="tint" color="informative">{money(est.monthlyAcr)}/mo</Badge>
                <Badge appearance="filled" color="success">{money(est.annualAcr)}/yr</Badge>
                <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>{est.formula}</Text>
              </div>
            )}
          </div>
        </>
      ) : null}
    </Panel>
  )
}

// Annual ACR (Azure Consumed Revenue) targets by fiscal year — the plan the attainment view measures against.
function FiscalTargetsPanel() {
  const { data, loading, error, reload } = useAsync(() => api.acrTargets(), [])
  const [edits, setEdits] = useState<Record<number, number>>({})
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [newFy, setNewFy] = useState(() => { const d = new Date(); return d.getMonth() + 1 >= 7 ? d.getFullYear() + 1 : d.getFullYear() })

  useEffect(() => {
    if (data) setEdits(Object.fromEntries(data.map((t) => [t.fiscalYear, t.target])))
  }, [data])

  const dirty = useMemo(() => {
    const base = new Map((data ?? []).map((t) => [t.fiscalYear, t.target]))
    const keys = new Set<number>([...base.keys(), ...Object.keys(edits).map(Number)])
    return [...keys].some((fy) => (base.get(fy) ?? 0) !== (edits[fy] ?? 0))
  }, [data, edits])

  const fmt = (n: number) =>
    n >= 1e9 ? `$${(n / 1e9).toFixed(2)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(2)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`
  const label = (fy: number) => `FY${String(fy % 100).padStart(2, '0')}`

  const addFy = () => {
    if (edits[newFy] != null) return
    setSaved(false)
    setEdits((e) => ({ ...e, [newFy]: 0 }))
  }

  const save = async () => {
    setSaving(true)
    try {
      // Send every current row; the API upserts and treats target<=0 as a removal.
      const updates = Object.entries(edits).map(([fy, target]) => ({ fiscalYear: Number(fy), target: Number(target) || 0 }))
      await api.updateAcrTargets(updates)
      setSaved(true)
      reload()
    } finally {
      setSaving(false)
    }
  }

  const years = useMemo(
    () => [...new Set([...(data ?? []).map((t) => t.fiscalYear), ...Object.keys(edits).map(Number)])].sort((a, b) => b - a),
    [data, edits],
  )

  return (
    <Panel
      title="Fiscal-year ACR targets"
      action={
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {saved && !dirty && (
            <Text size={200} style={{ color: 'var(--colorPaletteGreenForeground1)', display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              <CheckmarkCircleRegular /> Saved
            </Text>
          )}
          <Button appearance="primary" icon={<SaveRegular />} onClick={save} disabled={!dirty || saving}>
            {saving ? 'Saving\u2026' : 'Save changes'}
          </Button>
        </div>
      }
    >
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            Annual ACR (Azure Consumed Revenue) plan per fiscal year (Jul\u2013Jun, labelled by end year). This is the
            target the migration attainment view measures completed + in-flight ACR against. Set a target to 0 to remove
            a year.
          </Text>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: 12 }}>
            {years.map((fy) => {
              const changed = (data?.find((t) => t.fiscalYear === fy)?.target ?? 0) !== (edits[fy] ?? 0)
              return (
                <div
                  key={fy}
                  style={{
                    border: `1px solid ${changed ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)'}`,
                    borderRadius: 8, padding: 14, display: 'flex', flexDirection: 'column', gap: 8,
                    background: 'var(--colorNeutralBackground1)',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                    <Text weight="semibold">{label(fy)}</Text>
                    <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>{fmt(edits[fy] ?? 0)}</Text>
                  </div>
                  <Input
                    type="number"
                    min={0}
                    value={String(edits[fy] ?? 0)}
                    onChange={(_, d) => { setSaved(false); setEdits((e) => ({ ...e, [fy]: Math.max(0, Math.round(Number(d.value) || 0)) })) }}
                    contentBefore={<Text size={200}>$</Text>}
                    aria-label={`${label(fy)} ACR target`}
                  />
                </div>
              )
            })}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', marginTop: 14 }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Text size={100} style={{ color: 'var(--colorNeutralForeground3)', textTransform: 'uppercase', letterSpacing: 0.4 }}>Add fiscal year</Text>
              <SpinButton min={2000} max={2100} value={newFy} onChange={(_, d) => setNewFy(Math.round(d.value ?? Number(d.displayValue) ?? newFy))} style={{ width: 120 }} aria-label="New fiscal year" />
            </div>
            <Button appearance="secondary" icon={<AddRegular />} onClick={addFy} disabled={edits[newFy] != null}>Add {label(newFy)}</Button>
          </div>
        </>
      )}
    </Panel>
  )
}

const SETTING_LABELS: Record<string, string> = {
  StaleWarnDays: 'Stale · Warn after (days)',
  StaleEscalateDays: 'Stale · Escalate after (days)',
  StaleDeferDays: 'Stale · Suggest Deferred after (days)',
  StageTargetDays1: 'Stage 1 target · Validating (days)',
  StageTargetDays2: 'Stage 2 target · Pre-Requisites (days)',
  StageTargetDays3: 'Stage 3 target · Finalize Scope (days)',
  StageTargetDays4: 'Stage 4 target · Executing Migration (days)',
  LeaveClashWindowDays: 'Leave-clash window (days)',
}
const SETTING_ORDER = [
  'StaleWarnDays',
  'StaleEscalateDays',
  'StaleDeferDays',
  'StageTargetDays1',
  'StageTargetDays2',
  'StageTargetDays3',
  'StageTargetDays4',
  'LeaveClashWindowDays',
]

function OperationsSettingsPanel() {
  const { data, loading, error, reload } = useAsync(() => api.operationsSettings(), [])
  const [edits, setEdits] = useState<Record<string, number>>({})
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (data) setEdits(Object.fromEntries(data.map((s) => [s.key, Number(s.value) || 1])))
  }, [data])

  const dirty = useMemo(
    () => (data ?? []).some((s) => edits[s.key] !== Number(s.value)),
    [data, edits],
  )

  const save = async () => {
    if (!data) return
    setSaving(true)
    try {
      const updates = data
        .filter((s) => edits[s.key] !== Number(s.value))
        .map((s) => ({ key: s.key, value: String(edits[s.key]) }))
      await api.updateOperationsSettings(updates)
      setSaved(true)
      reload()
    } finally {
      setSaving(false)
    }
  }

  const ordered = useMemo(
    () => [...(data ?? [])].sort((a, b) => SETTING_ORDER.indexOf(a.key) - SETTING_ORDER.indexOf(b.key)),
    [data],
  )

  return (
    <Panel
      title="Operational thresholds"
      action={
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {saved && !dirty && (
            <Text size={200} style={{ color: 'var(--colorPaletteGreenForeground1)', display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              <CheckmarkCircleRegular /> Saved
            </Text>
          )}
          <Button appearance="primary" icon={<SaveRegular />} onClick={save} disabled={!dirty || saving}>
            {saving ? 'Saving…' : 'Save changes'}
          </Button>
        </div>
      }
    >
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            Drives nomination stale detection (Day 3/5/10 cadence) and the leave-clash alert window.
          </Text>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 12 }}>
            {ordered.map((s) => (
              <div
                key={s.key}
                style={{
                  border: `1px solid ${edits[s.key] !== Number(s.value) ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)'}`,
                  borderRadius: 8,
                  padding: 14,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 8,
                  background: 'var(--colorNeutralBackground1)',
                }}
              >
                <Text weight="semibold">{SETTING_LABELS[s.key] ?? s.key}</Text>
                {s.description && (
                  <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                    {s.description}
                  </Text>
                )}
                <SpinButton
                  min={1}
                  max={365}
                  value={edits[s.key] ?? Number(s.value)}
                  onChange={(_, d) => {
                    setSaved(false)
                    setEdits((e) => ({ ...e, [s.key]: Math.max(1, Math.round(d.value ?? Number(d.displayValue) ?? Number(s.value))) }))
                  }}
                  aria-label={SETTING_LABELS[s.key] ?? s.key}
                />
              </div>
            ))}
          </div>
        </>
      )}
    </Panel>
  )
}
