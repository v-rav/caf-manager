import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js'
import { Bar, Chart, Doughnut } from 'react-chartjs-2'
import type { AttainmentBucket, NameValue, TimeBucket } from '../types'

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, LineController, LineElement, PointElement, Tooltip, Legend)

const PALETTE = ['#0f6cbd', '#107c10', '#f7630c', '#c50f1f', '#8764b8', '#00b7c3', '#ca5010', '#498205']

const moneyShort = (n: number) =>
  n >= 1e9 ? `$${(n / 1e9).toFixed(1)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`

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

// Attainment: cumulative Completed + In-flight ACR (stacked bars) against the Target curve (line).
export function AttainmentChart({ buckets }: { buckets: AttainmentBucket[] }) {
  if (!buckets.length) return <EmptyChart />
  const labels = buckets.map((b) => b.label)
  const data = {
    labels,
    datasets: [
      { type: 'bar' as const, label: 'Completed', data: buckets.map((b) => b.completed), backgroundColor: '#107c10', stack: 'actual' },
      { type: 'bar' as const, label: 'In-flight', data: buckets.map((b) => b.inflight), backgroundColor: '#5b9bd5', stack: 'actual' },
      { type: 'line' as const, label: 'Target', data: buckets.map((b) => b.target), borderColor: '#c50f1f', backgroundColor: '#c50f1f', borderWidth: 2, pointRadius: 0, tension: 0.2 },
    ],
  }
  return (
    <div style={{ maxHeight: 320 }}>
      <Chart
        type="bar"
        data={data as never}
        options={{
          responsive: true,
          plugins: {
            legend: { position: 'bottom' },
            tooltip: { callbacks: { label: (c) => `${c.dataset.label}: ${moneyShort(Number(c.parsed.y))}` } },
          },
          scales: {
            x: { stacked: true, ticks: { maxRotation: 0 } },
            y: { stacked: true, beginAtZero: true, ticks: { callback: (v) => moneyShort(Number(v)) } },
          },
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
