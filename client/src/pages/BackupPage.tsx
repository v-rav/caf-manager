import { Badge, Button, Text } from '@fluentui/react-components'
import { ArrowDownloadRegular, ArrowUploadRegular, DatabaseRegular } from '@fluentui/react-icons'
import { useRef, useState } from 'react'
import { api } from '../api'
import { ConfirmDialog } from '../components/Modal'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { RestoreResult } from '../types'

function formatBytes(bytes: number): string {
  if (!bytes) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / Math.pow(1024, i)).toFixed(i === 0 ? 0 : 1)} ${units[i]}`
}

export function BackupPage() {
  const { data: status, loading, reload } = useAsync(() => api.adminStatus(), [])
  const fileInput = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [restoring, setRestoring] = useState(false)
  const [result, setResult] = useState<RestoreResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const pickFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0] ?? null
    setFile(f)
    setResult(null)
    setError(null)
  }

  const runRestore = async () => {
    if (!file) return
    setConfirmOpen(false)
    setRestoring(true)
    setError(null)
    setResult(null)
    try {
      const res = await api.restoreDatabase(file)
      setResult(res)
      setFile(null)
      if (fileInput.current) fileInput.current.value = ''
      reload()
    } catch (err) {
      const msg =
        (err as { response?: { data?: { message?: string; title?: string } } }).response?.data?.message ??
        (err as { response?: { data?: { message?: string; title?: string } } }).response?.data?.title ??
        (err as Error).message
      setError(msg || 'Restore failed.')
    } finally {
      setRestoring(false)
    }
  }

  if (loading) return <Loading />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text weight="bold" size={600} style={{ display: 'block' }}>
          Backup &amp; Restore
        </Text>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Download a compressed snapshot of the entire database, or restore the portal from a previous backup.
        </Text>
      </div>

      {status && (
        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
          <Stat label="Resources" value={status.resources} />
          <Stat label="Accounts" value={status.accounts} />
          <Stat label="Nominations" value={status.nominations} />
          <Stat label="Leave" value={status.leaveRecords} />
          <Stat label="Reviews" value={status.performanceReviews} />
        </div>
      )}

      <Panel title="Download backup">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, alignItems: 'flex-start' }}>
          <Text size={300} style={{ color: 'var(--colorNeutralForeground2)' }}>
            Creates a consistent snapshot of the live SQLite database and downloads it as a compressed{' '}
            <code>.zip</code>. Safe to run anytime — it does not lock or change the portal.
          </Text>
          <Button as="a" href={api.backupUrl()} appearance="primary" icon={<ArrowDownloadRegular />}>
            Download backup (.zip)
          </Button>
        </div>
      </Panel>

      <Panel title="Restore from backup">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, alignItems: 'flex-start' }}>
          <div
            style={{
              display: 'flex',
              gap: 8,
              alignItems: 'center',
              padding: '10px 14px',
              borderRadius: 6,
              background: 'var(--colorPaletteRedBackground1)',
              border: '1px solid var(--colorPaletteRedBorder1)',
            }}
          >
            <DatabaseRegular style={{ fontSize: 18, color: 'var(--colorPaletteRedForeground1)' }} />
            <Text size={300} style={{ color: 'var(--colorPaletteRedForeground1)' }}>
              Destructive action — this replaces <strong>all</strong> current portal data with the contents of the
              backup. A safety copy of the current database is kept on the server before the swap.
            </Text>
          </div>

          <input
            ref={fileInput}
            type="file"
            accept=".zip"
            onChange={pickFile}
            style={{ display: 'none' }}
          />
          <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
            <Button appearance="secondary" icon={<ArrowUploadRegular />} onClick={() => fileInput.current?.click()}>
              Choose backup (.zip)
            </Button>
            {file && (
              <Text size={300} style={{ color: 'var(--colorNeutralForeground2)' }}>
                {file.name} · {formatBytes(file.size)}
              </Text>
            )}
            <Button
              appearance="primary"
              disabled={!file || restoring}
              onClick={() => setConfirmOpen(true)}
            >
              {restoring ? 'Restoring…' : 'Restore database'}
            </Button>
          </div>

          {result && (
            <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
              <Badge appearance="filled" color="success">
                Restored
              </Badge>
              <Text size={300} style={{ color: 'var(--colorNeutralForeground2)' }}>
                {result.message} Now holding {result.nominations} nominations, {result.resources} resources,{' '}
                {result.accounts} accounts ({formatBytes(result.restoredBytes)}).
              </Text>
            </div>
          )}
          {error && <ErrorText error={error} />}
        </div>
      </Panel>

      <ConfirmDialog
        open={confirmOpen}
        title="Restore database?"
        message={`This will replace ALL current portal data with the contents of "${file?.name ?? ''}". This cannot be undone from the UI. Continue?`}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={runRestore}
        busy={restoring}
      />
    </div>
  )
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div style={{ minWidth: 96, padding: '8px 14px', border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6 }}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block' }}>
        {label}
      </Text>
      <Text size={600} weight="bold">
        {value}
      </Text>
    </div>
  )
}
