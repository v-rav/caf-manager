import { Badge, Button, Text, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular, ArrowUploadRegular } from '@fluentui/react-icons'
import { useMemo, useRef, useState } from 'react'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { Modal } from '../components/Modal'
import { ErrorText, FilterSelect, Loading, PageHeader, Panel } from '../components/common'
import { BarRow as Bar } from '../components/ui'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { ADOPTION_LEVELS } from '../adoption'
import { downloadCsv } from '../export'
import type { CapabilityUtilization, Nomination } from '../types'

const SETTLED = ['Withdrawn']

export function AdoptionPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const cap = useAsync(() => api.capability(region), [region])
  const [tool, setTool] = useState('')
  const [uploading, setUploading] = useState(false)
  const [uploadResult, setUploadResult] = useState<{ success: boolean; messages: string[] } | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const runUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // allow re-selecting the same file
    if (!file) return
    setUploading(true)
    try {
      setUploadResult(await api.uploadAuto(file))
    } catch {
      setUploadResult({ success: false, messages: ['Upload failed. Ensure the file is the Nomination Wize Tool Usage .xlsx export.'] })
    } finally {
      setUploading(false)
    }
  }

  const closeUpload = () => {
    const ok = uploadResult?.success
    setUploadResult(null)
    if (ok) {
      reload()
      cap.reload()
    }
  }

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
        { header: 'Adoption level (manual)', value: (n) => n.ghcpAdoptionLevel ?? 0 },
        { header: 'Effective level', value: (n) => n.ghcpEffectiveLevel ?? n.ghcpAdoptionLevel ?? 0 },
        { header: 'Tool attached', value: (n) => (n.isToolAttached == null ? '' : n.isToolAttached ? 'Yes' : 'No') },
        { header: 'Automation used', value: (n) => (n.isAutomationUsed == null ? '' : n.isAutomationUsed ? 'Yes' : 'No') },
        { header: 'Reported tools (FDO)', value: (n) => (n.ghcpToolsUsed ?? []).join('; ') },
        { header: 'Status', value: (n) => n.status },
      ],
      rows,
    )

  const kpis = useMemo(() => {
    const total = rows.length
    // Effective 0–7: manual level raised by reported-usage / tool-attached evidence (levels 1–3 can't be derived).
    const lvl = (n: Nomination) => n.ghcpEffectiveLevel ?? n.ghcpAdoptionLevel ?? 0
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
    for (const n of rows) counts[Math.min(7, Math.max(0, n.ghcpEffectiveLevel ?? n.ghcpAdoptionLevel ?? 0))]++
    const max = Math.max(1, ...counts)
    return { counts, max }
  }, [rows])

  // Real GHCP usage (from captured Tool × Activity), vs the coarse FDO tool-attached flag above.
  const ghcpAccelerated = cap.data?.byCategory.find((c) => c.name === 'GHCP')?.value ?? 0
  const ghcpPct = rows.length ? Math.round((ghcpAccelerated / rows.length) * 100) : 0

  const byRegion = useMemo(() => {
    const m = new Map<string, { total: number; used: number }>()
    for (const n of rows) {
      const r = n.region || 'Unspecified'
      const e = m.get(r) ?? { total: 0, used: 0 }
      e.total++
      if ((n.ghcpUsageCount ?? 0) > 0) e.used++
      m.set(r, e)
    }
    return [...m.entries()].sort((a, b) => b[1].total - a[1].total)
  }, [rows])

  // Reported tool usage from the FDO "Nomination Wize Tool Usage" export (real captured usage, by tool).
  const reported = useMemo(() => {
    const withUsage = rows.filter((n) => (n.ghcpUsageCount ?? 0) > 0).length
    const byTool = new Map<string, number>()
    for (const n of rows) for (const t of n.ghcpToolsUsed ?? []) byTool.set(t, (byTool.get(t) ?? 0) + 1)
    const tools = [...byTool.entries()].sort((a, b) => b[1] - a[1])
    return {
      withUsage,
      pct: rows.length ? Math.round((withUsage / rows.length) * 100) : 0,
      tools,
      max: Math.max(1, ...tools.map((t) => t[1])),
    }
  }, [rows])

  if (loading && !data) return <Loading label="Loading adoption…" />
  if (error) return <ErrorText error={error} onRetry={reload} />

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <PageHeader
        title="GHCP Adoption"
        subtitle="License readiness · adoption maturity 0–7 · tool usage — across active nominations"
        actions={<>
          <FilterSelect
            label="Tooling"
            value={tool}
            onChange={setTool}
            options={['Tool attached', 'No tool']}
          />
          <input ref={fileInput} type="file" accept=".xlsx" style={{ display: 'none' }} onChange={runUpload} />
          <Tooltip relationship="description" content="Upload the FDO Nomination Wize Tool Usage export (.xlsx). The file type is auto-detected and replaces the previous usage data.">
            <Button appearance="primary" icon={<ArrowUploadRegular />} disabled={uploading} onClick={() => fileInput.current?.click()}>
              {uploading ? 'Uploading…' : 'Upload'}
            </Button>
          </Tooltip>
          <Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportRows} disabled={!rows.length}>Export CSV</Button>
        </>}
      />

      <Modal
        open={!!uploadResult}
        title={uploadResult?.success ? 'Upload processed' : 'Upload not processed'}
        onClose={closeUpload}
        onSubmit={closeUpload}
        submitLabel={uploadResult?.success ? 'Refresh' : 'Close'}
        maxWidth={620}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {(uploadResult?.messages ?? []).map((m, i) => (
            <div key={i} style={{ fontSize: 13, fontWeight: /^(Detected|Records):/.test(m) ? 600 : 400 }}>{m}</div>
          ))}
        </div>
      </Modal>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: 12 }}>
        <KpiCard label="Licensed (L4+)" value={kpis.licensed} tone="success" />
        <KpiCard label="Awaiting license (L1–3)" value={kpis.awaiting} tone={kpis.awaiting ? 'warning' : 'neutral'} />
        <KpiCard label="GHCP-used %" value={`${kpis.usedPct}%`} tone={kpis.usedPct >= 50 ? 'success' : 'brand'} />
        <KpiCard label="Tool-attached % (FDO)" value={`${kpis.toolPct}%`} tone="brand" />
        <KpiCard label="Reported usage (FDO)" value={`${reported.withUsage} · ${reported.pct}%`} tone={reported.withUsage ? 'success' : 'neutral'} />
        <KpiCard label="GHCP-accelerated (usage)" value={`${ghcpAccelerated} · ${ghcpPct}%`} tone={ghcpAccelerated ? 'success' : 'neutral'} />
      </div>

      <Panel title="Adoption-level distribution (0–7)">
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 8 }}>
          Effective level: a manually-set level is kept; otherwise levels 5–7 are evidenced by reported GHCP usage and
          level 4 by tool-attached. Levels 1–3 (commercial) can't be inferred, so uncaptured nominations stay at 0.
        </Text>
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
              {r} · {e.total} · reported {e.total ? Math.round((e.used / e.total) * 100) : 0}%
            </Badge>
          ))}
        </div>
      </Panel>

      <Panel title="Reported GHCP tool usage (FDO)">
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
          Actual tool usage reported in the FDO “Nomination Wize Tool Usage” export — counts are nominations that
          reported each tool. {reported.withUsage
            ? `${reported.withUsage} nomination(s) reported usage.`
            : 'No reported usage yet — upload the Tool Usage export to populate this.'}
        </Text>
        {reported.tools.length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {reported.tools.map(([name, count]) => (
              <Bar key={name} label={name} count={count} max={reported.max} />
            ))}
          </div>
        )}
      </Panel>

      <CapabilitySection cap={cap} />
    </div>
  )
}

// Leadership rollup: which capabilities were accelerated by which tools (distinct nominations).
function CapabilitySection({ cap }: { cap: ReturnType<typeof useAsync<CapabilityUtilization>> }) {
  const { data, loading, error, reload } = cap
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
      action={<Button appearance="secondary" icon={<ArrowDownloadRegular />} onClick={exportCsv} disabled={!data.totalUsages}>Export CSV</Button>}
    >
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 12 }}>
        Which migration capability was accelerated by which tool — tools aligned to the master taxonomy and the activity
        inferred from the FDO usage notes; counts are distinct nominations.
        {data.totalUsages === 0 ? ' Upload the Nomination Wize Tool Usage export to populate this.' : ` ${data.nominationsWithUsage} nomination(s) with reported usage.`}
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
