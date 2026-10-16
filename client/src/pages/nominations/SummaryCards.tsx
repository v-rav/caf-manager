import { Text } from '@fluentui/react-components'

export type StatTone = 'neutral' | 'success' | 'warning' | 'danger' | 'brand'
const STAT_TONE: Record<StatTone, string> = {
  neutral: 'var(--colorNeutralForeground1)',
  success: 'var(--colorPaletteGreenForeground1)',
  warning: 'var(--colorPaletteDarkOrangeForeground1)',
  danger: 'var(--colorPaletteRedForeground1)',
  brand: 'var(--colorBrandForeground1)',
}

export interface Stat {
  label: string
  value: number
  tone?: StatTone
  active?: boolean
  onClick?: () => void
}

// One metric inside a SummaryCard; clickable stats act as drill-through filters.
function StatCell({ label, value, tone = 'neutral', active, onClick }: Stat) {
  return (
    <div
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      aria-pressed={onClick ? !!active : undefined}
      onKeyDown={onClick ? (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onClick() } } : undefined}
      title={onClick ? `Filter by ${label}` : undefined}
      style={{
        border: '1px solid',
        borderColor: active ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)',
        background: active ? 'var(--colorBrandBackground2)' : 'transparent',
        borderRadius: 6,
        padding: '6px 10px',
        cursor: onClick ? 'pointer' : 'default',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
      }}
    >
      <span style={{ fontSize: 18, fontWeight: 700, lineHeight: 1.1, color: STAT_TONE[tone] }}>{value}</span>
      <span style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)' }}>{label}</span>
    </div>
  )
}

// A titled summary card holding a small grid of related stats.
export function SummaryCard({ title, items, columns = 2 }: { title: string; items: Stat[]; columns?: number }) {
  return (
    <div
      style={{
        flex: '1 1 240px',
        minWidth: 220,
        background: 'var(--colorNeutralBackground1)',
        border: '1px solid var(--colorNeutralStroke2)',
        borderRadius: 8,
        padding: 12,
        boxShadow: 'var(--shadow2)',
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
      }}
    >
      <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground3)', textTransform: 'uppercase', letterSpacing: '.4px' }}>
        {title}
      </Text>
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${columns}, 1fr)`, gap: 6 }}>
        {items.map((it) => (
          <StatCell key={it.label} {...it} />
        ))}
      </div>
    </div>
  )
}
