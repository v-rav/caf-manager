import { Badge, Button, Dropdown, Option, SearchBox, Spinner, Text, Tooltip, tokens } from '@fluentui/react-components'
import { ArrowClockwiseRegular } from '@fluentui/react-icons'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import type { Nomination } from '../types'

/** Coloured badge mapping a capacity/risk status string to an intent. */
export function StatusBadge({ status }: { status: string }) {
  const map: Record<string, 'success' | 'warning' | 'danger' | 'informative'> = {
    Available: 'success',
    Green: 'success',
    'Partially Utilized': 'warning',
    'Fully Utilized': 'warning',
    Amber: 'warning',
    Overloaded: 'danger',
    Red: 'danger',
    Open: 'informative',
    'In Progress': 'warning',
    Closed: 'success',
  }
  return (
    <Badge appearance="filled" color={map[status] ?? 'informative'} style={{ whiteSpace: 'nowrap' }}>
      {status}
    </Badge>
  )
}

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return <Spinner label={label} style={{ padding: 40 }} />
}

/** Account name linking to the nomination's SA Workspace. */
export function NominationLink({ id, name }: { id: number; name?: string | null }) {
  return <RouterLink to={`/nominations/${id}`}>{name || `Nomination ${id}`}</RouterLink>
}

/** Standard page title row: title + one-line description on the left, page actions on the right. */
export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: ReactNode; actions?: ReactNode }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
      <div>
        <Text as="h1" size={600} weight="bold" style={{ display: 'block', margin: 0 }}>{title}</Text>
        {subtitle && <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>{subtitle}</Text>}
      </div>
      {actions && <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', flexWrap: 'wrap' }}>{actions}</div>}
    </div>
  )
}

export function ErrorText({ error, onRetry }: { error: string; onRetry?: () => void }) {
  return (
    <div style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 10, alignItems: 'flex-start' }}>
      <Text style={{ color: 'var(--colorPaletteRedForeground1)' }}>Failed to load data: {error}</Text>
      {onRetry && (
        <Button size="small" appearance="secondary" icon={<ArrowClockwiseRegular />} onClick={onRetry}>
          Retry
        </Button>
      )}
    </div>
  )
}

/** Compact horizontal utilization meter, colour-coded by load. */
export function UtilizationBar({ percent }: { percent: number }) {
  const pct = Math.max(0, Math.min(100, percent))
  const color =
    pct >= 100
      ? 'var(--colorPaletteRedForeground1)'
      : pct >= 80
        ? 'var(--colorPaletteDarkOrangeForeground1)'
        : pct > 0
          ? 'var(--colorPaletteGreenForeground1)'
          : 'var(--colorNeutralForeground4)'
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 120 }}>
      <div style={{ flex: 1, height: 6, borderRadius: 999, background: 'var(--colorNeutralBackground4)', overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: color, borderRadius: 999 }} />
      </div>
      <Text size={200} style={{ width: 38, textAlign: 'right', color: 'var(--colorNeutralForeground2)' }}>
        {percent}%
      </Text>
    </div>
  )
}

export function Panel({ title, children, action }: { title: string; children: ReactNode; action?: ReactNode }) {
  return (
    <section
      style={{
        background: 'var(--colorNeutralBackground1)',
        border: '1px solid var(--colorNeutralStroke2)',
        borderRadius: 8,
        padding: 16,
        boxShadow: 'var(--shadow2)',
        minWidth: 0,
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: 12,
          flexWrap: 'wrap',
          marginBottom: 12,
        }}
      >
        <Text weight="semibold" size={400}>
          {title}
        </Text>
        {action}
      </div>
      <div style={{ overflowX: 'auto' }}>{children}</div>
    </section>
  )
}

/** Compact labelled dropdown for table filters. Empty value = "All". */
export function FilterSelect({
  label,
  value,
  options,
  onChange,
  minWidth = 150,
  allLabel,
}: {
  label: string
  value: string
  options: string[]
  onChange: (value: string) => void
  minWidth?: number
  allLabel?: string
}) {
  const all = allLabel ?? 'All'
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Text size={200} weight="semibold" style={{ color: tokens.colorNeutralForeground3 }}>
        {label}
      </Text>
      <Dropdown
        aria-label={label}
        placeholder={all}
        value={value || all}
        selectedOptions={[value]}
        onOptionSelect={(_, d) => onChange(d.optionValue ?? '')}
        style={{ minWidth }}
      >
        <Option value="">{all}</Option>
        {options.map((o) => (
          <Option key={o} value={o}>
            {o}
          </Option>
        ))}
      </Dropdown>
    </div>
  )
}

/** Labelled free-text filter (contains match) — same layout as FilterSelect, for long lists like people. */
export function FilterText({ label, value, onChange, placeholder, minWidth = 150 }: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
  minWidth?: number
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Text size={200} weight="semibold" style={{ color: tokens.colorNeutralForeground3 }}>
        {label}
      </Text>
      <SearchBox aria-label={label} placeholder={placeholder ?? 'Any'} value={value} onChange={(_, d) => onChange(d.value)} style={{ minWidth }} />
    </div>
  )
}

/** Multi-select variant of FilterSelect: pick any number of options; empty = all. */
export function MultiFilterSelect({
  label,
  values,
  options,
  onChange,
  minWidth = 150,
  allLabel,
}: {
  label: string
  values: string[]
  options: string[]
  onChange: (values: string[]) => void
  minWidth?: number
  allLabel?: string
}) {
  const all = allLabel ?? 'All'
  const display = values.length === 0 ? all : values.length === 1 ? values[0] : `${values.length} selected`
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Text size={200} weight="semibold" style={{ color: tokens.colorNeutralForeground3 }}>
        {label}
      </Text>
      <Dropdown
        aria-label={label}
        multiselect
        placeholder={all}
        value={display}
        selectedOptions={values}
        onOptionSelect={(_, d) => onChange(d.selectedOptions)}
        style={{ minWidth }}
      >
        {options.map((o) => (
          <Option key={o} value={o}>
            {o}
          </Option>
        ))}
      </Dropdown>
    </div>
  )
}

/** SA workload over in-flight nominations: counts started waves (server `isUpcoming` = start after today + look-ahead
 *  is excluded) and shows not-yet-started waves separately as "+N upcoming" with the next start date. */
export function SaWorkloadList({ rows, top = 8 }: { rows: Nomination[]; top?: number }) {
  const m = new Map<string, { active: number; upcoming: number; next?: string }>()
  for (const n of rows) {
    const sa = (n.solutionArchitect ?? '').trim()
    if (!sa) continue
    const e = m.get(sa) ?? { active: 0, upcoming: 0 }
    if (n.isUpcoming) {
      e.upcoming++
      if (n.effectiveStartDate && (!e.next || n.effectiveStartDate < e.next)) e.next = n.effectiveStartDate
    } else e.active++
    m.set(sa, e)
  }
  const list = [...m.entries()].sort((a, b) => b[1].active - a[1].active || b[1].upcoming - a[1].upcoming).slice(0, top)
  if (!list.length) return <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>No assigned SAs.</Text>
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      {list.map(([sa, e]) => (
        <div key={sa} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8, fontSize: 13, padding: '3px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
          <span style={{ flex: 1 }}>{sa}</span>
          {e.upcoming > 0 && (
            <Tooltip content={`${e.upcoming} wave(s) not started yet (future start date) — next starts ${e.next ?? '—'}`} relationship="description">
              <Badge appearance="tint" color="informative" size="small">+{e.upcoming} upcoming</Badge>
            </Tooltip>
          )}
          <span style={{ color: tokens.colorNeutralForeground3, minWidth: 20, textAlign: 'right' }}>{e.active}</span>
        </div>
      ))}
      <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>Counts started waves (actual start, else planned). Future-dated waves show as upcoming.</Text>
    </div>
  )
}
