import { Button, SpinButton, Text } from '@fluentui/react-components'
import { CheckmarkCircleRegular, SaveRegular } from '@fluentui/react-icons'
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
