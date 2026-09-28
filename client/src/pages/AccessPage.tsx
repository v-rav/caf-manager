import { Badge, Button, Checkbox, Dropdown, Field, Input, Option, Text } from '@fluentui/react-components'
import { AddRegular, PeopleTeamRegular, SaveRegular } from '@fluentui/react-icons'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import { useAuth } from '../auth'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { AppUser, PageAccess, UserRole } from '../types'

const ROLES: UserRole[] = ['Admin', 'Lead', 'Sa']
const ROLE_LABEL: Record<UserRole, string> = { Admin: 'Admin', Lead: 'Lead', Sa: 'SA' }
// Roles that can be toggled in the matrix. Admin always has access (implicit, locked).
const GRANTABLE: UserRole[] = ['Lead', 'Sa']

export function AccessPage() {
  const { user } = useAuth()
  if (user?.role !== 'Admin') {
    return (
      <div style={{ display: 'grid', placeItems: 'center', minHeight: '50vh', textAlign: 'center' }}>
        <div>
          <Text size={500} weight="bold" style={{ display: 'block' }}>Admins only</Text>
          <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>User and access management is restricted to administrators.</Text>
        </div>
      </div>
    )
  }
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>User & Access</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Manage portal logins and control which pages each role can open. Administrators always see every page.
        </Text>
      </div>
      <PageAccessPanel />
      <UsersPanel />
    </div>
  )
}

// Page-access matrix: pages × grantable roles. Admin column is implicit and locked on.
function PageAccessPanel() {
  const { data, loading, error, reload } = useAsync(() => api.pageAccess(), [])
  const [edits, setEdits] = useState<Record<string, Set<UserRole>>>({})
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (data) {
      const next: Record<string, Set<UserRole>> = {}
      for (const p of data) next[p.key] = new Set(p.allowedRoles as UserRole[])
      setEdits(next)
    }
  }, [data])

  const dirty = useMemo(() => {
    if (!data) return false
    return data.some((p) => {
      const cur = edits[p.key]
      if (!cur) return false
      const orig = new Set(p.allowedRoles)
      if (cur.size !== orig.size) return true
      for (const r of cur) if (!orig.has(r)) return true
      return false
    })
  }, [data, edits])

  const toggle = (key: string, role: UserRole, on: boolean) => {
    setSaved(false)
    setEdits((e) => {
      const set = new Set(e[key] ?? [])
      if (on) set.add(role)
      else set.delete(role)
      set.add('Admin')
      return { ...e, [key]: set }
    })
  }

  const save = async () => {
    setSaving(true)
    try {
      const updates = Object.entries(edits).map(([key, set]) => ({ key, allowedRoles: Array.from(set) }))
      await api.savePageAccess(updates)
      setSaved(true)
      reload()
    } finally {
      setSaving(false)
    }
  }

  return (
    <Panel
      title="Page access"
      action={
        <Button appearance="primary" icon={<SaveRegular />} disabled={!dirty || saving} onClick={save}>
          {saving ? 'Saving…' : saved && !dirty ? 'Saved' : 'Save access'}
        </Button>
      }
    >
      {loading ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            Tick a role to let it open a page. Administrators always have full access, so the Admin column is locked on.
          </Text>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: 420 }}>
              <thead>
                <tr>
                  <th style={{ textAlign: 'left', padding: '8px 12px', borderBottom: '2px solid var(--colorNeutralStroke2)' }}>Page</th>
                  {ROLES.map((r) => (
                    <th key={r} style={{ textAlign: 'center', padding: '8px 12px', borderBottom: '2px solid var(--colorNeutralStroke2)', width: 90 }}>{ROLE_LABEL[r]}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {(data ?? []).map((p: PageAccess) => (
                  <tr key={p.key}>
                    <td style={{ padding: '6px 12px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>
                      <Text weight="semibold">{p.label}</Text>
                    </td>
                    {ROLES.map((r) => {
                      const checked = r === 'Admin' ? true : (edits[p.key]?.has(r) ?? p.allowedRoles.includes(r))
                      const locked = r === 'Admin' || !GRANTABLE.includes(r)
                      return (
                        <td key={r} style={{ textAlign: 'center', padding: '6px 12px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>
                          <Checkbox checked={checked} disabled={locked} onChange={(_, d) => toggle(p.key, r, !!d.checked)} />
                        </td>
                      )
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </Panel>
  )
}

// Admin-only: create/manage portal logins (custom auth).
function UsersPanel() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useAsync(() => api.users(), [])
  const [form, setForm] = useState<{ username: string; displayName: string; role: UserRole; password: string }>({ username: '', displayName: '', role: 'Sa', password: '' })
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [provisioning, setProvisioning] = useState(false)

  const provisionSa = async () => {
    if (!confirm('Create an SA login for every Solution Architect on nominations who does not already have one?')) return
    setProvisioning(true); setMsg(null)
    try {
      const r = await api.provisionSaLogins()
      setMsg(`Provisioned ${r.created} SA login${r.created === 1 ? '' : 's'} (skipped ${r.skipped} existing). Temp password: ${r.tempPassword} — each must change it at first login.`)
      reload()
    } catch {
      setMsg('Could not provision SA logins.')
    } finally {
      setProvisioning(false)
    }
  }

  if (user?.role !== 'Admin') return null

  const add = async () => {
    setBusy(true); setMsg(null)
    try {
      await api.createUser({ username: form.username.trim(), displayName: form.displayName.trim() || form.username.trim(), role: form.role, active: true, password: form.password })
      setForm({ username: '', displayName: '', role: 'Sa', password: '' })
      reload()
    } catch {
      setMsg('Could not create user (username may be taken).')
    } finally {
      setBusy(false)
    }
  }
  const toggleActive = async (u: AppUser) => {
    await api.updateUser(u.id, { username: u.username, displayName: u.displayName, role: u.role, active: !u.active })
    reload()
  }
  const setRole = async (u: AppUser, role: UserRole) => {
    await api.updateUser(u.id, { username: u.username, displayName: u.displayName, role, active: u.active })
    reload()
  }
  const reset = async (u: AppUser) => {
    const p = window.prompt(`New temporary password for ${u.username} (they must change it at next login):`)
    if (p) { await api.resetUserPassword(u.id, p); reload() }
  }

  return (
    <Panel
      title="Users"
      action={
        <Button appearance="secondary" icon={<PeopleTeamRegular />} disabled={provisioning} onClick={provisionSa}>
          {provisioning ? 'Provisioning…' : 'Provision SA logins'}
        </Button>
      }
    >
      {loading ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : (
        <>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
            Custom portal logins. New users get a temporary password and must change it at first login.
            <b> Provision SA logins</b> bulk-creates an SA account for every Solution Architect already on nominations.
          </Text>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 10, marginBottom: 14 }}>
            {data?.map((u) => (
              <div key={u.id} style={{ border: '1px solid var(--colorNeutralStroke2)', borderRadius: 8, padding: 12, display: 'flex', flexDirection: 'column', gap: 8 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div>
                    <Text weight="semibold">{u.displayName}</Text>
                    <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>{u.username}{u.mustChangePassword ? ' · must reset' : ''}</Text>
                  </div>
                  <Badge appearance="tint" color={u.active ? 'success' : 'danger'}>{u.active ? 'active' : 'disabled'}</Badge>
                </div>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Dropdown size="small" value={ROLE_LABEL[u.role]} selectedOptions={[u.role]} onOptionSelect={(_, d) => setRole(u, (d.optionValue as UserRole) ?? u.role)} style={{ minWidth: 90 }}>
                    {ROLES.map((r) => <Option key={r} value={r}>{ROLE_LABEL[r]}</Option>)}
                  </Dropdown>
                  <Button size="small" appearance="secondary" onClick={() => toggleActive(u)} disabled={u.id === user.id}>{u.active ? 'Disable' : 'Enable'}</Button>
                  <Button size="small" appearance="subtle" onClick={() => reset(u)}>Reset password</Button>
                </div>
              </div>
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', flexWrap: 'wrap', borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 12 }}>
            <Field label="Username"><Input size="small" value={form.username} onChange={(_, d) => setForm((f) => ({ ...f, username: d.value }))} /></Field>
            <Field label="Display name"><Input size="small" value={form.displayName} onChange={(_, d) => setForm((f) => ({ ...f, displayName: d.value }))} /></Field>
            <Field label="Role">
              <Dropdown size="small" value={ROLE_LABEL[form.role]} selectedOptions={[form.role]} onOptionSelect={(_, d) => setForm((f) => ({ ...f, role: (d.optionValue as UserRole) ?? 'Sa' }))} style={{ minWidth: 90 }}>
                {ROLES.map((r) => <Option key={r} value={r}>{ROLE_LABEL[r]}</Option>)}
              </Dropdown>
            </Field>
            <Field label="Temp password"><Input size="small" type="password" value={form.password} onChange={(_, d) => setForm((f) => ({ ...f, password: d.value }))} /></Field>
            <Button appearance="primary" icon={<AddRegular />} disabled={busy || !form.username || form.password.length < 6} onClick={add}>Add user</Button>
          </div>
          {msg && <Text size={200} style={{ color: 'var(--colorPaletteRedForeground1)', display: 'block', marginTop: 8 }}>{msg}</Text>}
        </>
      )}
    </Panel>
  )
}
