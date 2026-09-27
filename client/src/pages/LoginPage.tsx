import { Button, Field, Input, Spinner, Text } from '@fluentui/react-components'
import { useState } from 'react'
import { api } from '../api'
import { useAuth } from '../auth'

function Shell({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', background: 'var(--colorNeutralBackground2)' }}>
      <div style={{ width: 360, background: 'var(--colorNeutralBackground1)', borderRadius: 12, padding: 28, boxShadow: '0 8px 30px rgba(0,0,0,.12)' }}>
        <Text size={600} weight="bold" style={{ display: 'block' }}>CAF Operations Portal</Text>
        <Text size={300} weight="semibold" style={{ display: 'block', marginTop: 12 }}>{title}</Text>
        <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)', marginBottom: 16 }}>{subtitle}</Text>
        {children}
      </div>
    </div>
  )
}

export function LoginPage() {
  const { login } = useAuth()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(username.trim(), password)
    } catch {
      setError('Invalid username or password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Shell title="Sign in" subtitle="Use your portal login.">
      <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <Field label="Username">
          <Input value={username} onChange={(_, d) => setUsername(d.value)} autoFocus />
        </Field>
        <Field label="Password" validationState={error ? 'error' : 'none'} validationMessage={error ?? undefined}>
          <Input type="password" value={password} onChange={(_, d) => setPassword(d.value)} />
        </Field>
        <Button type="submit" appearance="primary" disabled={busy || !username || !password}>
          {busy ? <Spinner size="tiny" /> : 'Sign in'}
        </Button>
      </form>
    </Shell>
  )
}

// Forced password change on first login (MustChangePassword).
export function ForcedPasswordChange() {
  const { refresh, logout } = useAuth()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (next !== confirm) { setError('New passwords do not match.'); return }
    if (next.length < 6) { setError('Use at least 6 characters.'); return }
    setBusy(true)
    setError(null)
    try {
      await api.changePassword(current, next)
      await refresh()
    } catch {
      setError('Current password is incorrect.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Shell title="Set a new password" subtitle="Your password must be changed before continuing.">
      <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <Field label="Current password">
          <Input type="password" value={current} onChange={(_, d) => setCurrent(d.value)} autoFocus />
        </Field>
        <Field label="New password">
          <Input type="password" value={next} onChange={(_, d) => setNext(d.value)} />
        </Field>
        <Field label="Confirm new password" validationState={error ? 'error' : 'none'} validationMessage={error ?? undefined}>
          <Input type="password" value={confirm} onChange={(_, d) => setConfirm(d.value)} />
        </Field>
        <Button type="submit" appearance="primary" disabled={busy || !current || !next}>
          {busy ? <Spinner size="tiny" /> : 'Update password'}
        </Button>
        <Button appearance="subtle" onClick={() => void logout()}>Sign out</Button>
      </form>
    </Shell>
  )
}
