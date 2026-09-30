import {
  Badge,
  Button,
  Combobox,
  Dropdown,
  Input,
  Option,
  SearchBox,
  Switch,
  Text,
  Textarea,
  Tooltip,
} from '@fluentui/react-components'
import { AddRegular, ArrowDownloadRegular, ArrowUploadRegular, DeleteRegular, DismissRegular, EditRegular } from '@fluentui/react-icons'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { Modal } from '../components/Modal'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { ADOPTION_LEVELS } from '../adoption'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import { useFy, nominationInFy } from '../fy'
import { useAuth } from '../auth'
import { useMemo, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { Nomination, NominationUpdate } from '../types'

// Read-only labelled value used in the Manage dialog's FDO context panel.
function ReadField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block' }}>
        {label}
      </Text>
      <Text size={300}>{children}</Text>
    </div>
  )
}

// Wave links are informational — no type is mandatory. These filters help find records to enrich.
const LINK_FILTERS = ['No waves', 'Has any waves', 'Has DB', 'Has Security']

// Present wave-type short labels for a nomination (empty = none linked yet).
function presentWaves(n: Nomination): string[] {
  const out: string[] = []
  if (n.dbLinked) out.push('DB')
  if (n.securityLinked) out.push('Sec')
  if (n.alzLinked) out.push('ALZ')
  return out
}

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
  return (
    has(n.projectCoordinator) ||
    has(n.cftlPrimary) ||
    has(n.solutionArchitect) ||
    (n.assignedResources ?? []).some((r) => r.name.toLowerCase().includes(p))
  )
}

// Free-text search across account, TPID and the three ownership roles (PM / CFTL / SA).
function matchesText(n: Nomination, query: string): boolean {
  const q = query.toLowerCase()
  const has = (v?: string) => (v ?? '').toLowerCase().includes(q)
  return has(n.accountName) || has(n.tpid) || has(n.projectCoordinator) || has(n.cftlPrimary) || has(n.solutionArchitect)
}

// Labelled full-width form field for the Manage dialog (keeps controls aligned).
function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground2)' }}>
        {label}
      </Text>
      {children}
    </div>
  )
}

// Canonical Stage (Migration Status) values — editable in Manage; text matches migrationStage() keywords.
const STAGE_OPTIONS = [
  { value: '1 - Validating & Initial Scope', label: '1 — Validating' },
  { value: '2 - Executing Pre-Requisites', label: '2 — Pre-Requisites' },
  { value: '3 - Finalize Scope', label: '3 — Finalize Scope' },
  { value: '4 - Executing Migration', label: '4 — Executing Migration' },
]

// Wave-presence chip: brand (blue) when the wave is linked, red when it isn't.
function LinkChip({ label, linked }: { label: string; linked: boolean }) {
  return (
    <Badge appearance={linked ? 'filled' : 'tint'} color={linked ? 'brand' : 'danger'} size="small">
      {label}
    </Badge>
  )
}

type StatTone = 'neutral' | 'success' | 'warning' | 'danger' | 'brand'
const STAT_TONE: Record<StatTone, string> = {
  neutral: 'var(--colorNeutralForeground1)',
  success: 'var(--colorPaletteGreenForeground1)',
  warning: 'var(--colorPaletteDarkOrangeForeground1)',
  danger: 'var(--colorPaletteRedForeground1)',
  brand: 'var(--colorBrandForeground1)',
}

interface Stat {
  label: string
  value: number
  tone?: StatTone
  active?: boolean
  onClick?: () => void
}

// One metric inside a SummaryCard; clickable stats act as drill-through filters.
function StatCell({ label, value, tone = 'neutral', active, onClick }: Stat) {
  return (
    <div
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onClick() } } : undefined}
      title={onClick ? `Filter by ${label}` : undefined}
      style={{
        border: '1px solid',
        borderColor: active ? 'var(--colorBrandStroke1)' : 'var(--colorNeutralStroke2)',
        background: active ? 'var(--colorBrandBackground2)' : 'transparent',
        borderRadius: 6,
        padding: '6px 10px',
        cursor: onClick ? 'pointer' : 'default',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
      }}
    >
      <span style={{ fontSize: 18, fontWeight: 700, lineHeight: 1.1, color: STAT_TONE[tone] }}>{value}</span>
      <span style={{ fontSize: 11, color: 'var(--colorNeutralForeground3)' }}>{label}</span>
    </div>
  )
}

// A titled summary card holding a small grid of related stats.
function SummaryCard({ title, items, columns = 2 }: { title: string; items: Stat[]; columns?: number }) {
  return (
    <div
      style={{
        flex: '1 1 240px',
        minWidth: 220,
        background: 'var(--colorNeutralBackground1)',
        border: '1px solid var(--colorNeutralStroke2)',
        borderRadius: 8,
        padding: 12,
        boxShadow: 'var(--shadow2)',
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
      }}
    >
      <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground3)', textTransform: 'uppercase', letterSpacing: '.4px' }}>
        {title}
      </Text>
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${columns}, 1fr)`, gap: 6 }}>
        {items.map((it) => (
          <StatCell key={it.label} {...it} />
        ))}
      </div>
    </div>
  )
}

// The migration journey has four stages; the summary counts how many nominations sit in each.
const STAGES = [
  { n: 1, label: 'Validating', match: 'validating', tone: 'neutral' as const },
  { n: 2, label: 'Pre-Requisites', match: 'pre-requisite', tone: 'warning' as const },
  { n: 3, label: 'Finalize Scope', match: 'finalize', tone: 'warning' as const },
  { n: 4, label: 'Executing Migration', match: 'executing migration', tone: 'brand' as const },
]

// Migration Status text → numeric stage of the migration journey (1–4).
function migrationStage(migrationStatus?: string): { n: number; label: string } | null {
  const t = (migrationStatus ?? '').toLowerCase()
  const s = STAGES.find((x) => t.includes(x.match))
  return s ? { n: s.n, label: s.label } : null
}

// Current State text → Badge colour (on track / waiting / blocked).
function stateColor(cs?: string): 'success' | 'warning' | 'danger' | 'informative' {
  const t = (cs ?? '').toLowerCase()
  if (t.includes('block')) return 'danger'
  if (t.includes('waiting') || t.includes('follow')) return 'warning'
  if (t.includes('on track')) return 'success'
  return 'informative'
}

const STATUS_OPTIONS = [
  'Open',
  'In Progress',
  'Blocked',
  'Waiting for Customer Action',
  'Customer Deferred',
  'Waiting on Follow-up',
  'Completed',
  'Closed',
]

const BLOCKER_OPTIONS = [
  'Waiting for Customer Action',
  'Approval Pending',
  'Access Pending',
  'Landing Zone Pending',
  'Testing/Validation Pending',
  'Dependency Pending',
  'Budget/Priority Hold',
  'Internal Alignment',
]

const WAVE_OPTIONS = ['App', 'DB', 'Security/Defender', 'Landing Zone', 'Dispatch', 'Related']

// Delivery roles a resource can hold on a nomination (operational staffing, not the capacity model).
const ASSIGN_ROLES = ['Solution Architect', 'Migration Engineer', 'DevOps Engineer']

// Which resource Role values are eligible for each delivery role (keyword match, case-insensitive).
// Solution Architect ← *Architect*; Migration Engineer ← *Engineer* (non-DevOps) / SME / ME; DevOps ← *DevOps*.
function eligibleForRole(assignRole: string, resourceRole?: string): boolean {
  const r = (resourceRole ?? '').toLowerCase()
  switch (assignRole) {
    case 'Solution Architect':
      return r.includes('architect')
    case 'Migration Engineer':
      return (r.includes('engineer') && !r.includes('devops')) || r.includes('migration') || r.includes('sme')
    case 'DevOps Engineer':
      return r.includes('devops')
    default:
      return true
  }
}

const BLOCKED_STATES = ['Blocked', 'Waiting for Customer Action', 'Waiting on Follow-up']

// Lightweight keyword → blocker-reason suggestion from free-text (no AI needed).
function suggestBlocker(text?: string): string | undefined {
  const t = (text ?? '').toLowerCase()
  if (!t) return undefined
  if (/(access|permission|rbac)/.test(t)) return 'Access Pending'
  if (/(landing zone|\balz\b|prereq|pre-requisite)/.test(t)) return 'Landing Zone Pending'
  if (/(approv|sign-?off)/.test(t)) return 'Approval Pending'
  if (/(test|uat|validation)/.test(t)) return 'Testing/Validation Pending'
  if (/(budget|priorit|cost)/.test(t)) return 'Budget/Priority Hold'
  if (/(depend|blocked by)/.test(t)) return 'Dependency Pending'
  if (/(align|internal)/.test(t)) return 'Internal Alignment'
  if (/(customer|client|awaiting)/.test(t)) return 'Waiting for Customer Action'
  return undefined
}

const staleTone: Record<string, { label: string; color: string; bg: string }> = {
  Warn: { label: 'Warn', color: '#8a6d00', bg: '#fff4ce' },
  Escalate: { label: 'Escalate', color: '#8a3b00', bg: '#fed9cc' },
  Defer: { label: 'Defer', color: '#a4262c', bg: '#fde7e9' },
}

// >6 weeks in an execution stage (customer-driven) → Customer Deferred, per the governance cadence.
const DEFER_WEEKS_DAYS = 42

// Documented Day 3/5/10 action for a stale Stage 2–4 nomination (only set when an SLA tier applies).
function recommendedAction(staleTier: string, ageDays: number): string | null {
  if (!staleTier) return null
  if (ageDays >= DEFER_WEEKS_DAYS) return 'Delayed >6 weeks — move to Customer Deferred & set follow-up'
  switch (staleTier) {
    case 'Warn':
      return 'Day 3 — send reminder, then move to Blocked'
    case 'Escalate':
      return 'Day 5 — send 2nd reminder'
    case 'Defer':
      return 'Day 10 — final reminder, move to Customer Deferred & set follow-up'
    default:
      return null
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
  const [currentStateFilter, setCurrentStateFilter] = useState('')
  const [migrationFilter, setMigrationFilter] = useState('')
  const [slaFilter, setSlaFilter] = useState('')
  const [classFilter, setClassFilter] = useState('')
  const [linkFilter, setLinkFilter] = useState('')
  // Default the pipeline to Approved nominations; a person drill-through widens to all approvals.
  const [approvalFilter, setApprovalFilter] = useState(person ? '' : 'Approved')
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounced(search)

  // Manage dialog state.
  const [editing, setEditing] = useState<Nomination | null>(null)
  const [form, setForm] = useState<NominationUpdate>({ status: 'Open' })
  const [waveType, setWaveType] = useState('App')
  const [waveRef, setWaveRef] = useState('')
  // Resource-assignment state (Manage dialog).
  const [assignResourceId, setAssignResourceId] = useState<number | null>(null)
  const [assignRole, setAssignRole] = useState(ASSIGN_ROLES[0])
  const [resourceQuery, setResourceQuery] = useState('')
  // True only while the user is typing a search, so a seeded/selected value doesn't filter the list.
  const [resourceTyping, setResourceTyping] = useState(false)
  const [busy, setBusy] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [uploadResult, setUploadResult] = useState<{ success: boolean; messages: string[] } | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const runUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // allow re-selecting the same file
    if (!file) return
    setUploading(true)
    try {
      // Server auto-detects Detail View / Summary of Offerings / Nominations In-Flight from the columns.
      const res = await api.uploadAuto(file)
      setUploadResult(res)
    } catch {
      setUploadResult({ success: false, messages: ['Upload failed. Ensure the file is a valid .xlsx export (Detail View, Summary of Offerings, or Nominations In-Flight).'] })
    } finally {
      setUploading(false)
    }
  }

  // Approval + fiscal-year scope drive both the KPIs and the grid so counts match what's shown.
  const scoped = useMemo(
    () => (data ?? []).filter((n) => nominationInFy(n, fy) && (!approvalFilter || (n.approvalStatus ?? '') === approvalFilter)),
    [data, approvalFilter, fy],
  )

  const stageCount = (match: string) =>
    scoped.filter((n) => (n.migrationStatus ?? '').toLowerCase().includes(match)).length

  const blockedTotal = scoped.filter((n) => BLOCKED_STATES.includes(n.status)).length
  const staleCount = (tier: string) => scoped.filter((n) => n.staleTier === tier).length
  const noWaveTotal = scoped.filter((n) => n.noWavesLinked).length
  // Approval counts span ALL nominations (not the approval-scoped set) so the card shows the full split.
  const approvalCount = (s: string) => (data ?? []).filter((n) => (n.approvalStatus ?? '') === s).length
  const toggleApproval = (s: string) => setApprovalFilter(approvalFilter === s ? '' : s)

  const approvalOptions = useMemo(
    () => [...new Set(['Approved', 'Declined', ...(data ?? []).map((n) => n.approvalStatus).filter((v): v is string => !!v)])],
    [data],
  )

  const migrationOptions = useMemo(
    () => [...new Set((data ?? []).map((n) => n.migrationStatus).filter((v): v is string => !!v))].sort(),
    [data],
  )
  const currentStateOptions = useMemo(
    () => [...new Set((data ?? []).map((n) => n.currentState).filter((v): v is string => !!v))].sort(),
    [data],
  )
  const rows = useMemo(
    () =>
      scoped.filter(
        (n) =>
          (!currentStateFilter || n.currentState === currentStateFilter) &&
          (!migrationFilter || n.migrationStatus === migrationFilter) &&
          (!slaFilter || n.staleTier === slaFilter) &&
          (!classFilter ||
            (classFilter === 'Strategic (all)' ? n.isStrategic : n.classification === classFilter)) &&
          (!linkFilter ||
            (linkFilter === 'No waves' && n.noWavesLinked) ||
            (linkFilter === 'Has any waves' && !n.noWavesLinked) ||
            (linkFilter === 'Has DB' && n.dbLinked) ||
            (linkFilter === 'Has Security' && n.securityLinked)) &&
          (!effectivePerson || matchesPerson(n, effectivePerson)) &&
          (!debouncedSearch || matchesText(n, debouncedSearch)),
      ),
    [scoped, currentStateFilter, migrationFilter, slaFilter, classFilter, linkFilter, effectivePerson, debouncedSearch],
  )

  const openManage = (n: Nomination) => {
    setEditing(n)
    setForm({
      status: n.status,
      migrationStatus: n.migrationStatus ?? '',
      blockedReason: n.blockedReason ?? suggestBlocker(n.remarks ?? n.currentState),
      blockedSince: n.blockedSince,
      followUpDate: n.followUpDate,
      remarks: n.remarks ?? '',
      projectCoordinator: n.projectCoordinator ?? '',
      cftlPrimary: n.cftlPrimary ?? '',
      classification: n.classification ?? 'Standard Factory',
      velocityImpact: n.velocityImpact ?? '',
      ghcpAdoptionLevel: n.ghcpAdoptionLevel ?? 0,
      shortName: n.shortName ?? '',
    })
    setWaveType('App')
    setWaveRef('')
    // Seed the resource picker with the nomination's current SA so the dropdown reflects it;
    // pre-select the matching resource when the SA name maps to one in the resource list.
    const currentSa = (n.solutionArchitect ?? '').trim()
    const saMatch = currentSa
      ? (resourceList ?? []).find((r) => r.name.trim().toLowerCase() === currentSa.toLowerCase())
      : undefined
    setAssignResourceId(saMatch?.resourceId ?? null)
    setAssignRole('Solution Architect')
    setResourceQuery(currentSa)
    setResourceTyping(false)
  }

  const isBlocked = BLOCKED_STATES.includes(form.status)

  const saveManage = async () => {
    if (!editing) return
    setBusy(true)
    try {
      await api.updateNomination(editing.id, {
        ...form,
        blockedReason: isBlocked ? form.blockedReason : undefined,
        blockedSince: isBlocked ? form.blockedSince : undefined,
      })
      await reload()
      setEditing(null)
    } finally {
      setBusy(false)
    }
  }

  const addWave = async () => {
    if (!editing || !waveRef.trim()) return
    setBusy(true)
    try {
      await api.addWave(editing.id, { waveType, reference: waveRef.trim() })
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
      setWaveRef('')
    } finally {
      setBusy(false)
    }
  }

  const removeWave = async (waveId: number) => {
    if (!editing) return
    setBusy(true)
    try {
      await api.deleteWave(editing.id, waveId)
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
    } finally {
      setBusy(false)
    }
  }

  const assignResource = async () => {
    if (!editing || !assignResourceId) return
    setBusy(true)
    try {
      await api.assignNominationResource(editing.id, assignResourceId, assignRole)
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
      setAssignResourceId(null)
      setResourceQuery('')
      setResourceTyping(false)
    } finally {
      setBusy(false)
    }
  }

  const unassignResource = async (resourceId: number) => {
    if (!editing) return
    setBusy(true)
    try {
      await api.unassignNominationResource(editing.id, resourceId)
      const fresh = await api.nominations(region)
      await reload()
      setEditing(fresh.find((n) => n.id === editing.id) ?? editing)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Nomination Pipeline
      </Text>

      {person && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 12px', borderRadius: 6, background: 'var(--colorBrandBackground2)', border: '1px solid var(--colorBrandStroke2)', alignSelf: 'flex-start' }}>
          <Text size={300}>Filtered to <strong>{person}</strong> (PM / CFTL / SA / assigned)</Text>
          <Button size="small" appearance="subtle" icon={<DismissRegular />} onClick={() => navigate('/nominations')}>Clear</Button>
        </div>
      )}

      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <SummaryCard
              title={`Approval status · ${data?.length ?? 0} total`}
              items={[
                { label: 'Approved', value: approvalCount('Approved'), tone: 'success', active: approvalFilter === 'Approved', onClick: () => toggleApproval('Approved') },
                { label: 'Prov. Approved', value: approvalCount('Provisionally Approved'), tone: 'brand', active: approvalFilter === 'Provisionally Approved', onClick: () => toggleApproval('Provisionally Approved') },
                { label: 'Active Concierge', value: approvalCount('Active Concierge'), tone: 'neutral', active: approvalFilter === 'Active Concierge', onClick: () => toggleApproval('Active Concierge') },
                { label: 'Declined', value: approvalCount('Declined'), tone: 'danger', active: approvalFilter === 'Declined', onClick: () => toggleApproval('Declined') },
              ]}
            />
            <SummaryCard
              title="Migration stage"
              items={STAGES.map((s) => ({ label: `${s.n} · ${s.label}`, value: stageCount(s.match), tone: s.tone }))}
            />
            <SummaryCard
              title="Health"
              items={[
                { label: 'Blocked / Waiting', value: blockedTotal, tone: 'warning' },
                { label: 'Warn (3d+)', value: staleCount('Warn'), tone: 'warning', active: slaFilter === 'Warn', onClick: () => setSlaFilter(slaFilter === 'Warn' ? '' : 'Warn') },
                { label: 'Escalate (5d+)', value: staleCount('Escalate'), tone: 'warning', active: slaFilter === 'Escalate', onClick: () => setSlaFilter(slaFilter === 'Escalate' ? '' : 'Escalate') },
                { label: 'Defer (10d+)', value: staleCount('Defer'), tone: 'danger', active: slaFilter === 'Defer', onClick: () => setSlaFilter(slaFilter === 'Defer' ? '' : 'Defer') },
              ]}
            />
            <SummaryCard
              title={`Linkage · ${scoped.length} shown`}
              items={[
                { label: 'No waves', value: noWaveTotal, tone: 'warning', active: linkFilter === 'No waves', onClick: () => setLinkFilter(linkFilter === 'No waves' ? '' : 'No waves') },
                { label: 'Has waves', value: scoped.length - noWaveTotal, tone: 'success', active: linkFilter === 'Has any waves', onClick: () => setLinkFilter(linkFilter === 'Has any waves' ? '' : 'Has any waves') },
              ]}
            />
          </div>

          <Panel
            title="Nominations"
            action={
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'flex-end' }}>
                <SearchBox
                  placeholder="Search account, PM, CFTL, SA, TPID"
                  value={search}
                  onChange={(_, d) => setSearch(d.value)}
                  style={{ minWidth: 200 }}
                />
                {isSa && (
                  <Switch checked={mine} onChange={(_, d) => setMine(d.checked)} label="My nominations" />
                )}
                <FilterSelect label="Approval status" value={approvalFilter} options={approvalOptions} onChange={setApprovalFilter} minWidth={160} />
                <FilterSelect label="Stage" value={migrationFilter} options={migrationOptions} onChange={setMigrationFilter} minWidth={200} />
                <FilterSelect label="Status" value={currentStateFilter} options={currentStateOptions} onChange={setCurrentStateFilter} minWidth={200} />
                <FilterSelect label="SLA breach" value={slaFilter} options={['Warn', 'Escalate', 'Defer']} onChange={setSlaFilter} minWidth={150} />
                <FilterSelect label="Classification" value={classFilter} options={['Strategic (all)', ...(vocab?.classifications ?? [])]} onChange={setClassFilter} minWidth={170} />
                <FilterSelect label="Links" value={linkFilter} options={LINK_FILTERS} onChange={setLinkFilter} minWidth={150} />
                <input
                  ref={fileInput}
                  type="file"
                  accept=".xlsx"
                  style={{ display: 'none' }}
                  onChange={runUpload}
                />
                <Tooltip
                  relationship="description"
                  content="Upload any of the 3 workbooks — Detail View (pipeline), Summary of All Offerings (enrichment), or Nominations In-Flight (account master). The type is auto-detected."
                >
                  <Button
                    appearance="primary"
                    icon={<ArrowUploadRegular />}
                    disabled={uploading}
                    onClick={() => fileInput.current?.click()}
                  >
                    {uploading ? 'Uploading…' : 'Upload'}
                  </Button>
                </Tooltip>
                <Button
                  as="a"
                  href={api.exportUrl('nominations', region, {
                    approval: approvalFilter,
                    migrationStatus: migrationFilter,
                    currentState: currentStateFilter,
                    sla: slaFilter,
                    links: linkFilter,
                    search: debouncedSearch,
                  })}
                  appearance="secondary"
                  icon={<ArrowDownloadRegular />}
                >
                  Export
                </Button>
              </div>
            }
          >
            <DataTable<Nomination>
              ariaLabel="Nominations"
              rows={rows}
              rowKey={(n) => n.id}
              defaultSort={{ key: 'opened', dir: 'desc' }}
              emptyMessage="No nominations match your filters."
              columns={[
                { key: 'account', header: 'Account', sortValue: (n) => n.accountName ?? '', render: (n) => <RouterLink to={`/nominations/${n.id}`}>{n.accountName ?? '\u2014'}</RouterLink> },
                { key: 'tpid', header: 'TPID', sortValue: (n) => n.tpid ?? '', render: (n) => n.tpid ?? '—' },
                { key: 'offering', header: 'Offering', sortValue: (n) => n.technology ?? '', render: (n) => n.technology ? <Tooltip relationship="description" content={n.technology}><span>{offeringShort(n.technology)}</span></Tooltip> : '—' },
                { key: 'region', header: 'Region', sortValue: (n) => n.region },
                {
                  key: 'stage',
                  header: 'Stage',
                  align: 'center',
                  sortValue: (n) => migrationStage(n.migrationStatus)?.n ?? 0,
                  render: (n) => {
                    const s = migrationStage(n.migrationStatus)
                    if (!s) return <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                    return (
                      <Tooltip relationship="description" content={`Stage ${s.n}: ${s.label} — ${n.migrationStatus}`}>
                        <Badge appearance="tint" color="brand" size="medium">
                          {s.n}
                        </Badge>
                      </Tooltip>
                    )
                  },
                },
                {
                  key: 'state',
                  header: 'Status',
                  sortValue: (n) => n.currentState ?? '',
                  render: (n) =>
                    n.currentState ? (
                      <Badge appearance="tint" color={stateColor(n.currentState)} size="small">
                        {n.currentState}
                      </Badge>
                    ) : (
                      <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                    ),
                },
                {
                  key: 'summary',
                  header: 'Summary',
                  sortValue: (n) => n.staleTier || '',
                  render: (n) => {
                    const tone = staleTone[n.staleTier]
                    const details = (n.remarks ?? '').replace(/\s+/g, ' ').trim()
                    const age = n.stageAgeDays ?? n.daysSinceUpdate
                    const paused = age - n.effectiveAgeDays
                    const action = recommendedAction(n.staleTier, age)
                    return (
                      <Tooltip
                        relationship="description"
                        content={
                          <div style={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 320 }}>
                            {n.currentState && <div><b>State:</b> {n.currentState}</div>}
                            {n.blockedReason && <div><b>Blocker:</b> {n.blockedReason}</div>}
                            {n.blockedSince && <div><b>Blocked since:</b> {n.blockedSince}</div>}
                            {n.followUpDate && <div><b>Follow-up:</b> {n.followUpDate}</div>}
                            <div><b>Age in stage:</b> {age} days{n.staleTier ? ` · SLA ${n.staleTier}` : ''}</div>
                            {paused > 0 && <div><b>SLA clock:</b> {n.clockStopped ? 'stopped' : 'ran'} · {paused}d not counted · {n.effectiveAgeDays}d effective</div>}
                            {n.openBlockerCount > 0 && <div><b>Open blockers:</b> {n.openBlockerCount}</div>}
                            {action && <div style={{ color: 'var(--colorPaletteRedForeground1)' }}><b>Next step:</b> {action}</div>}
                            {n.waves.length > 0 && <div><b>Waves:</b> {n.waves.map((w) => w.waveType).join(', ')}</div>}
                            {details && <div><b>Details:</b> {details}</div>}
                          </div>
                        }
                      >
                        <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center', cursor: 'help', flexWrap: 'wrap' }}>
                          {n.clockStopped && (
                            <Badge appearance="tint" color="warning" size="small">Paused</Badge>
                          )}
                          {tone && (
                            <span style={{ background: tone.bg, color: tone.color, borderRadius: 4, padding: '1px 6px', fontSize: 11, fontWeight: 600 }}>
                              SLA {tone.label}
                            </span>
                          )}
                          {n.blockedReason && (
                            <Badge appearance="tint" color="danger" size="small">
                              {n.blockedReason}
                            </Badge>
                          )}
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
                  key: 'links',
                  header: 'Waves',
                  sortValue: (n) => n.waveCount,
                  render: (n) => (
                    <Tooltip
                      relationship="description"
                      content={
                        n.noWavesLinked
                          ? 'No waves linked — review and associate any related waves.'
                          : `Linked waves: ${presentWaves(n).join(', ')}`
                      }
                    >
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
                  render: (n) =>
                    n.assignedResourceCount === 0 ? (
                      <span style={{ color: 'var(--colorNeutralForeground3)' }}>—</span>
                    ) : (
                      <Tooltip
                        relationship="description"
                        content={
                          <div style={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 280 }}>
                            {n.assignedResources.map((a) => (
                              <div key={a.resourceId}>
                                <b>{a.role ?? 'Role TBD'}:</b> {a.name}
                              </div>
                            ))}
                          </div>
                        }
                      >
                        <Badge appearance="tint" color="brand" size="small" style={{ cursor: 'help' }}>
                          {n.assignedResourceCount}
                        </Badge>
                      </Tooltip>
                    ),
                },
                {
                  key: 'actions',
                  header: '',
                  align: 'center',
                  render: (n) => (
                    <Tooltip relationship="label" content="Manage">
                      <Button appearance="subtle" size="small" icon={<EditRegular />} aria-label="Manage" onClick={() => openManage(n)} />
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
        onClose={() => {
          const ok = uploadResult?.success
          setUploadResult(null)
          if (ok) window.location.reload()
        }}
        onSubmit={() => {
          const ok = uploadResult?.success
          setUploadResult(null)
          if (ok) window.location.reload()
        }}
        submitLabel={uploadResult?.success ? 'Reload' : 'Close'}
        maxWidth={620}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {(uploadResult?.messages ?? []).map((m, i) => (
            <div key={i} style={{ fontSize: 13, fontWeight: i === 0 ? 600 : 400 }}>{m}</div>
          ))}
        </div>
      </Modal>

      <Modal
        open={!!editing}
        title={editing ? `Manage — ${editing.accountName ?? 'Nomination'}` : ''}
        onClose={() => setEditing(null)}
        onSubmit={saveManage}
        submitLabel="Save"
        busy={busy}
        maxWidth={640}
      >
        {editing && (
          <div
            style={{
              background: 'var(--colorNeutralBackground2)',
              border: '1px solid var(--colorNeutralStroke2)',
              borderRadius: 6,
              padding: 12,
              display: 'flex',
              flexDirection: 'column',
              gap: 8,
            }}
          >
            <Text size={200} weight="semibold" style={{ color: 'var(--colorNeutralForeground3)' }}>
              From FDO · refreshed on import (read-only)
            </Text>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 16px' }}>
              <ReadField label="Current State">
                {editing.currentState ? (
                  <Badge appearance="tint" color={stateColor(editing.currentState)}>
                    {editing.currentState}
                  </Badge>
                ) : (
                  '—'
                )}
              </ReadField>
              <ReadField label="Age in stage">
                {editing.stageAgeDays != null ? `${editing.stageAgeDays}d` : '—'}
              </ReadField>
              <ReadField label="Offering">{editing.technology ?? '—'}</ReadField>
              <ReadField label="TPID · Region">{`${editing.tpid ?? '—'} · ${editing.region ?? '—'}`}</ReadField>
            </div>
          </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Stage (Migration Status)">
            <Dropdown
              style={{ width: '100%', minWidth: 0 }}
              value={
                migrationStage(form.migrationStatus)
                  ? STAGE_OPTIONS[migrationStage(form.migrationStatus)!.n - 1].label
                  : form.migrationStatus ?? ''
              }
              selectedOptions={
                migrationStage(form.migrationStatus) ? [STAGE_OPTIONS[migrationStage(form.migrationStatus)!.n - 1].value] : []
              }
              placeholder="Select stage"
              onOptionSelect={(_, d) => setForm((f) => ({ ...f, migrationStatus: d.optionValue ?? f.migrationStatus }))}
            >
              {STAGE_OPTIONS.map((o) => (
                <Option key={o.value} value={o.value}>
                  {o.label}
                </Option>
              ))}
            </Dropdown>
          </Field>
          <Field label="Status">
            <Dropdown
              style={{ width: '100%', minWidth: 0 }}
              value={form.status}
              selectedOptions={[form.status]}
              onOptionSelect={(_, d) => setForm((f) => ({ ...f, status: d.optionValue ?? f.status }))}
            >
              {STATUS_OPTIONS.map((s) => (
                <Option key={s} value={s}>
                  {s}
                </Option>
              ))}
            </Dropdown>
          </Field>
        </div>

        {isBlocked && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <Field label="Blocker reason">
              <Dropdown
                style={{ width: '100%', minWidth: 0 }}
                value={form.blockedReason ?? ''}
                selectedOptions={form.blockedReason ? [form.blockedReason] : []}
                placeholder="Select reason"
                onOptionSelect={(_, d) => setForm((f) => ({ ...f, blockedReason: d.optionValue }))}
              >
                {BLOCKER_OPTIONS.map((b) => (
                  <Option key={b} value={b}>
                    {b}
                  </Option>
                ))}
              </Dropdown>
            </Field>
            <Field label="Blocked since">
              <Input
                type="date"
                style={{ width: '100%' }}
                value={form.blockedSince ?? ''}
                onChange={(_, d) => setForm((f) => ({ ...f, blockedSince: d.value || undefined }))}
              />
            </Field>
          </div>
        )}

        <Field label="Follow-up date">
          <Input
            type="date"
            style={{ width: '100%' }}
            value={form.followUpDate ?? ''}
            onChange={(_, d) => setForm((f) => ({ ...f, followUpDate: d.value || undefined }))}
          />
        </Field>

        <Field label="Remarks">
          <Textarea
            style={{ width: '100%' }}
            value={form.remarks ?? ''}
            onChange={(_, d) => setForm((f) => ({ ...f, remarks: d.value }))}
            rows={2}
          />
        </Field>

        <Field label="App / short name">
          <Input style={{ width: '100%' }} value={form.shortName ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, shortName: d.value }))} placeholder="app or system name (shown as Account · Name)" />
        </Field>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="PM">
            <Input style={{ width: '100%' }} value={form.projectCoordinator ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, projectCoordinator: d.value }))} />
          </Field>
          <Field label="CFTL">
            <Input style={{ width: '100%' }} value={form.cftlPrimary ?? ''} onChange={(_, d) => setForm((f) => ({ ...f, cftlPrimary: d.value }))} />
          </Field>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Classification">
            <Dropdown
              style={{ width: '100%' }}
              value={form.classification ?? 'Standard Factory'}
              selectedOptions={[form.classification ?? 'Standard Factory']}
              onOptionSelect={(_, d) => setForm((f) => ({ ...f, classification: d.optionValue ?? 'Standard Factory' }))}
            >
              {(vocab?.classifications ?? ['Standard Factory']).map((c) => <Option key={c} value={c}>{c}</Option>)}
            </Dropdown>
          </Field>
          <Field label="Velocity impact">
            <Dropdown
              style={{ width: '100%' }}
              placeholder="—"
              value={form.velocityImpact ?? ''}
              selectedOptions={form.velocityImpact ? [form.velocityImpact] : []}
              onOptionSelect={(_, d) => setForm((f) => ({ ...f, velocityImpact: d.optionValue ?? '' }))}
            >
              <Option value="">—</Option>
              {(vocab?.velocityImpacts ?? []).map((v) => <Option key={v} value={v}>{v}</Option>)}
            </Dropdown>
          </Field>
        </div>

        <Field label="GHCP adoption level">
          <Dropdown
            style={{ width: '100%' }}
            value={ADOPTION_LEVELS[form.ghcpAdoptionLevel ?? 0].label}
            selectedOptions={[String(form.ghcpAdoptionLevel ?? 0)]}
            onOptionSelect={(_, d) => setForm((f) => ({ ...f, ghcpAdoptionLevel: Number(d.optionValue ?? 0) }))}
          >
            {ADOPTION_LEVELS.map((a) => <Option key={a.level} value={String(a.level)}>{`${a.level} · ${a.label}`}</Option>)}
          </Dropdown>
        </Field>

        <div style={{ borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 10 }}>
          <Text weight="semibold">Assigned resources</Text>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', margin: '2px 0 6px' }}>
            Staff this migration with a delivery role. The Solution Architect assigned here drives the SA column. Operational only — this does not affect account capacity.
          </Text>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: '8px 0' }}>
            {(editing?.assignedResources ?? []).length === 0 && (
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                No resources assigned yet.
              </Text>
            )}
            {(editing?.assignedResources ?? []).map((a) => (
              <div key={a.resourceId} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <Badge appearance="outline" color="brand" size="small">
                  {a.role ?? 'Role TBD'}
                </Badge>
                <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {a.name}
                  {a.region ? ` · ${a.region}` : ''}
                </span>
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<DeleteRegular />}
                  onClick={() => unassignResource(a.resourceId)}
                  disabled={busy}
                />
              </div>
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
            <Combobox
              placeholder="Search resource…"
              value={resourceQuery}
              selectedOptions={assignResourceId ? [String(assignResourceId)] : []}
              onChange={(e) => {
                setResourceQuery(e.target.value)
                setResourceTyping(true)
                setAssignResourceId(null)
              }}
              onOptionSelect={(_, d) => {
                const id = Number(d.optionValue)
                setAssignResourceId(id)
                const r = (resourceList ?? []).find((x) => x.resourceId === id)
                setResourceQuery(r ? r.name : '')
                setResourceTyping(false)
              }}
              style={{ flex: 1, minWidth: 180 }}
            >
              {(resourceList ?? [])
                .filter((r) => eligibleForRole(assignRole, r.role))
                .filter((r) => !resourceTyping || !resourceQuery || r.name.toLowerCase().includes(resourceQuery.toLowerCase()))
                .slice(0, 50)
                .map((r) => (
                  <Option key={r.resourceId} value={String(r.resourceId)} text={r.name}>
                    {r.name} · {r.role}
                  </Option>
                ))}
            </Combobox>
            <Dropdown
              value={assignRole}
              selectedOptions={[assignRole]}
              onOptionSelect={(_, d) => {
                const next = d.optionValue ?? assignRole
                setAssignRole(next)
                // Clear a selection that isn't eligible for the newly chosen role, and reset the search.
                const sel = (resourceList ?? []).find((x) => x.resourceId === assignResourceId)
                if (sel && !eligibleForRole(next, sel.role)) {
                  setAssignResourceId(null)
                  setResourceQuery('')
                }
                setResourceTyping(false)
              }}
              style={{ minWidth: 170 }}
            >
              {ASSIGN_ROLES.map((r) => (
                <Option key={r} value={r}>
                  {r}
                </Option>
              ))}
            </Dropdown>
            <Button
              appearance="secondary"
              icon={<AddRegular />}
              onClick={assignResource}
              disabled={busy || !assignResourceId}
            >
              Assign
            </Button>
          </div>
        </div>

        <div style={{ borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 10 }}>
          <Text weight="semibold">Related waves</Text>
          {editing && (
            <div style={{ display: 'flex', gap: 6, alignItems: 'center', margin: '6px 0' }}>
              <LinkChip label="DB" linked={editing.dbLinked} />
              <LinkChip label="ALZ" linked={editing.alzLinked} />
              <LinkChip label="Sec" linked={editing.securityLinked} />
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                {editing.noWavesLinked ? 'No waves linked yet — add any associated waves below.' : `Linked: ${presentWaves(editing).join(', ')}`}
              </Text>
            </div>
          )}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: '8px 0' }}>
            {(editing?.waves ?? []).length === 0 && (
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                No linked waves yet.
              </Text>
            )}
            {(editing?.waves ?? []).map((w) => (
              <div key={w.id} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <Badge appearance="outline" size="small">
                  {w.waveType}
                </Badge>
                <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{w.reference}</span>
                <Button appearance="subtle" size="small" icon={<DeleteRegular />} onClick={() => removeWave(w.id)} disabled={busy} />
              </div>
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
            <Dropdown
              value={waveType}
              selectedOptions={[waveType]}
              onOptionSelect={(_, d) => setWaveType(d.optionValue ?? waveType)}
              style={{ minWidth: 140 }}
            >
              {WAVE_OPTIONS.map((w) => (
                <Option key={w} value={w}>
                  {w}
                </Option>
              ))}
            </Dropdown>
            <Input
              placeholder="Wave reference / ID"
              value={waveRef}
              onChange={(_, d) => setWaveRef(d.value)}
              style={{ flex: 1 }}
            />
            <Button appearance="secondary" icon={<AddRegular />} onClick={addWave} disabled={busy || !waveRef.trim()}>
              Link
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
