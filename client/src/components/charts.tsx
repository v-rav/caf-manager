import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js'
import { Bar, Doughnut } from 'react-chartjs-2'
import type { NameValue, TimeBucket } from '../types'

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, Tooltip, Legend)

const PALETTE = ['#0f6cbd', '#107c10', '#f7630c', '#c50f1f', '#8764b8', '#00b7c3', '#ca5010', '#498205']

// Truncate long category labels; the full name stays available via the tooltip.
const short = (s: string) => (s.length > 16 ? `${s.slice(0, 15)}\u2026` : s)

function EmptyChart() {
  return (
    <div style={{ height: 200, display: 'grid', placeItems: 'center', color: 'var(--colorNeutralForeground3)', fontSize: 13 }}>
      No data for this selection.
    </div>
  )
}

export function DoughnutChart({ data, colors }: { data: NameValue[]; colors?: string[] }) {
  if (!data.length || data.every((d) => !d.value)) return <EmptyChart />
  return (
    <div style={{ maxHeight: 260 }}>
      <Doughnut
        data={{
          labels: data.map((d) => d.name),
          datasets: [{ data: data.map((d) => d.value), backgroundColor: colors ?? PALETTE }],
        }}
        options={{ responsive: true, plugins: { legend: { position: 'bottom' } } }}
      />
    </div>
  )
}

export function BarChart({ data, label }: { data: NameValue[]; label: string }) {
  if (!data.length || data.every((d) => !d.value)) return <EmptyChart />
  return (
    <div style={{ maxHeight: 280 }}>
      <Bar
        data={{
          labels: data.map((d) => short(d.name)),
          datasets: [{ label, data: data.map((d) => d.value), backgroundColor: '#0f6cbd' }],
        }}
        options={{
          responsive: true,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { title: (items) => data[items[0]?.dataIndex ?? 0]?.name ?? '' } },
          },
          scales: { x: { ticks: { maxRotation: 0 } }, y: { beginAtZero: true, ticks: { precision: 0 } } },
        }}
      />
    </div>
  )
}

// Stacked bar time series: one dataset per series (single dataset when SplitBy=none).
export function TimeSeriesChart({ buckets, series }: { buckets: TimeBucket[]; series: string[] }) {
  if (!buckets.length) return <EmptyChart />
  const datasets = series.map((s, i) => ({
    label: s,
    data: buckets.map((b) => b.values.find((v) => v.name === s)?.value ?? 0),
    backgroundColor: PALETTE[i % PALETTE.length],
  }))
  return (
    <div style={{ maxHeight: 340 }}>
      <Bar
        data={{ labels: buckets.map((b) => b.label), datasets }}
        options={{
          responsive: true,
          plugins: { legend: { display: series.length > 1, position: 'bottom' } },
          scales: { x: { stacked: true, ticks: { maxRotation: 0 } }, y: { stacked: true, beginAtZero: true, ticks: { precision: 0 } } },
        }}
      />
    </div>
  )
}
