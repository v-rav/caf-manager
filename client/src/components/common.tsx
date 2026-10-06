import { Badge, Button, Dropdown, Option, Spinner, Text, tokens } from '@fluentui/react-components'
import { ArrowClockwiseRegular } from '@fluentui/react-icons'
import type { ReactNode } from 'react'

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
    <Badge appearance="filled" color={map[status] ?? 'informative'}>
      {status}
    </Badge>
  )
}

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return <Spinner label={label} style={{ padding: 40 }} />
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
