import { Badge, Button, SearchBox, Switch, Text, Tooltip } from '@fluentui/react-components'
import { ArrowDownloadRegular, ArrowUploadRegular, DismissRegular, EditRegular } from '@fluentui/react-icons'
import { useMemo, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { useAuth } from '../auth'
import { DataTable } from '../components/DataTable'
import { Modal } from '../components/Modal'
import { ErrorText, FilterSelect, FilterText, Loading, NominationLink, PageHeader, Panel } from '../components/common'
import { FilterBar, HealthBadge, SlaBadge } from '../components/ui'
import { useFacets, useUrlFilters } from '../filters'
import { nominationInFy, useFy } from '../fy'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { Nomination } from '../types'
import { ManageNominationDialog } from './nominations/ManageNominationDialog'
import { LinkChip, NominationDetail, presentWaves } from './nominations/NominationDetail'
import { SummaryCard } from './nominations/SummaryCards'

// Wave links are informational — no type is mandatory. These filters help find records to enrich.
const LINK_FILTERS = ['No waves', 'Has any waves', 'Has DB', 'Has Security']
const SLA_TIERS = ['Warn', 'Escalate', 'Defer']
const QUALITY = ['Check FDO days', 'Missing cores']
const STATUS_ORDER = ['On Track', 'Waiting', 'Blocked', 'Other']
const STAGE_TONE = ['neutral', 'warning', 'warning', 'brand', 'neutral', 'neutral', 'success'] as const
const BADGE_COLOR = ['brand', 'brand', 'brand', 'brand', 'warning', 'subtle', 'success'] as const

const KEYS = ['approval', 'stage', 'status', 'sla', 'cls', 'links', 'ghcp', 'quality', 'owner', 'q'] as const
type Key = (typeof KEYS)[number]
// Full FDO stage 1–7 (5 Deferred · 6 Cancelled / Archived · 7 Completed) — from the server.
const stageText = (n: Nomination) => (n.fdoStage ? `${n.fdoStage} · ${n.fdoStageLabel ?? ''}` : '')
// Dropdowns whose options narrow to values still present under the other filters.
const facetPickers = {
  stage: stageText,
  status: (n: Nomination) => n.healthBucket ?? '',
}
const has = (v: string | undefined | null, part: string) => (v ?? '').toLowerCase().includes(part.trim().toLowerCase())
const ownedBy = (n: Nomination, q: string) => has(n.solutionArchitect, q) || has(n.projectCoordinator, q) || has(n.cftlPrimary, q)

// Trim the noisy offering prefix (e.g. "App Modernization Nominations - Wave 18" → "Wave 18").
function offeringShort(tech: string): string {
  const wave = tech.match(/Wave\s*\d+/i)
  if (wave) return wave[0].replace(/\s+/, ' ')
  const dash = tech.lastIndexOf(' - ')
  return dash >= 0 ? tech.slice(dash + 3).trim() : tech
}

// True when the person (name substring) holds any ownership/assignment role on the nomination.
function matchesPerson(n: Nomination, person: string): boolean {
  const p = person.toLowerCase()
  const has = (v?: string) => (v ?? '').toLowerCase().includes(p)
  return has(n.projectCoordinator) || has(n.cftlPrimary) || has(n.solutionArchitect)
    || (n.assignedResources ?? []).some((r) => r.name.toLowerCase().includes(p))
}

// Nomination identity search (account, TPID, FDO Task Id); people are searched via the SA / PM / CFTL box.
function matchesText(n: Nomination, query: string): boolean {
  const q = query.toLowerCase()
  const has = (v?: string | null) => (v ?? '').toLowerCase().includes(q)
  return has(n.accountName) || has(n.tpid) || has(n.externalTaskId)
}

function matchesLinks(n: Nomination, links: string): boolean {
  switch (links) {
    case 'No waves': return n.noWavesLinked
    case 'Has any waves': return !n.noWavesLinked
    case 'Has DB': return n.dbLinked
    case 'Has Security': return n.securityLinked
    default: return true
  }
}

export function NominationsPage() {
  const { region } = useRegion()
  const { fy } = useFy()
  const navigate = useNavigate()
  const { user } = useAuth()
  const isSa = user?.role === 'Sa'
  const [searchParams] = useSearchParams()
  // Drill-through from Capacity: ?person=<name> filters to that person's nominations (any role).
  const person = searchParams.get('person') ?? ''
  // SAs land on their own book of work by default so they focus on their pending actions.
  const [mine, setMine] = useState(isSa)
  const effectivePerson = person || (mine && isSa ? user?.displayName ?? '' : '')

  const { data, loading, error, reload } = useAsync(() => api.nominations(region), [region])
  const { data: resourceList } = useAsync(() => api.resources({}), [])
  const { data: vocab } = useAsync(() => api.nominationVocab(), [])
  const { data: statusInfo } = useAsync(() => api.statusRules(), [])
  const classOn = !!vocab?.classificationEnabled

  const { f, set, reset, any: anyFilter } = useUrlFilters(KEYS)
  const search = useDebounced(f.q)
  // Approval defaults to the configured value (Configuration → Workflow status rules); "all" in the URL means no filter.
  const approval = f.approval === 'all' ? '' : f.approval || (person ? '' : statusInfo?.defaultApproval ?? '')
  const setApproval = (v: string) => set('approval')(v || 'all')
  const toggle = (k: Key, v: string) => set(k)(f[k] === v ? '' : v)

  const [editing, setEditing] = useState<Nomination | null>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadResult, setUploadResult] = useState<{ success: boolean; messages: string[] } | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const runUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // allow re-selecting the same file
    if (!file) return
    setUploading(true)
    try {
      // Server auto-detects Detail View / Summary of Offerings / Nominations In-Flight / Tool Usage from the columns.
      setUploadResult(await api.uploadAuto(file))
    } catch {
      setUploadResult({ success: false, messages: ['Upload failed. Ensure the file is a valid .xlsx export (Detail View, Summary of Offerings, Nominations In-Flight, or Nomination Wize Tool Usage).'] })
    } finally {
      setUploading(false)
    }
  }
  const closeUpload = () => {
    const ok = uploadResult?.success
    setUploadResult(null)
    if (ok) reload()
  }

  // Approval + fiscal-year scope drive both the summary cards and the grid so counts match what's shown.
  const scoped = useMemo(
    () => (data ?? []).filter((n) => nominationInFy(n, fy) && (!approval || (n.approvalStatus ?? '') === approval)),
    [data, approval, fy],
  )
  // Fixed-option filters first; the faceted dropdowns then narrow within what's left.
  const base = useMemo(() => scoped.filter((n) =>
    (!f.sla || n.staleTier === f.sla)
    && (!f.cls || (f.cls === 'Strategic (all)' ? n.isStrategic : n.classification === f.cls))
    && matchesLinks(n, f.links)
    && (!f.ghcp || (f.ghcp === 'Reported' ? (n.ghcpUsageCount ?? 0) > 0 : (n.ghcpUsageCount ?? 0) === 0))
    && (!f.quality || (n.dataQualityFlags ?? []).includes(f.quality))
    && (!f.owner || ownedBy(n, f.owner))
    && (!effectivePerson || matchesPerson(n, effectivePerson))
    && (!search || matchesText(n, search))), [scoped, f, effectivePerson, search])
  const { rows, options } = useFacets<Nomination, keyof typeof facetPickers>(base, facetPickers, f, { status: STATUS_ORDER })

  const count = (p: (n: Nomination) => boolean) => scoped.filter(p).length
  // Approval counts span ALL nominations (not the approval-scoped set) so the card shows the full split.
  const approvalCount = (s: string) => (data ?? []).filter((n) => (n.approvalStatus ?? '') === s).length
  const approvalOptions = useMemo(
    () => [...new Set(['Approved', 'Declined', ...(data ?? []).map((n) => n.approvalStatus).filter((v): v is string => !!v)])],
    [data],
  )
  // Stage names come from the server (`fdoStageLabel`).
  const stageName = (k: number) => (data ?? []).find((n) => n.fdoStage === k)?.fdoStageLabel || `Stage ${k}`

  const exportHref = api.exportUrl('nominations', region, {
    approval,
    stage: f.stage ? f.stage.split(' ')[0] : undefined,
    health: f.status,
    sla: f.sla,
    links: f.links,
    quality: f.quality,
    owner: f.owner,
    search,
    fy: fy === 'all' ? undefined : String(fy),
  })

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <PageHeader title="Nomination Pipeline" subtitle="FDO waves with portal governance · filters are kept in the address, so a view can be bookmarked or shared" />

      <FilterBar active={anyFilter} onReset={reset}>
        <SearchBox aria-label="Search nominations" placeholder="Account, TPID or Task Id" value={f.q} onChange={(_, d) => set('q')(d.value)} style={{ minWidth: 220 }} />
        {isSa && <Switch checked={mine} onChange={(_, d) => setMine(d.checked)} label="My nominations" />}
        <FilterSelect label="Approval status" value={approval} options={approvalOptions} onChange={setApproval} minWidth={150} />
        <FilterSelect label="Stage" value={f.stage} options={[1, 2, 3, 4, 5, 6, 7].map((k) => `${k} · ${stageName(k)}`)} onChange={set('stage')} minWidth={190} />
        <FilterSelect label="Status" value={f.status} options={options.status} onChange={set('status')} minWidth={120} />
        <FilterSelect label="SLA tier" value={f.sla} options={SLA_TIERS} onChange={set('sla')} minWidth={120} />
        <FilterText label="SA / PM / CFTL" value={f.owner} onChange={set('owner')} placeholder="Search owner name" minWidth={190} />
        {classOn && <FilterSelect label="Classification" value={f.cls} options={['Strategic (all)', ...(vocab?.classifications ?? [])]} onChange={set('cls')} minWidth={160} />}
        <FilterSelect label="Links" value={f.links} options={LINK_FILTERS} onChange={set('links')} minWidth={140} />
        <FilterSelect label="GHCP usage" value={f.ghcp} options={['Reported', 'None']} onChange={set('ghcp')} minWidth={130} />
        <FilterSelect label="Data quality (fix in FDO)" value={f.quality} options={QUALITY} onChange={set('quality')} minWidth={170} />
      </FilterBar>

      {person && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 12px', borderRadius: 6, background: 'var(--colorBrandBackground2)', border: '1px solid var(--colorBrandStroke2)', alignSelf: 'flex-start' }}>
          <Text size={300}>Filtered to <strong>{person}</strong> (PM / CFTL / SA / assigned)</Text>
          <Button size="small" appearance="subtle" icon={<DismissRegular />} onClick={() => navigate('/nominations')}>Clear</Button>
        </div>
      )}

      {loading && !data ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <SummaryCard
              title={`Approval status · ${data?.length ?? 0} total`}
              items={['Approved', 'Provisionally Approved', 'Active Concierge', 'Declined'].map((s, i) => ({
                label: s === 'Provisionally Approved' ? 'Prov. Approved' : s,
                value: approvalCount(s),
                tone: (['success', 'brand', 'neutral', 'danger'] as const)[i],
                active: approval === s,
                onClick: () => setApproval(approval === s ? '' : s),
              }))}
            />
            <SummaryCard
              title="Migration stage"
              items={[1, 2, 3, 4, 5, 6, 7].map((k) => {
                const label = `${k} · ${stageName(k)}`
                return { label, value: count((n) => n.fdoStage === k), tone: STAGE_TONE[k - 1], active: f.stage === label, onClick: () => toggle('stage', label) }
              })}
            />
            <SummaryCard
              title="Status & SLA"
              items={[
                { label: 'Blocked / Waiting', value: count((n) => n.healthBucket === 'Blocked' || n.healthBucket === 'Waiting'), tone: 'warning' },
                ...SLA_TIERS.map((t, i) => ({
                  label: `${t} (${[3, 5, 10][i]}d+ over target)`,
                  value: count((n) => n.staleTier === t),
                  tone: (t === 'Defer' ? 'danger' : 'warning') as 'danger' | 'warning',
                  active: f.sla === t,
                  onClick: () => toggle('sla', t),
                })),
              ]}
            />
            <SummaryCard
              title={`Linkage · ${scoped.length} shown`}
              items={[
                { label: 'No waves', value: count((n) => n.noWavesLinked), tone: 'warning', active: f.links === 'No waves', onClick: () => toggle('links', 'No waves') },
                { label: 'Has waves', value: count((n) => !n.noWavesLinked), tone: 'success', active: f.links === 'Has any waves', onClick: () => toggle('links', 'Has any waves') },
              ]}
            />
            <SummaryCard
              title="GHCP usage (FDO)"
              items={[
                { label: 'Reported', value: count((n) => (n.ghcpUsageCount ?? 0) > 0), tone: 'success', active: f.ghcp === 'Reported', onClick: () => toggle('ghcp', 'Reported') },
                { label: 'None', value: count((n) => (n.ghcpUsageCount ?? 0) === 0), tone: 'neutral', active: f.ghcp === 'None', onClick: () => toggle('ghcp', 'None') },
              ]}
            />
          </div>

          <Panel
            title={`Nominations · ${rows.length}`}
            action={
              <div style={{ display: 'flex', gap: 8 }}>
                {!isSa && (<>
                  <input ref={fileInput} type="file" accept=".xlsx" style={{ display: 'none' }} onChange={runUpload} />
                  <Tooltip
                    relationship="description"
                    content="Upload any of the 4 workbooks — Detail View (pipeline), Summary of All Offerings (enrichment), Nominations In-Flight (account master), or Nomination Wize Tool Usage (GHCP usage). The type is auto-detected."
                  >
                    <Button appearance="primary" icon={<ArrowUploadRegular />} disabled={uploading} onClick={() => fileInput.current?.click()}>
                      {uploading ? 'Uploading…' : 'Upload'}
                    </Button>
                  </Tooltip>
                </>)}
                <Button as="a" href={exportHref} appearance="secondary" icon={<ArrowDownloadRegular />}>Export Excel</Button>
              </div>
            }
          >
            <DataTable<Nomination>
              ariaLabel="Nominations"
              rows={rows}
              rowKey={(n) => n.id}
              defaultSort={{ key: 'opened', dir: 'desc' }}
              emptyMessage="No nominations match these filters. Try Reset, or widen the Approval status / FY scope."
              expandedContent={(n) => <NominationDetail n={n} />}
              columns={[
                { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => <NominationLink id={n.id} name={n.accountName} /> },
                { key: 'tpid', header: 'TPID', sortValue: (n) => n.tpid ?? '', render: (n) => n.tpid ?? '—' },
                { key: 'offering', header: 'Offering', sortValue: (n) => n.technology ?? '', render: (n) => n.technology ? <Tooltip relationship="description" content={n.technology}><span>{offeringShort(n.technology)}</span></Tooltip> : '—' },
                { key: 'region', header: 'Region', sortValue: (n) => n.region },
                {
                  key: 'stage',
                  header: 'Stage',
                  align: 'center',
                  sortValue: (n) => n.fdoStage ?? 0,
                  render: (n) => n.fdoStage ? (
                    <Tooltip relationship="description" content={`Stage ${n.fdoStage}: ${n.fdoStageLabel} — ${n.migrationStatus}`}>
                      <Badge appearance="tint" color={BADGE_COLOR[n.fdoStage - 1]} size="medium">{n.fdoStage}</Badge>
                    </Tooltip>
                  ) : <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>,
                },
                {
                  key: 'state',
                  header: 'Status',
                  sortValue: (n) => n.healthBucket ?? '',
                  render: (n) => <HealthBadge bucket={n.healthBucket} tone={n.healthTone} currentState={n.currentState} />,
                },
                {
                  key: 'summary',
                  header: 'Summary',
                  sortValue: (n) => n.staleTier || '',
                  render: (n) => {
                    const details = (n.remarks ?? '').replace(/\s+/g, ' ').trim()
                    const age = n.stageAgeDays ?? n.daysSinceUpdate
                    const breakdown = (n.fdoStageDays ?? []).some((d) => d != null)
                      ? (n.fdoStageDays ?? []).map((d, i) => `S${i + 1} ${d ?? '–'}`).join(' · ') + (n.fdoTotalDays != null ? ` = ${n.fdoTotalDays}` : '')
                      : null
                    return (
                      <Tooltip
                        relationship="description"
                        content={
                          <div style={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 320 }}>
                            {n.currentState && <div><b>Current State:</b> {n.currentState}</div>}
                            {n.blockedReason && <div><b>Blocker:</b> {n.blockedReason}</div>}
                            {n.blockedSince && <div><b>Blocked since:</b> {n.blockedSince}</div>}
                            {n.followUpDate && <div><b>Follow-up:</b> {n.followUpDate}</div>}
                            <div><b>Age in stage:</b> {age} days{n.stageTargetDays ? ` / target ${n.stageTargetDays}` : ''}{n.staleTier ? ` · SLA ${n.staleTier}` : ''}</div>
                            {breakdown && <div><b>FDO days by stage:</b> {breakdown}</div>}
                            {n.velocity != null && <div><b>Velocity:</b> {Math.round(n.velocity * 100)}% ({n.velocityTargetDays} target ÷ {n.velocityDays} actual days)</div>}
                            {(n.dataQualityFlags ?? []).length > 0 && <div><b>Fix in FDO:</b> {n.dataQualityFlags!.join(' · ')}</div>}
                            {n.nextStep && <div style={{ color: 'var(--colorPaletteRedForeground1)' }}><b>Next step:</b> {n.nextStep}</div>}
                            {n.waves.length > 0 && <div><b>Waves:</b> {n.waves.map((w) => w.waveType).join(', ')}</div>}
                            {details && <div><b>Details:</b> {details}</div>}
                          </div>
                        }
                      >
                        <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center', cursor: 'help', flexWrap: 'wrap' }}>
                          <SlaBadge tier={n.staleTier} tone={n.slaTone} />
                          {n.blockedReason && <Badge appearance="tint" color="danger" size="small">{n.blockedReason}</Badge>}
                          {(n.dataQualityFlags ?? []).length > 0 && <Badge appearance="tint" color="warning" size="small">⚠ fix in FDO</Badge>}
                          <span style={{ color: 'var(--colorNeutralForeground3)' }}>{age}d in stage</span>
                        </span>
                      </Tooltip>
                    )
                  },
                },
                { key: 'pm', header: 'PM', sortValue: (n) => n.projectCoordinator ?? '', render: (n) => n.projectCoordinator ?? '—' },
                { key: 'cftl', header: 'CFTL', sortValue: (n) => n.cftlPrimary ?? '', render: (n) => n.cftlPrimary ?? '—' },
                { key: 'sa', header: 'SA', sortValue: (n) => n.solutionArchitect ?? '', render: (n) => n.solutionArchitect ?? '—' },
                {
                  key: 'ghcp',
                  header: 'GHCP',
                  sortValue: (n) => n.ghcpUsageCount ?? 0,
                  render: (n) => (n.ghcpToolsUsed?.length ?? 0) === 0 ? (
                    <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                  ) : (
                    <Tooltip relationship="description" content={`Reported tool usage (FDO): ${n.ghcpToolsUsed!.join(', ')}`}>
                      <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
                        {n.ghcpToolsUsed!.slice(0, 2).map((t) => <Badge key={t} appearance="tint" color="success" size="small">{t}</Badge>)}
                        {n.ghcpToolsUsed!.length > 2 && <Badge appearance="tint" color="success" size="small">+{n.ghcpToolsUsed!.length - 2}</Badge>}
                      </span>
                    </Tooltip>
                  ),
                },
                {
                  key: 'links',
                  header: 'Waves',
                  sortValue: (n) => n.waveCount,
                  render: (n) => (
                    <Tooltip relationship="description" content={n.noWavesLinked ? 'No waves linked — review and associate any related waves.' : `Linked waves: ${presentWaves(n).join(', ')}`}>
                      <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
                        <LinkChip label="DB" linked={n.dbLinked} />
                        <LinkChip label="ALZ" linked={n.alzLinked} />
                        <LinkChip label="Sec" linked={n.securityLinked} />
                      </span>
                    </Tooltip>
                  ),
                },
                {
                  key: 'team',
                  header: 'Team',
                  sortValue: (n) => n.assignedResourceCount,
                  render: (n) => n.assignedResourceCount === 0 ? (
                    <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                  ) : (
                    <Tooltip
                      relationship="description"
                      content={
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 280 }}>
                          {n.assignedResources.map((a) => <div key={a.resourceId}><b>{a.role ?? 'Role TBD'}:</b> {a.name}</div>)}
                        </div>
                      }
                    >
                      <Badge appearance="tint" color="brand" size="small" style={{ cursor: 'help' }}>{n.assignedResourceCount}</Badge>
                    </Tooltip>
                  ),
                },
                {
                  key: 'actions',
                  header: '',
                  align: 'center',
                  render: (n) => (
                    <Tooltip relationship="label" content="Manage">
                      <Button appearance="subtle" size="small" icon={<EditRegular />} aria-label="Manage" onClick={() => setEditing(n)} />
                    </Tooltip>
                  ),
                },
              ]}
            />
          </Panel>
        </>
      )}

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

      {editing && statusInfo && (
        <ManageNominationDialog
          key={editing.id}
          nomination={editing}
          region={region}
          resources={resourceList ?? []}
          classOn={classOn}
          classifications={vocab?.classifications ?? []}
          statusInfo={statusInfo}
          onClose={() => setEditing(null)}
          onChanged={reload}
        />
      )}
    </div>
  )
}
