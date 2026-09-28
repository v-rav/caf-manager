import { Accordion, AccordionHeader, AccordionItem, AccordionPanel, Link, Text } from '@fluentui/react-components'
import type { CSSProperties } from 'react'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { AcrRates } from '../types'

const money = (n: number) => `$${Math.round(n).toLocaleString()}`

export function HelpPage() {
  const { data: rates, loading, error, reload } = useAsync(() => api.acrRates(), [])

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 900 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>Help &amp; FAQ</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>What the portal does, how to use each page, and how the key numbers are computed. Rates are configurable (Admin → Configuration).</Text>
      </div>

      <UsageGuide />

      <Panel title="Frequently asked questions">
        <Accordion collapsible multiple>
          <AccordionItem value="acr">
            <AccordionHeader>How is ACR (Annual Consumption Revenue) calculated?</AccordionHeader>
            <AccordionPanel>
              {loading && !rates ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : rates ? (
                <AcrAnswer rates={rates} />
              ) : null}
            </AccordionPanel>
          </AccordionItem>
        </Accordion>
      </Panel>

      <ProposalsSection />
    </div>
  )
}

const CONTAINER_ACR_LINK = 'https://outlook.office.com/mail/MBX%3A7bb996a6-f854-4e67-a63b-0007862f5fc6%4072f988bf-86f1-41af-91ab-2d7cd011db47/applink/read/AAkALgAAAAAAHYQDEapmEc2byACqAC-EWg0A2hw4T10NJ06u6N-1HxGV3AABpsuOugAA/'

type GuidePage = { name: string; body: string }
type GuideGroup = { group: string; intro: string; pages: GuidePage[] }

const GUIDE_GROUPS: GuideGroup[] = [
  {
    group: 'Overview',
    intro: 'Leadership-level rollups and this help.',
    pages: [
      { name: 'Executive Dashboard', body: 'Four tabbed views: Leadership (totals, ACR influenced, average MSI + band rollup, adoption rate), Operational (by-stage counts, clock-aware SLA on Standard work, open blockers, average effective age, SA load), GHCP Adoption (embedded), and Factory Productivity (completed migrations, ACR realized, cores, tool/automation adoption). KPI cards drill through to the underlying grid. Honors the Region and Fiscal-Year selectors in the header.' },
      { name: 'Migration Analytics', body: 'Headline KPIs plus distribution and value cuts (ACR, cores, adoption) over the Approved pipeline, and a fiscal-year-aware Trends time-series (configurable basis / granularity / measure / split-by). One page-level Excel export (top-right) writes a 4-sheet workbook matching the on-screen filters.' },
      { name: 'Help & FAQ', body: 'This page — system usage guide, how ACR is calculated (from the live rate master), and the containerized-app counting proposal.' },
    ],
  },
  {
    group: 'Delivery',
    intro: 'The migration pipeline — track, govern, and drive nominations to completion.',
    pages: [
      { name: 'Nominations', body: 'The pipeline grid: Account, TPID, Offering, Region, Stage (1–4), Status (on track / blocked), Summary (SLA tier + blocker + days-in-stage), PM / CFTL / SA owners, Waves, Team. Defaults to Approved nominations; filter by Approval, Stage, State, SLA breach, Wave links, or search. Click an account to open its SA Workspace. Export is filter-aware.' },
      { name: 'SA Workspace', body: 'Per-nomination cockpit at /nominations/{id}: an 8-gate stepper, the current gate checklist (tick items, mark N/A, advance only when the gate is green), the Migration Capability panel (capture which tool accelerated which activity), and a rail of Blockers, Milestones, and an audit Timeline. Header shows Readiness % and MSI.' },
      { name: 'Governance Board', body: 'All open blockers across the pipeline: KPIs, by-category breakdown, an aging table, and resolve actions. Blockers can be clock-stopping — a live clock-stop freezes that nomination’s SLA.' },
      { name: 'Migration Flow', body: 'Funnel conversion and drop-off across the journey, plus bottleneck analytics: in-flight by stage × age, blockers by category, SA workload, and volume by migration type.' },
      { name: 'Strategic Register', body: 'Strategic-classified nominations: time-threshold tier KPIs (Green/Amber/Red/Exec), share of active pipeline, breakdown by classification, and an aging table.' },
      { name: 'GHCP Adoption', body: 'Copilot adoption: licensed vs awaiting, used %, tool-attached %, the 0–7 adoption-level distribution, and the Migration Capability rollup (tool adoption, by activity, most-used tool per activity, GHCP-accelerated KPI). CSV export.' },
    ],
  },
  {
    group: 'Resourcing',
    intro: 'People, accounts, and capacity.',
    pages: [
      { name: 'Resource Hub', body: 'Roster of delivery resources with skills, region, and their nomination-derived account load.' },
      { name: 'Account Hub', body: 'The account master (TPID-keyed customers) with ownership, segment, status, and TPID filters. Master import (Nominations In-Flight.xlsx), de-duplication, and reversible parking of no-TPID rows live here.' },
      { name: 'Capacity', body: 'The capacity cockpit: 1 resource = 5 active accounts, utilization heat-bands, a Headroom column, Available-capacity and Bench KPIs, a per-row leave-clash flag, and drill-through from a resource to their nominations. Account count is nomination-derived. Export includes the assigned-account list.' },
      { name: 'Leave', body: 'Leave calendar feeding the capacity leave-clash signal.' },
      { name: 'Reconciliation', body: 'Cross-check imported data against portal state to surface mismatches.' },
      { name: 'Performance', body: 'Performance-review visibility per resource.' },
    ],
  },
  {
    group: 'Admin',
    intro: 'Data management, configuration, and platform controls (Admin-only unless noted).',
    pages: [
      { name: 'Import History', body: 'Every upload/refresh is recorded as an ImportRun with field-level change deltas (Added / Updated from→to / Withdrawn). Rows are pruned after the retention window.' },
      { name: 'Configuration', body: 'Editable lookup master (blocker categories/owners, classification, velocity, milestone types, tool/skill/segment), ACR calculation rates + live estimator, and operations settings (stale cadence, per-stage targets).' },
      { name: 'Gate Template', body: 'Add / rename / reweight / reorder / activate / delete the 8 governance gates and their checklist items. Delete is blocked once a nomination has captured progress — deactivate instead.' },
      { name: 'Capability Masters', body: 'Manage the Migration Tool and Activity masters and the per-tool supported-activity mapping (which activities a tool can accelerate). Delete is blocked once usage exists — deactivate instead.' },
      { name: 'User & Access', body: 'Create / disable / reset users, provision SA logins in bulk, and set the role-based page-access matrix. Admin bypasses all page gates.' },
      { name: 'Backup & Restore', body: 'Download a consistent zipped SQLite snapshot anytime, or restore one (replaces the live DB, keeps a safety copy). Admin-only.' },
    ],
  },
]

type GuideConcept = { term: string; body: string }
const GUIDE_CONCEPTS: GuideConcept[] = [
  { term: 'Stage vs Status', body: 'Stage (1–4) is which phase of the migration journey a nomination is in — Validating, Executing Pre-Requisites, Finalize Scope, Executing Migration. Status is whether that work is on track or blocked. They are tracked separately.' },
  { term: 'MSI (Migration Success Index)', body: '0–100 composite: 30% readiness (gates 1–3) + 20% scope (4–5) + 20% delivery (6–7) + 10% risk (blockers) + 10% GHCP adoption + 10% sign-off (gate 8). Bands: Green > 80, Amber 60–80, Red < 60.' },
  { term: 'SLA & the clock', body: 'Stale tiers (Warn / Escalate / Defer) apply to execution stages 2–4 based on days in the current stage. Clock-stopping blockers subtract their open window, so a live clock-stop freezes the SLA — the grid shows a Paused chip.' },
  { term: 'Waves', body: 'A nomination may link to Security / DB / Landing-Zone / Dispatch waves. Wave links are informational, not required — no wave type is mandatory. FDO links refresh each drop; portal-added links are preserved.' },
  { term: 'ACR', body: 'Estimated annual Azure consumption a workload will drive once migrated, derived from cores-per-app and rate-per-core thumb-rules (see the FAQ below). Rates are admin-editable.' },
  { term: 'Migration Capability usage', body: 'Which tool (e.g. Azure Migrate, GHCP, AppMod) actually accelerated which activity, captured per nomination in the SA Workspace and rolled up on GHCP Adoption — the value story behind the pipeline.' },
]

type GuideStep = { title: string; body: string }
const GUIDE_WORKFLOWS: GuideStep[] = [
  { title: 'Drive a nomination to completion', body: 'Open it from Nominations → work the current gate’s checklist in the SA Workspace → Advance when the gate is green → repeat through gate 8. Capture blockers, milestones, and tool usage as you go.' },
  { title: 'Raise & clear a blocker', body: 'In the SA Workspace rail, raise a blocker (category, owner, ETA, clock-stopped?). It appears on the Governance Board; resolve it there or in the workspace. Clock-stopped blockers pause the SLA.' },
  { title: 'Ingest a new FDO drop', body: 'Refresh Data (header) upserts by FDO Task Id — FDO-owned fields refresh, portal-owned fields (status, blockers, waves, edited remarks) are preserved, absent rows are soft-withdrawn (never hard-deleted). Review the deltas on Import History.' },
  { title: 'Export what you see', body: 'Grid and analytics exports are filter-aware — the file matches the on-screen filters, region, and fiscal year. Use the export button on the page you’re viewing.' },
  { title: 'Back up before a risky change', body: 'Admin → Backup & Restore → Download a snapshot. Restore replaces the live DB and keeps a server-side safety copy.' },
]

function UsageGuide() {
  return (
    <Panel title="System usage guide">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 820 }}>
        <Text size={300}>
          The <b>CAF Operations Portal</b> gives EMEA governance a single view of the migration (nomination) pipeline,
          resource capacity, and delivery health. Data originates from Excel/FDO imports; after import the <b>portal is
          the system of record</b>. What you can see and edit depends on your role (<b>Admin</b>, <b>Lead</b>, <b>SA</b>);
          Admin sees everything.
        </Text>

        <Accordion collapsible multiple>
          <AccordionItem value="pages">
            <AccordionHeader>Pages by area — what each one is for</AccordionHeader>
            <AccordionPanel>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                {GUIDE_GROUPS.map((g) => (
                  <div key={g.group}>
                    <Text size={400} weight="semibold" style={{ display: 'block' }}>{g.group}</Text>
                    <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 6 }}>{g.intro}</Text>
                    <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 5 }}>
                      {g.pages.map((p) => (
                        <li key={p.name}><Text size={300}><b>{p.name}</b> — {p.body}</Text></li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="concepts">
            <AccordionHeader>Key concepts</AccordionHeader>
            <AccordionPanel>
              <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 6 }}>
                {GUIDE_CONCEPTS.map((c) => (
                  <li key={c.term}><Text size={300}><b>{c.term}</b> — {c.body}</Text></li>
                ))}
              </ul>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="workflows">
            <AccordionHeader>Common workflows</AccordionHeader>
            <AccordionPanel>
              <ol style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 6 }}>
                {GUIDE_WORKFLOWS.map((w) => (
                  <li key={w.title}><Text size={300}><b>{w.title}</b> — {w.body}</Text></li>
                ))}
              </ol>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="scope">
            <AccordionHeader>Region &amp; Fiscal-Year scope</AccordionHeader>
            <AccordionPanel>
              <Text size={300}>
                The header <b>Region</b> (Global / EMEA / ASIA) and <b>Fiscal Year</b> (FY starts Jul 1, labelled by end
                year; default current FY) selectors scope nomination-centric pages. A nomination belongs to one FY — settled
                items by their close date, in-flight items by when they entered. Choose <b>All FY</b> to see everything.
                Analytics keeps its own dedicated Trends controls.
              </Text>
            </AccordionPanel>
          </AccordionItem>
        </Accordion>
      </div>
    </Panel>
  )
}

function ProposalsSection() {
  const th: CSSProperties = { textAlign: 'left', padding: '6px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }
  const td: CSSProperties = { padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }
  const examples: [string, string][] = [
    ['1 app → 1 container', '1 app'],
    ['1 app → 5 independently deployable containers', '5 apps'],
    ['10 apps → 10 containers', '10 apps'],
    ['20 apps → 35 containers', '35 apps'],
  ]

  return (
    <Panel title="Proposals">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 760 }}>
        <Text size={500} weight="bold">Counting containerized applications (for ACR)</Text>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Principle</Text>
          <ul style={{ margin: 0, paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 4 }}>
            <li><Text size={300}><b>1 Containerized Application = 1 Application</b></Text></li>
            <li><Text size={300}>If an application is split into multiple <b>independently deployable containers</b>, each container should be counted as a separate application.</Text></li>
            <li><Text size={300}>FDO application count should be updated to reflect the actual number of <b>containerized applications delivered</b>.</Text></li>
          </ul>
        </div>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Examples</Text>
          <table style={{ borderCollapse: 'collapse', width: '100%' }}>
            <thead><tr><th style={th}>Scenario</th><th style={th}>Application Count</th></tr></thead>
            <tbody>
              {examples.map(([s, c]) => (
                <tr key={s}><td style={td}>{s}</td><td style={td}>{c}</td></tr>
              ))}
            </tbody>
          </table>
        </div>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Summary</Text>
          <div style={{ borderLeft: '3px solid var(--colorBrandStroke1)', paddingLeft: 12, color: 'var(--colorNeutralForeground2)' }}>
            <Text size={300}>
              For containerization engagements, each independently deployable containerized application should be counted as
              <b> 1 application</b>. If a monolithic application is broken into multiple deployable containers, the application
              count should be updated accordingly.
            </Text>
          </div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 8 }}>
            Aligns with the direction discussed with Pradeep Mamidi and Anuj. Source:{' '}
            <Link href={CONTAINER_ACR_LINK} target="_blank" rel="noopener noreferrer">ACR calculation for AKS (email thread)</Link>.
          </Text>
        </div>
      </div>
    </Panel>
  )
}

function AcrAnswer({ rates }: { rates: AcrRates }) {
  const months = rates.annualizationMonths || 12
  // Worked example: 10 apps on each target.
  const apps = 10
  const appSvc = apps * rates.appServiceCoresPerApp * rates.appServiceArpuPerCoreMonth * months
  const aksLinux = apps * rates.aksCoresPerApp * rates.aksLinuxArpuPerCoreMonth * months
  const aksWin = apps * rates.aksCoresPerApp * rates.aksWindowsArpuPerCoreMonth * months
  const acaCores = apps * rates.aksCoresPerApp
  const aca = rates.acaArpuPerCoreHour * acaCores * rates.acaUtilization * rates.acaHoursPerMonth * months

  const th: CSSProperties = { textAlign: 'left', padding: '6px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }
  const td: CSSProperties = { padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
      <Text size={300}>
        <b>ACR</b> estimates the annual Azure consumption a workload will drive once migrated. The portal derives it from
        two App Factory thumb-rules — <b>how many cores an app needs</b> and <b>the $ rate per core</b> — then annualizes.
        You supply either an <b>app count</b> (the portal converts it to cores) or an explicit <b>core count</b>.
      </Text>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>1 · Cores per app (sizing thumb-rules)</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Cores per app</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{rates.appServiceCoresPerApp}</td></tr>
            <tr><td style={td}>Containerized (AKS / ACA)</td><td style={td}>{rates.aksCoresPerApp}</td></tr>
          </tbody>
        </table>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 4 }}>
          Containerized workloads are sized higher ({rates.aksCoresPerApp} vs {rates.appServiceCoresPerApp}) because sidecars, ingress and platform overhead add cores per app.
        </Text>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>2 · Rate per core</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Rate</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{money(rates.appServiceArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>AKS (Linux)</td><td style={td}>{money(rates.aksLinuxArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>AKS (Windows)</td><td style={td}>{money(rates.aksWindowsArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>Azure Container Apps</td><td style={td}>${rates.acaArpuPerCoreHour} / core / hour × {rates.acaUtilization} utilization × {rates.acaHoursPerMonth} hr/mo</td></tr>
          </tbody>
        </table>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>3 · Formula</Text>
        <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 6, padding: '10px 12px', fontFamily: 'monospace', fontSize: 13, lineHeight: 1.6 }}>
          cores = apps × cores-per-app<br />
          monthly ACR = cores × rate-per-core-month<br />
          <b>annual ACR = monthly ACR × {months} months</b><br />
          <span style={{ color: 'var(--colorNeutralForeground3)' }}>(ACA: monthly = rate/core/hr × cores × utilization × hours/mo)</span>
        </div>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Worked example — {apps} apps</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Cores</th><th style={th}>Annual ACR</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{apps} × {rates.appServiceCoresPerApp} = {apps * rates.appServiceCoresPerApp}</td><td style={td}><b>{money(appSvc)}</b></td></tr>
            <tr><td style={td}>AKS (Linux)</td><td style={td}>{apps} × {rates.aksCoresPerApp} = {apps * rates.aksCoresPerApp}</td><td style={td}><b>{money(aksLinux)}</b></td></tr>
            <tr><td style={td}>AKS (Windows)</td><td style={td}>{apps} × {rates.aksCoresPerApp} = {apps * rates.aksCoresPerApp}</td><td style={td}><b>{money(aksWin)}</b></td></tr>
            <tr><td style={td}>Azure Container Apps</td><td style={td}>{acaCores}</td><td style={td}><b>{rates.acaArpuPerCoreHour ? money(aca) : '— (set ACA rate)'}</b></td></tr>
          </tbody>
        </table>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 4 }}>
          e.g. App Service: {apps} apps × {rates.appServiceCoresPerApp} cores × {money(rates.appServiceArpuPerCoreMonth)}/core/mo × {months} = {money(appSvc)}/yr.
        </Text>
      </div>

      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        All rates are editable by an administrator in <b>Configuration → ACR calculation rates</b>, where a live estimator is also available.
      </Text>

      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        Source of the thumb-rules:{' '}
        <Link href="https://microsoft.sharepoint.com/:x:/t/SMFTeamInternal/cQr-UQcfNWAqSqTwK5Jb9ie0EgUCFc3tPEve27xAUmSMNcs2kg" target="_blank" rel="noopener noreferrer">
          Factory_Realized ADS_ACR Calculation.xlsx
        </Link>.
      </Text>
    </div>
  )
}
