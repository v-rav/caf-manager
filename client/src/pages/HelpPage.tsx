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

      <FdoHygieneSection />

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

const WORKSPACE_STEPS: GuideStep[] = [
  { title: '1 · Open the workspace', body: 'From the Nominations grid, click the account name (or go to /nominations/{id}). The header shows the account · short name, classification / FDO Stage / TPID badges, the PM · CFTL · SA · Age · clock-stopped meta line, and Readiness % + MSI cards.' },
  { title: '2 · Read the gate stepper (left)', body: 'The 8 gates (G1 Discovery → G8 Closure) run down the left column; the current gate carries a red “!”. Click any gate to load its checklist in the middle column.' },
  { title: '3 · Work the current-gate checklist (middle)', body: 'Each item has a checkbox (Pending → Done), an N/A toggle for items that don’t apply, kind chips, an optional doc-ref link, and a mandatory ⚑ flag. Tick items only when the work is genuinely done; mark truly non-applicable items N/A so the gate can go green.' },
  { title: '4 · Advance the gate', body: 'The “Advance ▸” button enables only when the gate is green (every mandatory item Done or N/A). Advancing moves the current-gate marker forward and recomputes Readiness % and MSI. Repeat through G8.' },
  { title: '5 · Raise & track blockers (right rail)', body: 'Add a blocker with category, owner, ETA, notes, and a clock-stopped flag. A clock-stopped blocker subtracts its open window from the SLA (the grid shows a Paused chip). Blockers surface on the Governance Board; resolve them here or there.' },
  { title: '6 · Record milestones & dates (right rail)', body: 'Add dated app-factory events (type · date · tool used · notes). Kick-off and actual-migration-start are separate dated milestones — important for lift-n-shift where they differ. Each milestone is logged to the Timeline.' },
  { title: '7 · Capture migration capability (panel below the gates)', body: 'Record which tool accelerated which activity (Tool · Activity · Date · Notes). The Activity list is filtered to the activities the selected tool supports. This is the value story — it rolls up on GHCP Adoption (tool adoption, by activity, most-used tool per activity, GHCP-accelerated KPI).' },
  { title: '8 · Review the audit Timeline (right rail)', body: 'An append-only log of every gate-item change, blocker raise/resolve, and milestone add (type · field · old→new · by · at). Use it to see exactly what happened and who did it — no need to reconstruct history elsewhere.' },
  { title: '9 · Keep FDO in sync', body: 'Portal gate/blocker/milestone/capability data is portal-owned and never overwritten by FDO imports — but the SA still owns the technical fields in FDO itself. See the FDO Hygiene guide below for exactly what to keep current and validate before wave closure.' },
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

          <AccordionItem value="workspace">
            <AccordionHeader>Using the SA Workspace (step by step)</AccordionHeader>
            <AccordionPanel>
              <Text size={300} style={{ display: 'block', marginBottom: 8 }}>
                The <b>SA Workspace</b> (<code>/nominations/&#123;id&#125;</code>, opened by clicking an account on the
                Nominations grid) is where a Solution Architect actually drives a migration through the 8 governance gates
                and records the evidence behind it. There is no explicit save — every edit persists immediately and is
                written to the audit Timeline.
              </Text>
              <ol style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 6 }}>
                {WORKSPACE_STEPS.map((s) => (
                  <li key={s.title}><Text size={300}><b>{s.title}</b> — {s.body}</Text></li>
                ))}
              </ol>
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

type FdoBlock = string | string[]
type FdoFaq = { q: string; blocks: FdoBlock[] }

const FDO_CHECKLIST: string[] = [
  'Technical Summary is current',
  'Tool Used is recorded',
  'Automation Used is recorded, when applicable',
  'Product Feedback is captured, when applicable',
  'Intellectual Property is captured, when applicable',
  'Scope document is technically reviewed',
  'Architecture and migration approach are documented',
  'Technical risks, blockers, dependencies, and next actions are current',
  'Application count, migration path, environments, and core data are accurate',
  'App–DB and relevant dependent waves are linked',
  'Technical artifacts, runbooks, and execution evidence are uploaded',
  'Quality Gate review checkbox is completed where required',
  'POE is validated before wave closure',
]

const FDO_FAQS: FdoFaq[] = [
  { q: '1 · What is FDO hygiene?', blocks: [
    'FDO hygiene means keeping the engagement record complete, accurate, current, and supported by evidence. A hygienic record should let another stakeholder understand:',
    ['What is in scope', 'What has been completed', 'Which tools and automations were used', 'What is currently blocked', 'What happens next', 'What evidence supports the reported outcome'],
    'FDO should remain the authoritative record for execution visibility, audit readiness, quality-gate compliance, and outcome reporting.',
  ] },
  { q: '2 · Who owns FDO hygiene: PM or SA?', blocks: [
    'Ownership is shared, but responsibilities differ. PM owns:',
    ['Overall engagement status and stage', 'Planned dates and follow-up dates', 'Customer communications and operational comments', 'Blocked or Deferred status processing', 'Project documentation and operational evidence', 'Overall closure coordination'],
    'SA owns:',
    ['Technical Summary', 'Tool Used', 'Automation Used', 'Product Feedback', 'Intellectual Property', 'Technical risks and dependencies', 'Technical validation of scope, architecture, migration path, application count, cores, and technical evidence', 'Technical Quality Gate review'],
    'Shared: validate FDO reflects the actual engagement state, confirm application and core data before closure, confirm mandatory artifacts are present, confirm blockers/ownership/next actions/evidence, and validate the wave before it is marked complete.',
  ] },
  { q: '3 · When should an SA start updating FDO?', blocks: [
    'Once assigned, and actively as scope and tooling become clear — normally during prerequisite execution and scope finalization. Do not wait until closure to populate all technical fields; update as it becomes known and validate the complete record before the wave closes.',
  ] },
  { q: '4 · How often should the Technical Summary be updated?', blocks: [
    'Update it:',
    ['After every customer or partner technical session', 'Whenever there is a major technical decision, blocker, or scope change', 'At least once per week for an active wave'],
    'The Technical Summary is separate from the PM’s project summary.',
  ] },
  { q: '5 · What should a good Technical Summary contain?', blocks: [
    'Use this structure — Completed: what was achieved since the last update. Issues/Blockers: technical or customer dependencies affecting progress. Actions: follow-ups, owners, expected outputs. Next Session: what will be addressed next.',
    'Example: “Completed AppCAT assessment for 3 applications and reviewed findings with the customer. Repository access is available; lower-environment access is pending. Customer to provide access by 30 Sep. Next session will finalize remediation scope and migration sequencing.”',
  ] },
  { q: '6 · Is Tool Used mandatory?', blocks: [
    'Yes — recorded at wave level for every applicable wave. Capture the tool/product name, the specific feature where relevant, and how it was used. Examples:',
    ['Azure Migrate: Discovery and assessment', 'AppCAT: Application cloud-readiness assessment', 'GHCP App Modernization: Code assessment and remediation', 'GitHub Actions: CI/CD pipeline implementation', 'Bicep or Terraform: Infrastructure provisioning'],
    'If no listed tool applies, follow the current FDO guidance for unavailable or manual options and provide meaningful details.',
  ] },
  { q: '7 · When should Tool Used be updated?', blocks: [
    'When the execution tool is selected or begins to be used — usually during prerequisites or scope finalization. Do not leave it blank until closure; revalidate before closing the wave.',
  ] },
  { q: '8 · What should be entered under Automation Used?', blocks: [
    'Any approved automation, script, or accelerator used during execution:',
    ['PowerShell scripts', 'Approved assessment or migration automation', 'CI/CD automation', 'Infrastructure-as-code automation', 'Factory accelerators'],
    'Include what the automation did and, where required, the effort saved. If not in the selectable list, use the applicable unavailable option and provide details.',
  ] },
  { q: '9 · When is Product Feedback required?', blocks: [
    'When the team identifies:',
    ['A product defect or bug', 'A tool limitation', 'Incorrect or incomplete assessment output', 'A recurring manual workaround', 'A feature request', 'A successful workaround that could improve the product'],
    'Do not use Product Feedback merely to record that a tool was used — use Tool Used for normal tool tracking.',
  ] },
  { q: '10 · What should Product Feedback contain?', blocks: [
    ['Product or tool name', 'Feature or component', 'Problem or observation', 'Customer or execution impact', 'Workaround, if any', 'Supporting evidence or document reference'],
    'Where escalation is required, notify the CFTL so the relevant UAT/TFT tracking ID can be created and mapped back to the FDO feedback entry.',
  ] },
  { q: '11 · When should Intellectual Property be recorded?', blocks: [
    'When a Factory, Microsoft, partner, or practice-owned reusable asset is used — script, tool, process, runbook, agent, or reusable accelerator. Include its category and a short description of how it supported execution.',
  ] },
  { q: '12 · Is the SA responsible for the scope document?', blocks: [
    'The PM coordinates and maintains it; the SA is responsible for technical validation. Confirm the scope document correctly identifies:',
    ['Applications and environments in scope', 'Source and target platforms', 'Migration or modernization activities', 'In-scope and out-of-scope items', 'Customer, partner, Factory, and account-team responsibilities', 'Assumptions and dependencies', 'Acceptance and exit criteria'],
    'After reviewing the artifact, complete the applicable SA Quality Gate confirmation in FDO.',
  ] },
  { q: '13 · What technical documents should be available in FDO?', blocks: [
    'Depending on stage and offering, validate the presence of applicable artifacts:',
    ['Scope confirmation document', 'Assessment output', 'Target architecture or design', 'Migration approach and project plan', 'Prerequisite checklist', 'Runbooks', 'Risk and dependency log', 'Test or validation evidence', 'Deployment evidence', 'POE and closure evidence'],
    'Uploading a document alone is not enough — the corresponding Quality Gate status must also be updated where required.',
  ] },
  { q: '14 · What is the SA’s responsibility for FDO status?', blocks: [
    'The PM updates the official stage and status; the SA must promptly tell the PM when the displayed state does not match technical reality. Examples:',
    ['Prerequisites are incomplete, but the wave shows Executing Migration', 'Execution has stopped for missing access, but the wave remains On Track', 'Migration is complete, but technical validation or POE is missing', 'Scope is not finalized, but the wave shows a later stage'],
    'The SA should not silently allow an inaccurate status to remain.',
  ] },
  { q: '15 · What should happen when work cannot progress?', blocks: [
    'Document the blocker clearly and work with the PM to apply the approved Blocked or Customer Deferred process. State:',
    ['Exact dependency', 'Owner', 'Date identified', 'Impact on execution', 'Action taken', 'Next follow-up date', 'Evidence or customer communication'],
    'Operational status changes are PM-owned; the SA provides the technical blocker details and validates the status reflects reality.',
  ] },
  { q: '16 · Does “Waiting Action on Follow-up Date” stop the velocity clock?', blocks: [
    'Under current guidance, no — this waiting state does not stop the velocity clock. When execution cannot continue because of a customer or external dependency, the PM should use the documented Blocked or Customer Deferred process rather than leave the nomination in an inaccurate waiting state.',
  ] },
  { q: '17 · Who is responsible for App–DB linkage?', blocks: [
    'The SA identifies whether a database dependency exists and validates linkage with the PM. Confirm whether a DB wave is required, that App and DB nominations are linked, validate the relationship before closure, and document why linkage is not applicable if so. The same applies to other dependent waves (ALZ Dispatch, Security) where required.',
  ] },
  { q: '18 · What should the SA validate for application counts and cores?', blocks: [
    'Before closure, validate with the PM:',
    ['Number of applications completed or remediated', 'Application technology, where supported', 'Primary migration path', 'Target Azure service', 'Number of environments', 'Reported core or ACR-related values', 'Whether one reported application contains multiple independently deployed components or containers'],
    'Do not rely only on the core value — application count and technical scope must also be correct.',
  ] },
  { q: '19 · What is POE, and what is the SA’s responsibility?', blocks: [
    'Proof of Execution demonstrates the agreed technical work was completed. Validate that POE matches the approved scope, identifies the workload/application, demonstrates the target Azure outcome, supports the reported application and core data, includes relevant screenshots/logs/deployment evidence or customer validation, and is available before technical closure. The PM coordinates closure; the SA validates the technical evidence.',
  ] },
  { q: '20 · Can a wave be closed if technical hygiene is incomplete?', blocks: [
    'No wave is technically ready for closure while mandatory technical information or evidence is missing. Before closure confirm the Technical Summary is current, Tool Used is captured, Product Feedback and Automation are captured when applicable, scope and technical artifacts are present, risks and blockers are resolved or dispositioned, application counts and core values are validated, related nominations are linked, POE is complete, and Quality Gate reviews are updated.',
  ] },
  { q: '21 · What if FDO does not provide the required option?', blocks: [
    'Do not leave the record blank without explanation. Use the currently approved unavailable / other / manual option where applicable, add meaningful details, and raise the gap through the appropriate FDO/CFTL support process.',
  ] },
  { q: '22 · What are the most common FDO hygiene mistakes?', blocks: [
    ['Technical Summary missing or stale', 'Tool Used blank or only a tool name without usage details', 'Product Feedback not raised for known limitations', 'Scope document uploaded but Quality Gate not checked', 'Architecture or migration plan missing', 'Status does not match actual execution', 'Blockers with no owner or next action', 'Application count blank or zero despite completed work', 'App–DB or ALZ relationships missing', 'POE does not support the reported result', 'Updates postponed until closure'],
  ] },
  { q: '23 · What does “audit-ready” mean?', blocks: [
    'An audit-ready FDO record has current data, approved artifacts, clear ownership, customer communication history, documented status decisions, technical evidence, and no unexplained gaps. A reviewer should not need to contact the SA or PM to understand what happened.',
  ] },
  { q: '24 · What is the simplest rule for SAs?', blocks: [
    'After every meaningful technical activity, update FDO. Before every stage exit, validate FDO. Before closure, verify the evidence.',
  ] },
]

const FDO_CLOSURE: { group: string; items: string[] }[] = [
  { group: 'Scope and design', items: ['Scope is finalized and technically accurate', 'In-scope and out-of-scope items are explicit', 'Target architecture and migration path are validated', 'Customer and Factory responsibilities are clear'] },
  { group: 'Technical data', items: ['Application count is correct', 'Technology and migration path are correct', 'Target service and environment count are correct', 'Core and ACR-related data are validated'] },
  { group: 'Tools and feedback', items: ['Tool Used is complete', 'Automation is recorded, when applicable', 'Product Feedback is recorded, when applicable', 'UAT/TFT ID is mapped when required', 'IP is recorded, when applicable'] },
  { group: 'Dependencies and linked work', items: ['Technical blockers are resolved or formally dispositioned', 'App–DB linkage is complete or justified as not applicable', 'ALZ Dispatch and other dependent waves are linked where applicable'] },
  { group: 'Evidence and closure', items: ['Technical artifacts are uploaded', 'Validation or test evidence is available', 'POE supports the claimed outcome', 'Technical Summary reflects the final state', 'SA Quality Gate review is complete', 'PM and SA have jointly validated closure data'] },
]

const FDO_REFS: string[] = [
  'FY27 CAF PM Operational Standards SOP',
  'FDO Hygiene & Compliance for Tech Community guidance',
  'FDO Product Feedback, Automation and IP walkthrough',
  'App Modernization Leads and Architect Sync guidance',
  'Blocked and Customer Deferred process communications',
  'FDO Hygiene recurring review checklist',
]

function FdoBlocks({ blocks }: { blocks: FdoBlock[] }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      {blocks.map((b, i) => Array.isArray(b) ? (
        <ul key={i} style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 3 }}>
          {b.map((li) => <li key={li}><Text size={300}>{li}</Text></li>)}
        </ul>
      ) : (
        <Text key={i} size={300}>{b}</Text>
      ))}
    </div>
  )
}

function FdoHygieneSection() {
  const tmpl = `Completed: [technical activities completed]
Decision/Outcome: [technical decision or result]
Blocker/Dependency: [issue, owner, impact]
Action: [next action, owner, target date]
Next Session: [planned activity]`
  return (
    <Panel title="FDO hygiene — Solution Architect guide">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 820 }}>
        <div style={{ borderLeft: '3px solid var(--colorBrandStroke1)', paddingLeft: 12, color: 'var(--colorNeutralForeground2)' }}>
          <Text size={300}>
            <b>Core principle:</b> the <b>PM</b> owns overall operational FDO hygiene and project status. The <b>SA</b> owns
            the accuracy, completeness, and currency of the <b>technical</b> information, and validates technical artifacts
            and completion data with the PM.
          </Text>
        </div>

        <Accordion collapsible multiple>
          <AccordionItem value="fdo-checklist">
            <AccordionHeader>Quick SA checklist (per active wave)</AccordionHeader>
            <AccordionPanel>
              <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 4 }}>
                {FDO_CHECKLIST.map((c) => <li key={c}><Text size={300}>{c}</Text></li>)}
              </ul>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="fdo-faq">
            <AccordionHeader>Frequently asked questions (24)</AccordionHeader>
            <AccordionPanel>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                {FDO_FAQS.map((f) => (
                  <div key={f.q}>
                    <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>{f.q}</Text>
                    <FdoBlocks blocks={f.blocks} />
                  </div>
                ))}
              </div>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="fdo-tmpl">
            <AccordionHeader>Technical Summary template</AccordionHeader>
            <AccordionPanel>
              <pre style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 6, padding: '10px 12px', fontFamily: 'monospace', fontSize: 13, lineHeight: 1.6, whiteSpace: 'pre-wrap', margin: 0 }}>{tmpl}</pre>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="fdo-closure">
            <AccordionHeader>Wave closure checklist</AccordionHeader>
            <AccordionPanel>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {FDO_CLOSURE.map((g) => (
                  <div key={g.group}>
                    <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>{g.group}</Text>
                    <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 3 }}>
                      {g.items.map((i) => <li key={i}><Text size={300}>{i}</Text></li>)}
                    </ul>
                  </div>
                ))}
              </div>
            </AccordionPanel>
          </AccordionItem>

          <AccordionItem value="fdo-gov">
            <AccordionHeader>Governance note &amp; references</AccordionHeader>
            <AccordionPanel>
              <Text size={300} style={{ display: 'block', marginBottom: 8 }}>
                This guidance separates technical ownership from operational ownership — the PM is the overall FDO hygiene
                owner, while specific technical fields and reviews are assigned to the SA. Where process guidance changes,
                the latest approved Factory process communication takes precedence.
              </Text>
              <Text size={200} weight="semibold" style={{ display: 'block', marginBottom: 4, color: 'var(--colorNeutralForeground3)' }}>Internal references used</Text>
              <ul style={{ margin: 0, paddingLeft: 18, display: 'flex', flexDirection: 'column', gap: 3 }}>
                {FDO_REFS.map((r) => <li key={r}><Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>{r}</Text></li>)}
              </ul>
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
