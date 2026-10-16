import { Spinner } from '@fluentui/react-components'
import { lazy, Suspense, type ComponentProps, type ReactNode } from 'react'
import type * as Charts from './charts'

// Chart.js is the heaviest library after Fluent; these wrappers download it only when a chart actually renders.
const load = () => import('./charts')
const LazyDoughnut = lazy(() => load().then((m) => ({ default: m.DoughnutChart })))
const LazyBar = lazy(() => load().then((m) => ({ default: m.BarChart })))
const LazyAttainment = lazy(() => load().then((m) => ({ default: m.AttainmentChart })))
const LazyTimeSeries = lazy(() => load().then((m) => ({ default: m.TimeSeriesChart })))

const Wait = ({ children }: { children: ReactNode }) => (
  <Suspense fallback={<Spinner size="small" style={{ padding: 24 }} />}>{children}</Suspense>
)

export const DoughnutChart = (p: ComponentProps<typeof Charts.DoughnutChart>) => <Wait><LazyDoughnut {...p} /></Wait>
export const BarChart = (p: ComponentProps<typeof Charts.BarChart>) => <Wait><LazyBar {...p} /></Wait>
export const AttainmentChart = (p: ComponentProps<typeof Charts.AttainmentChart>) => <Wait><LazyAttainment {...p} /></Wait>
export const TimeSeriesChart = (p: ComponentProps<typeof Charts.TimeSeriesChart>) => <Wait><LazyTimeSeries {...p} /></Wait>
