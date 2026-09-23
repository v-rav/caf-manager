import { Text } from '@fluentui/react-components'

interface KpiCardProps {
  label: string
  value: number | string
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'brand'
  blocked?: number
}

const toneColor: Record<NonNullable<KpiCardProps['tone']>, string> = {
  neutral: 'var(--colorNeutralForeground1)',
  success: 'var(--colorPaletteGreenForeground1)',
  warning: 'var(--colorPaletteDarkOrangeForeground1)',
  danger: 'var(--colorPaletteRedForeground1)',
  brand: 'var(--colorBrandForeground1)',
}

export function KpiCard({ label, value, tone = 'neutral', blocked }: KpiCardProps) {
  return (
    <div
      style={{
        position: 'relative',
        background: 'var(--colorNeutralBackground1)',
        border: '1px solid var(--colorNeutralStroke2)',
        borderRadius: 8,
        padding: '16px 18px',
        minWidth: 150,
        flex: '1 1 160px',
        boxShadow: 'var(--shadow2)',
      }}
    >
      {blocked && blocked > 0 ? (
        <span
          title={`${blocked} blocked in this stage`}
          style={{
            position: 'absolute',
            top: 10,
            right: 10,
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
            background: 'var(--colorPaletteRedBackground2)',
            color: 'var(--colorPaletteRedForeground1)',
            borderRadius: 999,
            padding: '2px 8px',
            fontSize: 11,
            fontWeight: 600,
            lineHeight: 1.4,
          }}
        >
          {blocked} blocked
        </span>
      ) : null}
      <Text size={100} style={{ color: 'var(--colorNeutralForeground3)', textTransform: 'uppercase', letterSpacing: 0.5 }}>
        {label}
      </Text>
      <div>
        <Text size={800} weight="bold" style={{ color: toneColor[tone] }}>
          {value}
        </Text>
      </div>
    </div>
  )
}
