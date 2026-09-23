import { Badge, Button, Dropdown, Option, Spinner, Text } from '@fluentui/react-components'
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
}: {
  label: string
  value: string
  options: string[]
  onChange: (value: string) => void
  minWidth?: number
}) {
  return (
    <Dropdown
      aria-label={label}
      placeholder={`All ${label}`}
      value={value || `All ${label}`}
      selectedOptions={[value]}
      onOptionSelect={(_, d) => onChange(d.optionValue ?? '')}
      style={{ minWidth }}
    >
      <Option value="">{`All ${label}`}</Option>
      {options.map((o) => (
        <Option key={o} value={o}>
          {o}
        </Option>
      ))}
    </Dropdown>
  )
}
