import { Badge, Button, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import type { ReactNode } from 'react'
import { downloadCsv, type CsvColumn } from '../export'

// Shared presentation building blocks. Colours always come from the server (`slaTone`, `healthTone`, `velocityTone`).

type Tone = 'success' | 'warning' | 'severe' | 'danger' | 'informative' | 'brand' | 'subtle'

/** SLA tier badge (Warn / Escalate / Defer); renders nothing when the wave isn't tiered. */
export function SlaBadge({ tier, tone }: { tier?: string | null; tone?: Tone | null }) {
  if (!tier) return null
  return <Badge appearance="tint" color={tone ?? 'informative'} size="small">SLA {tier}</Badge>
}

/** Status (health) badge — On Track / Waiting / Blocked — with the raw FDO Current State in the tooltip. */
export function HealthBadge({ bucket, tone, currentState }: { bucket?: string | null; tone?: Tone | null; currentState?: string | null }) {
  if (!bucket) return <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
  const badge = <Badge appearance="tint" color={tone ?? 'informative'} size="small">{bucket}</Badge>
  return currentState ? <Tooltip relationship="description" content={`Current State (FDO): ${currentState}`}>{badge}</Tooltip> : badge
}

/** Velocity % badge (100% = on target, higher = faster). */
export function VelocityBadge({ pct, tone }: { pct?: number | null; tone?: Tone | null }) {
  if (pct == null) return <>—</>
  return <Badge appearance="tint" color={tone ?? 'informative'}>{pct}%</Badge>
}

/** "⚠ fix in FDO" marker for server data-quality flags; the flags are listed in the tooltip. */
export function FixInFdoBadge({ flags }: { flags?: string[] | null }) {
  if (!flags?.length) return null
  return (
    <Tooltip relationship="description" content={`Fix in FDO: ${flags.join(' · ')}`}>
      <Badge appearance="tint" color="warning" size="small">⚠ fix in FDO</Badge>
    </Tooltip>
  )
}

/** One labelled horizontal bar; `note` overrides the count shown on the right (e.g. "91 · 63%"). */
export function BarRow({ label, count, max, note, labelWidth = 210 }: { label: ReactNode; count: number; max: number; note?: string; labelWidth?: number }) {
  const pct = max ? Math.round((count / max) * 100) : 0
  return (
    <div style={{ display: 'grid', gridTemplateColumns: `${labelWidth}px 1fr auto`, gap: 10, alignItems: 'center' }}>
      <span style={{ fontSize: 12 }}>{label}</span>
      <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 4, height: 14, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: 'var(--colorBrandBackground)' }} />
      </div>
      <span style={{ fontSize: 12, textAlign: 'right', color: 'var(--colorNeutralForeground3)', minWidth: 40 }}>{note ?? count}</span>
    </div>
  )
}

/** Responsive row of KPI cards. */
export function KpiRow({ children }: { children: ReactNode }) {
  return <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>{children}</div>
}

/** Filter row: the page's FilterSelects, then Reset (only while a filter is set) and an optional status note. */
export function FilterBar({ children, active, onReset, note }: { children: ReactNode; active?: boolean; onReset?: () => void; note?: ReactNode }) {
  return (
    <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
      {children}
      {active && onReset && <Button size="small" onClick={onReset}>Reset</Button>}
      {note}
    </div>
  )
}

/** "Export CSV" button: downloads `rows` with a date-stamped file name. */
export function ExportCsvButton<T>({ name, columns, rows, primary }: { name: string; columns: CsvColumn<T>[]; rows: T[]; primary?: boolean }) {
  return (
    <Button
      appearance={primary ? 'primary' : 'secondary'}
      icon={<ArrowDownloadRegular />}
      disabled={!rows.length}
      onClick={() => downloadCsv<T>(`${name}-${new Date().toISOString().slice(0, 10)}`, columns, rows)}
    >
      Export CSV
    </Button>
  )
}
