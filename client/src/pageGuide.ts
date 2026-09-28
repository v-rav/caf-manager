// Shared page-guide content — consumed by the Help page (grouped guide) and the
// per-page contextual "?" help popover in the header. Keyed to the route's top-level
// segment (matching Layout's access keys), so any page can look up its own entry.

export type PageGuide = { key: string; route: string; name: string; group: string; body: string }

export const GUIDE_GROUP_ORDER = ['Overview', 'Delivery', 'Resourcing', 'Admin'] as const

export const GUIDE_GROUP_INTRO: Record<string, string> = {
  Overview: 'Leadership-level rollups and this help.',
  Delivery: 'The migration pipeline — track, govern, and drive nominations to completion.',
  Resourcing: 'People, accounts, and capacity.',
  Admin: 'Data management, configuration, and platform controls (Admin-only unless noted).',
}

export const PAGE_GUIDES: PageGuide[] = [
  // Overview
  { key: 'dashboard', route: '/', group: 'Overview', name: 'Executive Dashboard', body: 'Four tabbed views: Leadership (totals, ACR influenced, average MSI + band rollup, adoption rate), Operational (by-stage counts, clock-aware SLA on Standard work, open blockers, average effective age, SA load), GHCP Adoption (embedded), and Factory Productivity (completed migrations, ACR realized, cores, tool/automation adoption). KPI cards drill through to the underlying grid. Honors the Region and Fiscal-Year selectors in the header.' },
  { key: 'analytics', route: '/analytics', group: 'Overview', name: 'Migration Analytics', body: 'Headline KPIs plus distribution and value cuts (ACR, cores, adoption) over the Approved pipeline, and a fiscal-year-aware Trends time-series (configurable basis / granularity / measure / split-by). One page-level Excel export (top-right) writes a 4-sheet workbook matching the on-screen filters.' },
  { key: 'help', route: '/help', group: 'Overview', name: 'Help & FAQ', body: 'This page — system usage guide, the SA Workspace walkthrough, the FDO hygiene SA guide, how ACR is calculated (from the live rate master), and the containerized-app counting proposal.' },

  // Delivery
  { key: 'nominations', route: '/nominations', group: 'Delivery', name: 'Nominations', body: 'The pipeline grid: Account, TPID, Offering, Region, Stage (1–4), Status (on track / blocked), Summary (SLA tier + blocker + days-in-stage), PM / CFTL / SA owners, Waves, Team. Defaults to Approved nominations; filter by Approval, Stage, State, SLA breach, Wave links, or search. Click an account to open its SA Workspace. Export is filter-aware.' },
  { key: 'saworkspace', route: '/nominations/:id', group: 'Delivery', name: 'SA Workspace', body: 'Per-nomination cockpit: an 8-gate stepper, the current gate checklist (tick items, mark N/A, advance only when the gate is green), the Migration Capability panel (capture which tool accelerated which activity), and a rail of Blockers, Milestones, and an audit Timeline. Header shows Readiness % and MSI. See the step-by-step walkthrough in the usage guide.' },
  { key: 'governance', route: '/governance', group: 'Delivery', name: 'Governance Board', body: 'All open blockers across the pipeline: KPIs, by-category breakdown, an aging table, and resolve actions. Blockers can be clock-stopping — a live clock-stop freezes that nomination’s SLA.' },
  { key: 'flow', route: '/flow', group: 'Delivery', name: 'Migration Flow', body: 'Funnel conversion and drop-off across the journey, plus bottleneck analytics: in-flight by stage × age, blockers by category, SA workload, and volume by migration type.' },
  { key: 'strategic', route: '/strategic', group: 'Delivery', name: 'Strategic Register', body: 'Strategic-classified nominations: time-threshold tier KPIs (Green/Amber/Red/Exec), share of active pipeline, breakdown by classification, and an aging table.' },
  { key: 'adoption', route: '/adoption', group: 'Delivery', name: 'GHCP Adoption', body: 'Copilot adoption: licensed vs awaiting, used %, tool-attached %, the 0–7 adoption-level distribution, and the Migration Capability rollup (tool adoption, by activity, most-used tool per activity, GHCP-accelerated KPI). CSV export.' },
  { key: 'workspace', route: '/workspace', group: 'Delivery', name: 'SA Workspace (preview)', body: 'A reference/preview mock of the workspace layout. The live workspace opens per nomination from the Nominations grid (click an account).' },

  // Resourcing
  { key: 'resources', route: '/resources', group: 'Resourcing', name: 'Resource Hub', body: 'Roster of delivery resources with skills, region, and their nomination-derived account load.' },
  { key: 'accounts', route: '/accounts', group: 'Resourcing', name: 'Account Hub', body: 'The account master (TPID-keyed customers) with ownership, segment, status, and TPID filters. Master import (Nominations In-Flight.xlsx), de-duplication, and reversible parking of no-TPID rows live here.' },
  { key: 'capacity', route: '/capacity', group: 'Resourcing', name: 'Capacity', body: 'The capacity cockpit: 1 resource = 5 active accounts, utilization heat-bands, a Headroom column, Available-capacity and Bench KPIs, a per-row leave-clash flag, and drill-through from a resource to their nominations. Account count is nomination-derived. Export includes the assigned-account list.' },
  { key: 'leave', route: '/leave', group: 'Resourcing', name: 'Leave', body: 'Leave calendar feeding the capacity leave-clash signal.' },
  { key: 'reconciliation', route: '/reconciliation', group: 'Resourcing', name: 'Reconciliation', body: 'Cross-check imported data against portal state to surface mismatches.' },
  { key: 'performance', route: '/performance', group: 'Resourcing', name: 'Performance', body: 'Performance-review visibility per resource.' },

  // Admin
  { key: 'history', route: '/history', group: 'Admin', name: 'Import History', body: 'Every upload/refresh is recorded as an ImportRun with field-level change deltas (Added / Updated from→to / Withdrawn). Rows are pruned after the retention window.' },
  { key: 'configuration', route: '/configuration', group: 'Admin', name: 'Configuration', body: 'Editable lookup master (blocker categories/owners, classification, velocity, milestone types, tool/skill/segment), ACR calculation rates + live estimator, and operations settings (stale cadence, per-stage targets).' },
  { key: 'gates', route: '/gates', group: 'Admin', name: 'Gate Template', body: 'Add / rename / reweight / reorder / activate / delete the 8 governance gates and their checklist items. Delete is blocked once a nomination has captured progress — deactivate instead.' },
  { key: 'capability', route: '/capability', group: 'Admin', name: 'Capability Masters', body: 'Manage the Migration Tool and Activity masters and the per-tool supported-activity mapping (which activities a tool can accelerate). Delete is blocked once usage exists — deactivate instead.' },
  { key: 'access', route: '/access', group: 'Admin', name: 'User & Access', body: 'Create / disable / reset users, provision SA logins in bulk, and set the role-based page-access matrix. Admin bypasses all page gates.' },
  { key: 'backup', route: '/backup', group: 'Admin', name: 'Backup & Restore', body: 'Download a consistent zipped SQLite snapshot anytime, or restore one (replaces the live DB, keeps a safety copy). Admin-only.' },
]

export function guideForKey(key: string): PageGuide | undefined {
  return PAGE_GUIDES.find((p) => p.key === key)
}

export function pagesInGroup(group: string): PageGuide[] {
  return PAGE_GUIDES.filter((p) => p.group === group && p.key !== 'workspace')
}
