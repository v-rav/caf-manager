import { Badge, Button } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { downloadCsv } from '../export'
import type { Blocker, Nomination } from '../types'

function stageIdx(n: Nomination): number {
  const m = (n.migrationStatus ?? '').toLowerCase()
  if (m.includes('executing migration')) return 4
  if (m.includes('finalize')) return 3
  if (m.includes('pre-requisite') || m.includes('prerequisite') || m.includes('pre requisite')) return 2
  if (m.includes('validating')) return 1
  return 0
}
const STAGE_LABELS = ['—', 'Validating', 'Prerequisites', 'Finalize Scope', 'Executing Migration']

function FunnelBar({ label, count, top, prev }: { label: string; count: number; top: number; prev?: number }) {
  const pct = top ? Math.round((count / top) * 100) : 0
  const conv = prev != null && prev > 0 ? Math.round((count / prev) * 100) : null
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '190px 1fr 120px', gap: 10, alignItems: 'center' }}>
      <span style={{ fontSize: 12 }}>{label}</span>
      <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 4, height: 18, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: 'var(--colorBrandBackground)' }} />
      </div>
      <span style={{ fontSize: 12, textAlign: 'right', color: 'var(--colorNeutralForeground3)' }}>
        {count}{conv != null ? ` · ${conv}%` : ''}
      </span>
    </div>
  )
}

export function FlowPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const noms = useAsync(() => api.nominations(region), [region])
  const blk = useAsync(() => api.openBlockers(region), [region])
  const [path, setPath] = useState('')

  const scoped = useMemo(() => (noms.data ?? []).filter((n) => n.status !== 'Withdrawn' && nominationInFy(n, fy)), [noms.data, fy])
  const pathOptions = useMemo(
    () => [...new Set(scoped.map((n) => n.primaryMigrationPath?.trim()).filter((p): p is string => !!p))].sort(),
    [scoped],
  )
  const base = useMemo(() => (path ? scoped.filter((n) => (n.primaryMigrationPath?.trim() || '') === path) : scoped), [scoped, path])
  const approved = useMemo(() => base.filter((n) => (n.approvalStatus ?? '') === 'Approved'), [base])
  const completed = (n: Nomination) => n.status === 'Completed'

  const exportRows = () =>
    downloadCsv<Nomination>(
      `migration-flow-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: 'Account', value: (n) => n.accountName ?? '' },
        { header: 'TPID', value: (n) => n.tpid ?? '' },
        { header: 'Stage', value: (n) => stageIdx(n) || '' },
        { header: 'Migration status', value: (n) => n.migrationStatus ?? '' },
        { header: 'Current state', value: (n) => n.currentState ?? '' },
        { header: 'Migration path', value: (n) => n.primaryMigrationPath ?? '' },
        { header: 'SA', value: (n) => n.solutionArchitect ?? '' },
        { header: 'Age (stage days)', value: (n) => n.stageAgeDays ?? n.daysSinceUpdate },
        { header: 'Status', value: (n) => n.status },
        { header: 'Region', value: (n) => n.region },
      ],
      approved,
    )

  const funnel = useMemo(() => {
    const reached = (k: number) => approved.filter((n) => completed(n) || stageIdx(n) >= k).length
    const steps = [
      { label: 'Received', count: base.length },
      { label: 'Approved', count: approved.length },
      { label: 'Validating (S1+)', count: reached(1) },
      { label: 'Prerequisites (S2+)', count: reached(2) },
      { label: 'Finalize Scope (S3+)', count: reached(3) },
      { label: 'Executing (S4+)', count: reached(4) },
      { label: 'Completed', count: approved.filter(completed).length },
    ]
    const top = steps[0]?.count || 1
    const overall = steps[0].count ? Math.round((steps[steps.length - 1].count / steps[0].count) * 100) : 0
    return { steps, top, overall }
  }, [base, approved])

  const stageBottleneck = useMemo(() => {
    const active = approved.filter((n) => !completed(n) && stageIdx(n) >= 1)
    const rows = [1, 2, 3, 4].map((k) => {
      const inStage = active.filter((n) => stageIdx(n) === k)
      const avgAge = inStage.length ? Math.round(inStage.reduce((s, n) => s + (n.stageAgeDays ?? n.daysSinceUpdate), 0) / inStage.length) : 0
      return { k, label: STAGE_LABELS[k], count: inStage.length, avgAge, pressure: inStage.length * avgAge }
    })
    const worst = rows.reduce((a, b) => (b.pressure > a.pressure ? b : a), rows[0])
    return { rows, worstK: worst?.k }
  }, [approved])

  const byBlockerCategory = useMemo(() => {
    const m = new Map<string, number>()
    for (const b of (blk.data ?? []) as Blocker[]) m.set(b.category, (m.get(b.category) ?? 0) + 1)
    return [...m.entries()].sort((a, b) => b[1] - a[1])
  }, [blk.data])

  const saWorkload = useMemo(() => {
    const m = new Map<string, number>()
    for (const n of approved.filter((n) => !completed(n))) {
      const sa = (n.solutionArchitect ?? '').trim()
      if (sa) m.set(sa, (m.get(sa) ?? 0) + 1)
    }
    return [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, 8)
  }, [approved])

  const byPath = useMemo(() => {
    const m = new Map<string, { count: number; age: number }>()
    for (const n of approved.filter((n) => !completed(n))) {
      const p = n.primaryMigrationPath?.trim() || 'Unspecified'
      const e = m.get(p) ?? { count: 0, age: 0 }
      e.count++
      e.age += n.stageAgeDays ?? n.daysSinceUpdate
      m.set(p, e)
    }
    return [...m.entries()].map(([p, e]) => ({ path: p, count: e.count, avgAge: e.count ? Math.round(e.age / e.count) : 0 }))
      .sort((a, b) => b.count - a.count)
  }, [approved])

  if ((noms.loading && !noms.data) || (blk.loading && !blk.data)) return <Loading label="Loading migration flow…" />
  if (noms.error) return <ErrorText error={noms.error} onRetry={noms.reload} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ fontSize: 22, fontWeight: 700 }}>Migration Flow</div>
          <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>Funnel conversion &amp; drop-off · bottleneck analytics across the migration journey</div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <FilterSelect label="Path" value={path} onChange={setPath} options={pathOptions} minWidth={200} />
          <Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportRows} disabled={!approved.length}>Export</Button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: 12 }}>
        <KpiCard label="Received" value={funnel.steps[0].count} tone="neutral" />
        <KpiCard label="Approved" value={funnel.steps[1].count} tone="brand" />
        <KpiCard label="Completed" value={funnel.steps[6].count} tone="success" />
        <KpiCard label="Overall conversion" value={`${funnel.overall}%`} tone={funnel.overall >= 25 ? 'success' : 'warning'} />
      </div>

      <Panel title="Migration funnel">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 7 }}>
          {funnel.steps.map((s, i) => (
            <FunnelBar key={s.label} label={s.label} count={s.count} top={funnel.top} prev={i ? funnel.steps[i - 1].count : undefined} />
          ))}
        </div>
      </Panel>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 16, alignItems: 'start' }}>
        <Panel title="Where nominations stick (in-flight by stage)">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {stageBottleneck.rows.map((r) => (
              <div key={r.k} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '5px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                <Badge appearance={r.k === stageBottleneck.worstK ? 'filled' : 'tint'} color={r.k === stageBottleneck.worstK ? 'danger' : 'informative'}>{r.k}</Badge>
                <span style={{ flex: 1, fontSize: 13 }}>{r.label}</span>
                <span style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>{r.count} · avg {r.avgAge}d</span>
              </div>
            ))}
          </div>
        </Panel>

        <Panel title="Customer dependencies (open blockers by category)">
          {byBlockerCategory.length === 0 ? (
            <span style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>No open blockers.</span>
          ) : (
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {byBlockerCategory.map(([c, n]) => <Badge key={c} appearance="tint" color="warning">{c} · {n}</Badge>)}
            </div>
          )}
        </Panel>

        <Panel title="SA workload (active nominations)">
          {saWorkload.length === 0 ? (
            <span style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>No assigned SAs.</span>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {saWorkload.map(([sa, n]) => (
                <div key={sa} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '3px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                  <span>{sa}</span><span style={{ color: 'var(--colorNeutralForeground3)' }}>{n}</span>
                </div>
              ))}
            </div>
          )}
        </Panel>

        <Panel title="By migration type (path)">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {byPath.map((p) => (
              <div key={p.path} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '3px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                <span>{p.path}</span><span style={{ color: 'var(--colorNeutralForeground3)' }}>{p.count} · avg {p.avgAge}d</span>
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </div>
  )
}
