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
import type { NameValue } from '../types'

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, Tooltip, Legend)

const PALETTE = ['#0f6cbd', '#107c10', '#f7630c', '#c50f1f', '#8764b8', '#00b7c3', '#ca5010', '#498205']

export function DoughnutChart({ data }: { data: NameValue[] }) {
  return (
    <div style={{ maxHeight: 260 }}>
      <Doughnut
        data={{
          labels: data.map((d) => d.name),
          datasets: [{ data: data.map((d) => d.value), backgroundColor: PALETTE }],
        }}
        options={{ responsive: true, plugins: { legend: { position: 'bottom' } } }}
      />
    </div>
  )
}

export function BarChart({ data, label }: { data: NameValue[]; label: string }) {
  return (
    <div style={{ maxHeight: 280 }}>
      <Bar
        data={{
          labels: data.map((d) => d.name),
          datasets: [{ label, data: data.map((d) => d.value), backgroundColor: '#0f6cbd' }],
        }}
        options={{
          responsive: true,
          plugins: { legend: { display: false } },
          scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
        }}
      />
    </div>
  )
}
