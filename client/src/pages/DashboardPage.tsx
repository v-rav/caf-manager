import { Badge, Button, Tab, TabList, Text } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { BarChart, DoughnutChart } from '../components/charts'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { ADOPTION_LEVELS } from '../adoption'
import type { ExecutiveDashboard, Nomination } from '../types'

const CAPACITY_COLORS = ['#107c10', '#eaa300', '#ca5010', '#c50f1f']
const SETTLED = ['Completed', 'Closed', 'Customer Deferred', 'Withdrawn']
const STAGE_LABELS = ['—', 'Validating', 'Prerequisites', 'Finalize Scope', 'Executing Migration']

function stageIdx(n: Nomination): number {
  const t = (n.migrationStatus ?? '').toLowerCase()
  if (t.includes('executing migration')) return 4
  if (t.includes('finalize')) return 3
  if (t.includes('pre-requisite') || t.includes('prerequisite') || t.includes('pre requisite')) return 2
  if (t.includes('validating')) return 1
  return 0
}
function money(n: number): string {
  return n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`
}
function Bar({ label, count, max }: { label: string; count: number; max: number }) {
  const pct = max ? Math.round((count / max) * 100) : 0
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '210px 1fr 40px', gap: 10, alignItems: 'center' }}>
      <span style={{ fontSize: 12 }}>{label}</span>
      <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 4, height: 14, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: 'var(--colorBrandBackground)' }} />
      </div>
      <span style={{ fontSize: 12, textAlign: 'right', color: 'var(--colorNeutralForeground3)' }}>{count}</span>
    </div>
  )
}

type M = {
  all: Nomination[]; approved: Nomination[]; active: Nomination[]; completed: Nomination[]; blocked: Nomination[]
  sum: (xs: Nomination[], f: (n: Nomination) => number | undefined) => number
}

function msiBands(xs: Nomination[]) {
  return {
    g: xs.filter((n) => n.msiBand === 'Green').length,
    a: xs.filter((n) => n.msiBand === 'Amber').length,
    r: xs.filter((n) => n.msiBand === 'Red').length,
    avg: xs.length ? Math.round(xs.reduce((s, n) => s + n.msiScore, 0) / xs.length) : 0,
  }
}

export function DashboardPage() {
  const { region } = useRegion()
  const navigate = useNavigate()
  const [view, setView] = useState('leadership')
  const dash = useAsync(() => api.dashboard(region), [region])
  const noms = useAsync(() => api.nominations(region), [region])
  const blk = useAsync(() => api.openBlockers(region), [region])

  const m: M = useMemo(() => {
    const all = noms.data ?? []
    const approved = all.filter((n) => (n.approvalStatus ?? '') === 'Approved')
    const active = approved.filter((n) => !SETTLED.includes(n.status))
    const completed = approved.filter((n) => n.status === 'Completed')
    const blocked = active.filter((n) => n.openBlockerCount > 0)
    const sum = (xs: Nomination[], f: (n: Nomination) => number | undefined) => xs.reduce((s, n) => s + (f(n) ?? 0), 0)
    return { all, approved, active, completed, blocked, sum }
  }, [noms.data])

  if ((dash.loading && !dash.data) || (noms.loading && !noms.data)) return <Loading label="Loading executive dashboard…" />
  if (dash.error) return <ErrorText error={dash.error} onRetry={dash.reload} />
  if (!dash.data) return null
  const d = dash.data

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <Text size={600} weight="bold">Executive Dashboard</Text>
        <Button as="a" href={api.exportUrl('summary', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>Executive summary</Button>
      </div>

      <TabList selectedValue={view} onTabSelect={(_, dt) => setView(dt.value as string)}>
        <Tab value="leadership">Leadership</Tab>
        <Tab value="operational">Operational</Tab>
        <Tab value="adoption">GHCP Adoption</Tab>
        <Tab value="productivity">Factory Productivity</Tab>
      </TabList>

      {view === 'leadership' && <Leadership d={d} m={m} region={region} navigate={navigate} />}
      {view === 'operational' && <Operational d={d} m={m} blk={blk.data ?? []} navigate={navigate} />}
      {view === 'adoption' && <Adoption m={m} navigate={navigate} />}
      {view === 'productivity' && <Productivity m={m} />}
    </div>
  )
}

function Leadership({ d, m, region, navigate }: { d: ExecutiveDashboard; m: M; region?: string; navigate: (p: string) => void }) {
  const acr = m.sum(m.approved, (n) => n.totalAcr)
  const usedRate = m.active.length ? Math.round((m.active.filter((n) => (n.ghcpAdoptionLevel ?? 0) >= 5).length / m.active.length) * 100) : 0
  const bands = msiBands(m.active)
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Total nominations" value={m.all.length} tone="brand" onClick={() => navigate('/nominations')} />
        <KpiCard label="Active" value={m.active.length} onClick={() => navigate('/nominations')} />
        <KpiCard label="Blocked" value={m.blocked.length} tone={m.blocked.length ? 'warning' : 'neutral'} onClick={() => navigate('/governance')} />
        <KpiCard label="Completed" value={m.completed.length} tone="success" />
        <KpiCard label="ACR influenced" value={money(acr)} tone="brand" onClick={() => navigate('/analytics')} />
        <KpiCard label="GHCP adoption rate" value={`${usedRate}%`} tone={usedRate >= 40 ? 'success' : 'brand'} onClick={() => navigate('/adoption')} />
        <KpiCard label="Avg MSI" value={bands.avg} tone={bands.avg > 80 ? 'success' : bands.avg >= 60 ? 'warning' : 'danger'} />
        <KpiCard label="Strategic accounts" value={d.strategicAccounts} tone="brand" onClick={() => navigate('/strategic')} />
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 16 }}>
        <Panel title="MSI health (active)">
          <div style={{ display: 'flex', gap: 8 }}>
            <Badge appearance="filled" color="success">Green {bands.g}</Badge>
            <Badge appearance="filled" color="warning">Amber {bands.a}</Badge>
            <Badge appearance="filled" color="danger">Red {bands.r}</Badge>
          </div>
        </Panel>
        <Panel title="Regional distribution"><DoughnutChart data={d.regionDistribution} /></Panel>
        <Panel title="Strategic account coverage"><BarChart data={d.strategicAccountCoverage} label="Assigned resources" /></Panel>
      </div>
      <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Button as="a" href={api.exportUrl('analytics', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>Migration analytics export</Button>
      </div>
    </div>
  )
}

function Operational({ d, m, blk, navigate }: { d: ExecutiveDashboard; m: M; blk: unknown[]; navigate: (p: string) => void }) {
  const stages = [1, 2, 3, 4].map((k) => ({ k, label: STAGE_LABELS[k], count: m.active.filter((n) => stageIdx(n) === k).length }))
  // Strategic pilots are excluded from Standard SLA denominators.
  const standard = m.active.filter((n) => !n.isStrategic)
  const sla = (t: string) => standard.filter((n) => n.staleTier === t).length
  const avgAge = m.active.length ? Math.round(m.active.reduce((s, n) => s + n.effectiveAgeDays, 0) / m.active.length) : 0
  const saLoad = useMemo(() => {
    const mp = new Map<string, number>()
    for (const n of m.active) { const sa = (n.solutionArchitect ?? '').trim(); if (sa) mp.set(sa, (mp.get(sa) ?? 0) + 1) }
    return [...mp.entries()].sort((a, b) => b[1] - a[1]).slice(0, 8)
  }, [m.active])
  const stageMax = Math.max(1, ...stages.map((s) => s.count))
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Active" value={m.active.length} />
        <KpiCard label="SLA Warn (Standard)" value={sla('Warn')} tone={sla('Warn') ? 'warning' : 'neutral'} onClick={() => navigate('/nominations?sla=Warn')} />
        <KpiCard label="SLA Escalate" value={sla('Escalate')} tone={sla('Escalate') ? 'warning' : 'neutral'} onClick={() => navigate('/nominations?sla=Escalate')} />
        <KpiCard label="SLA Defer" value={sla('Defer')} tone={sla('Defer') ? 'danger' : 'neutral'} onClick={() => navigate('/nominations?sla=Defer')} />
        <KpiCard label="Open blockers" value={blk.length} tone={blk.length ? 'warning' : 'neutral'} onClick={() => navigate('/governance')} />
        <KpiCard label="Avg effective age" value={`${avgAge}d`} tone={avgAge >= 15 ? 'warning' : 'neutral'} />
        <KpiCard label="Overloaded resources" value={d.overloadedResources} tone={d.overloadedResources ? 'danger' : 'neutral'} onClick={() => navigate('/capacity?status=Overloaded')} />
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 16, alignItems: 'start' }}>
        <Panel title="Active by stage">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {stages.map((s) => <Bar key={s.k} label={`${s.k} · ${s.label}`} count={s.count} max={stageMax} />)}
          </div>
        </Panel>
        <Panel title="Capacity distribution"><DoughnutChart data={d.capacityDistribution} colors={CAPACITY_COLORS} /></Panel>
        <Panel title="SA workload (active)">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {saLoad.map(([sa, n]) => (
              <div key={sa} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '3px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                <span>{sa}</span><span style={{ color: 'var(--colorNeutralForeground3)' }}>{n}</span>
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </div>
  )
}

function Adoption({ m, navigate }: { m: M; navigate: (p: string) => void }) {
  const active = m.active
  const lvl = (n: Nomination) => n.ghcpAdoptionLevel ?? 0
  const licensed = active.filter((n) => lvl(n) >= 4).length
  const awaiting = active.filter((n) => lvl(n) >= 1 && lvl(n) <= 3).length
  const usedPct = active.length ? Math.round((active.filter((n) => lvl(n) >= 5).length / active.length) * 100) : 0
  const withTool = active.filter((n) => n.isToolAttached != null)
  const toolPct = withTool.length ? Math.round((withTool.filter((n) => n.isToolAttached).length / withTool.length) * 100) : 0
  const counts = new Array(8).fill(0)
  for (const n of active) counts[Math.min(7, Math.max(0, lvl(n)))]++
  const max = Math.max(1, ...counts)
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Licensed (L4+)" value={licensed} tone="success" />
        <KpiCard label="Awaiting (L1–3)" value={awaiting} tone={awaiting ? 'warning' : 'neutral'} />
        <KpiCard label="GHCP-used %" value={`${usedPct}%`} tone={usedPct >= 40 ? 'success' : 'brand'} />
        <KpiCard label="Tool-attached %" value={`${toolPct}%`} tone="brand" />
        <KpiCard label="Full adoption page" value="→" onClick={() => navigate('/adoption')} />
      </div>
      <Panel title="Adoption-level distribution (0–7)">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {ADOPTION_LEVELS.map((a) => <Bar key={a.level} label={`${a.level} · ${a.label}`} count={counts[a.level]} max={max} />)}
        </div>
      </Panel>
    </div>
  )
}

function Productivity({ m }: { m: M }) {
  const acrRealized = m.sum(m.completed, (n) => n.totalAcr)
  const cores = m.sum(m.completed, (n) => n.totalCores)
  const withTool = m.approved.filter((n) => n.isToolAttached != null)
  const toolPct = withTool.length ? Math.round((withTool.filter((n) => n.isToolAttached).length / withTool.length) * 100) : 0
  const withAuto = m.approved.filter((n) => n.isAutomationUsed != null)
  const autoPct = withAuto.length ? Math.round((withAuto.filter((n) => n.isAutomationUsed).length / withAuto.length) * 100) : 0
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <KpiCard label="Migrations completed" value={m.completed.length} tone="success" />
        <KpiCard label="ACR realized" value={money(acrRealized)} tone="brand" />
        <KpiCard label="Cores migrated" value={cores.toLocaleString()} tone="brand" />
        <KpiCard label="Tool adoption %" value={`${toolPct}%`} tone="brand" />
        <KpiCard label="Automation used %" value={`${autoPct}%`} tone="brand" />
      </div>
      <Panel title="Value realization">
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Hours saved · defects prevented · CSAT are captured per migration once outcome recording is enabled — until then,
          throughput (completed migrations, ACR realized, cores) and GHCP tool/automation adoption stand in as the productivity signal.
        </Text>
      </Panel>
    </div>
  )
}
