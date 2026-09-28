import { Badge, Button, Text } from '@fluentui/react-components'
import { ArrowDownloadRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { ADOPTION_LEVELS } from '../adoption'
import { downloadCsv } from '../export'
import type { Nomination } from '../types'

const SETTLED = ['Withdrawn']

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

export function AdoptionPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const [tool, setTool] = useState('')

  const base = useMemo(() => (data ?? []).filter((n) => nominationInFy(n, fy) && !SETTLED.includes(n.status)), [data, fy])
  const rows = useMemo(
    () => base.filter((n) => (tool === 'Tool attached' ? n.isToolAttached === true : tool === 'No tool' ? n.isToolAttached === false : true)),
    [base, tool],
  )

  const exportRows = () =>
    downloadCsv<Nomination>(
      `ghcp-adoption-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: 'Account', value: (n) => n.accountName ?? '' },
        { header: 'TPID', value: (n) => n.tpid ?? '' },
        { header: 'Region', value: (n) => n.region },
        { header: 'Adoption level', value: (n) => n.ghcpAdoptionLevel ?? 0 },
        { header: 'Tool attached', value: (n) => (n.isToolAttached == null ? '' : n.isToolAttached ? 'Yes' : 'No') },
        { header: 'Automation used', value: (n) => (n.isAutomationUsed == null ? '' : n.isAutomationUsed ? 'Yes' : 'No') },
        { header: 'Status', value: (n) => n.status },
      ],
      rows,
    )

  const kpis = useMemo(() => {
    const total = rows.length
    const lvl = (n: Nomination) => n.ghcpAdoptionLevel ?? 0
    const licensed = rows.filter((n) => lvl(n) >= 4).length
    const awaiting = rows.filter((n) => lvl(n) >= 1 && lvl(n) <= 3).length
    const used = rows.filter((n) => lvl(n) >= 5).length
    const withToolData = rows.filter((n) => n.isToolAttached != null)
    const toolAttached = withToolData.filter((n) => n.isToolAttached).length
    return {
      total,
      licensed,
      awaiting,
      usedPct: total ? Math.round((used / total) * 100) : 0,
      toolPct: withToolData.length ? Math.round((toolAttached / withToolData.length) * 100) : 0,
    }
  }, [rows])

  const dist = useMemo(() => {
    const counts = new Array(8).fill(0)
    for (const n of rows) counts[Math.min(7, Math.max(0, n.ghcpAdoptionLevel ?? 0))]++
    const max = Math.max(1, ...counts)
    return { counts, max }
  }, [rows])

  const byRegion = useMemo(() => {
    const m = new Map<string, { total: number; used: number }>()
    for (const n of rows) {
      const r = n.region || 'Unspecified'
      const e = m.get(r) ?? { total: 0, used: 0 }
      e.total++
      if ((n.ghcpAdoptionLevel ?? 0) >= 5) e.used++
      m.set(r, e)
    }
    return [...m.entries()].sort((a, b) => b[1].total - a[1].total)
  }, [rows])

  if (loading && !data) return <Loading label="Loading adoption…" />
  if (error) return <ErrorText error={error} onRetry={reload} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <div style={{ fontSize: 22, fontWeight: 700 }}>GHCP Adoption</div>
          <div style={{ fontSize: 12, color: 'var(--colorNeutralForeground3)' }}>License readiness · adoption maturity 0–7 · tool usage — across active nominations</div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <FilterSelect
            label="Tooling"
            value={tool}
            onChange={setTool}
            options={['Tool attached', 'No tool']}
          />
          <Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportRows} disabled={!rows.length}>Export</Button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: 12 }}>
        <KpiCard label="Licensed (L4+)" value={kpis.licensed} tone="success" />
        <KpiCard label="Awaiting license (L1–3)" value={kpis.awaiting} tone={kpis.awaiting ? 'warning' : 'neutral'} />
        <KpiCard label="GHCP-used %" value={`${kpis.usedPct}%`} tone={kpis.usedPct >= 50 ? 'success' : 'brand'} />
        <KpiCard label="Tool-attached %" value={`${kpis.toolPct}%`} tone="brand" />
      </div>

      <Panel title="Adoption-level distribution (0–7)">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {ADOPTION_LEVELS.map((a) => (
            <Bar key={a.level} label={`${a.level} · ${a.label}`} count={dist.counts[a.level]} max={dist.max} />
          ))}
        </div>
      </Panel>

      <Panel title="By region">
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {byRegion.map(([r, e]) => (
            <Badge key={r} appearance="tint" color="informative">
              {r} · {e.total} · used {e.total ? Math.round((e.used / e.total) * 100) : 0}%
            </Badge>
          ))}
        </div>
      </Panel>

      <CapabilitySection region={region} />
    </div>
  )
}

// Leadership rollup: which capabilities were accelerated by which tools (distinct nominations).
function CapabilitySection({ region }: { region?: string }) {
  const { data, loading, error, reload } = useAsync(() => api.capability(region), [region])
  if (loading && !data) return <Panel title="Migration Capability Utilization"><Loading /></Panel>
  if (error) return <Panel title="Migration Capability Utilization"><ErrorText error={error} onRetry={reload} /></Panel>
  if (!data) return null

  const exportCsv = () =>
    downloadCsv(
      `capability-utilization-${new Date().toISOString().slice(0, 10)}`,
      [
        { header: 'Cut', value: (r: { cut: string; name: string; value: number | string }) => r.cut },
        { header: 'Name', value: (r) => r.name },
        { header: 'Value', value: (r) => r.value },
      ],
      [
        ...data.byTool.map((t) => ({ cut: 'Tool adoption', name: t.name, value: t.value })),
        ...data.byCategory.map((t) => ({ cut: 'By category', name: t.name, value: t.value })),
        ...data.byActivity.map((t) => ({ cut: 'By activity', name: t.name, value: t.value })),
        ...data.mostUsedToolPerActivity.map((t) => ({ cut: 'Most-used tool', name: t.activity, value: `${t.tool} (${t.nominations})` })),
      ],
    )

  const maxTool = Math.max(1, ...data.byTool.map((t) => t.value))
  const maxAct = Math.max(1, ...data.byActivity.map((t) => t.value))

  return (
    <Panel
      title="Migration Capability Utilization"
      action={<Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportCsv} disabled={!data.totalUsages}>Export</Button>}
    >
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
        Which migration capability was accelerated by GHCP / AppMod / accelerators / Azure tooling — counts are distinct nominations.
        {data.totalUsages === 0 ? ' Capture tool usage in a nomination workspace to populate this.' : ` ${data.nominationsWithUsage} nomination(s) with logged usage.`}
      </Text>
      {data.totalUsages === 0 ? null : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 20 }}>
          <div>
            <Text size={300} weight="semibold" style={{ display: 'block', marginBottom: 6 }}>Tool adoption</Text>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {data.byTool.map((t) => <Bar key={t.name} label={t.name} count={t.value} max={maxTool} />)}
            </div>
          </div>
          <div>
            <Text size={300} weight="semibold" style={{ display: 'block', marginBottom: 6 }}>By activity</Text>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {data.byActivity.map((t) => <Bar key={t.name} label={t.name} count={t.value} max={maxAct} />)}
            </div>
          </div>
          <div>
            <Text size={300} weight="semibold" style={{ display: 'block', marginBottom: 6 }}>Most-used tool per activity</Text>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {data.mostUsedToolPerActivity.map((t) => (
                <div key={t.activity} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '3px 0', borderBottom: '1px solid var(--colorNeutralStroke3)' }}>
                  <span>{t.activity}</span>
                  <span style={{ color: 'var(--colorNeutralForeground3)' }}>{t.tool} · {t.nominations}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </Panel>
  )
}
