import { Button, Input, SpinButton, Text } from '@fluentui/react-components'
import { AddRegular, CheckmarkCircleRegular, EditRegular, SaveRegular } from '@fluentui/react-icons'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { RoleCapacity } from '../types'

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
      <OperationsSettingsPanel />
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

const SETTING_LABELS: Record<string, string> = {
  StaleWarnDays: 'Stale · Warn after (days)',
  StaleEscalateDays: 'Stale · Escalate after (days)',
  StaleDeferDays: 'Stale · Suggest Deferred after (days)',
  LeaveClashWindowDays: 'Leave-clash window (days)',
}
const SETTING_ORDER = ['StaleWarnDays', 'StaleEscalateDays', 'StaleDeferDays', 'LeaveClashWindowDays']

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
