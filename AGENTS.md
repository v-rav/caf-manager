# AGENTS.md — CAF Operations Portal

Guidance for AI/coding agents working in this repo. Read this before making changes so you don't
need re-instruction on conventions, domain rules, and run/build steps.

## What this is
Internal **CAF Operations Portal** (EMEA governance): resource/capacity/leave/strategic-account
visibility, nomination (migration) pipeline tracking, and performance reviews. Single deployable
(.NET API serves the built React SPA from `wwwroot`). Data originates from Excel imports; after
import the **web app is the system of record**.

## Stack & layout
- **Backend**: .NET 9, Clean Architecture.
  - `src/CafPortal.Domain` — entities/enums (no dependencies).
  - `src/CafPortal.Application` — DTOs, service interfaces (`Abstractions/`), services (`Services/`).
  - `src/CafPortal.Infrastructure` — EF Core (SQLite), imports, exports, migrations, seed.
  - `src/CafPortal.Api` — controllers, `Program.cs`, serves SPA.
  - EF Core 9 + SQLite at `src/CafPortal.Api/App_Data/cafdb.sqlite`. Migrations auto-apply on
    startup; `SeedData.SeedAsync` runs on startup (idempotent, per-item guards).
  - Migrations dir: `src/CafPortal.Infrastructure/Persistence/Migrations` (output-dir
    `Persistence/Migrations`).
- **Frontend**: React 19 + TypeScript + Vite in `client/`, **Fluent UI v9**.
  - Reusable: `DataTable<T>` (sortable/paginated), `KpiCard` (tones + optional `onClick` for
    drill-through), `Modal`/`ConfirmDialog`, `common.tsx` (`Panel`, `FilterSelect`, `StatusBadge`,
    `UtilizationBar`, `ErrorText`, `Loading`), `charts.tsx`, `hooks.ts` (`useAsync`, `useDebounced`).
  - `api.ts` is the single API client; `types.ts` mirrors backend DTOs; `region.tsx` holds the
    Global/Regional scope shared by all pages.

## Build / run / verify
- **Backend build**: `dotnet build src/CafPortal.Api/CafPortal.Api.csproj -v q --nologo`
- **Run API**: `dotnet run --project src/CafPortal.Api --urls http://localhost:5080 --no-launch-profile [--no-build]`
- **Frontend build** (outputs into `src/CafPortal.Api/wwwroot`): `cd client; npm run build`
- A **frontend-only** change needs just `npm run build` + browser reload — no API restart.
- A **backend** change needs an API rebuild/restart.

### Terminal quirks (Windows PowerShell)
- Kill the API before any .NET rebuild: `Get-Process -Name CafPortal.Api -ErrorAction SilentlyContinue | Stop-Process -Force`
- Start API in background:
  `Start-Process dotnet -ArgumentList 'run --project src/CafPortal.Api --urls http://localhost:5080 --no-launch-profile --no-build' -RedirectStandardOutput api.out.log -RedirectStandardError api.err.log -WindowStyle Hidden; Start-Sleep -Seconds 18`
  then **delete** the temp `api.out.log` / `api.err.log` afterwards.
- EF tools path: `$env:PATH += ";$env:USERPROFILE\.dotnet\tools"`.
- The terminal wrapper may drop a leading `cd` (it "simplifies" the command) — harmless.

## Domain rules (must preserve)
- **Capacity model**: `1 resource = 5 active accounts`. `Utilization = AccountCount / CapacityLimit`.
  Bands: 0–2 Available · 3–4 Partially Utilized · 5 Fully Utilized · 6+ Overloaded. `CapacityRowDto`
  carries a `HeatColor` (Green/Amber/Red).
  - **Account count is nomination-derived**: distinct in-flight accounts via `NominationResource → Nomination
    → Account` (Approved + not settled), NOT the noisy `ResourceAccounts` links. Both the live `CapacityService`
    (Capacity page/export) and the materialized `CapacityFact` (Dashboard + Resources page) use this basis and the
    same `ResolveCapacityLimitAsync` limit engine, so all three agree. `CapacityRebuildService` rebuilds
    `CapacityFact` on startup and after every import/merge/park.
- **Nomination = migration tracking**. Two distinct tracking fields, keep them separate:
  - **Stage** (`Nomination.MigrationStatus`): which **phase** of the migration journey. Show as a
    **number 1–4**, not raw text. Mapping (keyword → stage) lives in `STAGES` +
    `migrationStage()` in `NominationsPage.tsx`:
    1 Validating · 2 Executing Pre-Requisites · 3 Finalize Scope · 4 Executing Migration.
  - **Status** (`Nomination.CurrentState`): whether work is **on track or blocked**. Colour via
    `stateColor()`: On Track = success, Waiting/follow-up = warning, Blocked* = danger.
  - **Summary**: operational health only — SLA stale tier (Warn/Escalate/Defer) badge, blocker
    reason, and days-in-stage; full detail (current state, blocker, blocked-since, follow-up,
    age·SLA, waves, remarks) on hover. The synthetic workflow status (Open/In Progress/Closed) is
    **not shown** (it duplicated Stage and contradicted Current State); it stays editable only in the
    Manage dialog. A **SLA breach** filter (Warn/Escalate/Defer) narrows the grid by stale tier.
  - **Age = days in the current stage** (`Nomination.StageAgeDays`), imported from that stage's
    day-count column in `Detail View.xlsx`: "1 - Validating & Initial Scope", "2 - Executing
    Pre-Requisites", "3 - Finalize Scope", "4 - Executing Migration" (values like "9 days").
    `NominationService` uses it as the age basis (falls back to update-recency) and it drives the
    stale tier.
    > ⚠ **Pending (verify vs FDO):** the stage day-count may only accrue **after** an SLA breach, not
    > from stage entry. If so, the Warn/Escalate/Defer thresholds and the "Age in stage" label need
    > re-mapping. Tracked as PV-01 in `DEVELOPMENT-PLAN.md` §7 — do not treat the current SLA-breach
    > basis as final.
- **TPID** lives on `Account` (external master key), surfaced onto `NominationDto.Tpid` via
  `Include(n => n.Account)` in `NominationService`.
- **Approval status** (`Nomination.ApprovalStatus`, FDO-owned): from the FDO "Nomination Approval
  Status" column (values seen: Approved · Declined · Provisionally Approved · Active Concierge). The
  Nominations grid **defaults to `Approved` only** via an **Approval** `FilterSelect` (default state
  `'Approved'`, "All Approval" clears it); the approval scope drives both the KPIs and the grid so
  counts match. Surfaced as an **Approval** export column. (Declined also maps to `Status=Withdrawn`
  via `MapStatus` on first insert.)- **Ownership short names** used in the Nominations grid: **PM** = `ProjectCoordinator`,
  **CFTL** = `CftlPrimary`, **SA** = `SolutionArchitect`. These are **portal-owned** (editable in the
  Manage dialog); the FDO import only **seeds them when empty** (never overwrites a portal edit).
- **Wave linkage (informational, NOT required)**: a nomination may legitimately have **no waves**;
  no wave type is mandatory (a DB wave is only sometimes relevant, and many apps have no DB). Wave
  links come from the FDO export's **`Linked to` / `Linked to ID`** columns (';'-separated), which
  `NominationImportService` parses and classifies by name → `WaveType` (`security`→Security,
  `sql`/`ossdb`/`database`→Db, `landing zone`/`alz`→LandingZone, `dispatch`→Dispatch; App/unknown are
  skipped). FDO links carry `WaveLink.Source="FDO"` and are **refreshed every drop**; portal-added
  links (`Source="Portal"`, via the Manage dialog) are **preserved**. `NominationService` derives
  `DbLinked`/`AlzLinked`/`SecurityLinked`, `WaveCount`, and `NoWavesLinked` (WaveCount==0). Surfaced as
  a **Waves** column (present-type chips, or a neutral "None"), a **No waves linked** KPI
  (drill-through) whose only purpose is to **find records to enrich**, a **Links** filter
  (No waves / Has any waves / Has DB / Has Security), a checklist in the Manage dialog, and a
  **Wave Types** export column. The current FDO export contains only App/Security/SQL(DB)/OSSDB(DB)
  waves — there is **no Landing Zone/Dispatch/ALZ wave** in the data, so ALZ stays empty unless added
  in the portal.
- **Stale cadence** (configurable via `ApplicationSetting`): Warn `StaleWarnDays=3` ·
    Escalate `StaleEscalateDays=5` · Defer `StaleDeferDays=10`. **Applies to execution Stages 2–4
    only** (not Stage 1 Validating) — gated in `NominationService.StaleTier` via `StageIndex`. The
    documented governance action per tier (Day 3 remind → Blocked, Day 5 2nd remind, Day 10 →
    Customer Deferred) plus the **>6-week-in-stage** (`DEFER_WEEKS_DAYS=42`) Customer-Deferred rule are
    surfaced as the "Next step" line in the Summary hover (`recommendedAction()` in `NominationsPage`).
    **Per-stage target days** (`StageTargetDays1..4`, defaults 10/10/5/23 from the FDO TARGET columns) are
    editable in Configuration → Operations Settings; currently informational (config-only), not yet wired
    into `StaleTier` (that swap is the PV-01 decision).
- **Account master data** (single source of truth): `Account` carries identity (`AccountId`, `Tpid`,
    `ExternalAccountId`, `AccountName`, `Segment`, `Region`, `Status`, `StrategicFlag`) + the ownership
    matrix. `AccountMasterImportService` loads **`Nominations In-Flight.xlsx`** (Segment · TPID · Customer
    Name · Account ID) — one row per in-flight nomination, so it **dedups by TPID** (→ Account ID →
    alias-aware name) to one record per account, **upsert-merges** (sets canonical Name + Segment +
    ExternalAccountId, adds missing, syncs the segment vocabulary), and is **additive — never deletes,
    never touches ownership/region/status**. Upload kind **`accounts`** (Accounts page → *Import master*),
    wired into `DataRefreshService` after nominations. The FDO nominations import **also creates accounts**
    by TPID (`ResolveAccountAsync`) — but leaves **Segment blank**, which the master file backfills
    (segment-present = "in master list"). New master accounts get `Region=UNSPECIFIED` (not in the file).
    The **Accounts page** owns master + ownership actions only; associated info (resources, nominations,
    engagements) lives on its own page.
  - **Account de-duplication** (`POST /api/admin/merge-accounts?apply=`): folds casing/punctuation variants
    into the TPID-bearing master (re-points Nomination/ResourceAccount/WaveLink/EngagementFact/OwnershipHistory/
    StrategicAccount FKs, keeps the variant as an alias). Skips non-Latin names and groups with ≥2 distinct TPIDs.
  - **Account parking** (`POST /api/admin/park-accounts?apply=`, `POST /api/admin/unpark-accounts`): the master is
    **TPID-keyed canonical customers**; **no-TPID rows are non-canonical** (departments / app names / abbreviations
    from name-only imports) and move to the reversible **`ParkedAccount`** snapshot (full record + resource links +
    referencing nomination ids). **Skips any no-TPID account a nomination still references** (kept in the master until
    the next FDO drop gives it a TPID) so nothing is orphaned. Nullable FKs nulled; non-null `ResourceAccount`/
    `OwnershipHistory` recorded then removed; **Unpark** fully restores. Signal: `no TPID ⇒ no segment ⇒ not in master`.
- **Regions**: Global Lead → EMEA (Ravinder Rana) / ASIA. Region scope flows from `region.tsx`.
- **Fiscal-year scope** (`fy.tsx`, header selector next to Region, default **current FY**; FY starts Jul 1, labelled
  by end year). Nomination-centric pages (Nominations, Dashboard, Strategic Register, GHCP Adoption, Migration Flow)
  honor it via `nominationInFy(n, fy)`: **every nomination belongs to one FY** — settled items by their close date
  (`ActualEndDate ?? NominatedDate ?? OpenedDate`), active/in-flight by when they entered (`NominatedDate ??
  OpenedDate`). So a FY view = work completed-this-FY + work nominated-this-FY (still in flight); "All FY" shows
  everything. The FY genuinely filters every section (SA workload, stage bottleneck, funnel, KPIs). Analytics keeps
  its own dedicated FY/Trends controls; Governance Board (open blockers) is inherently current, so neither uses the
  global FY.

## UI conventions
- Prefer **short, business column names** (Stage, Status, Summary, PM, CFTL, SA) over verbose ones.
- Use existing components (`DataTable`, `KpiCard`, `Panel`, `FilterSelect`, `Modal`) — don't hand-roll
  tables/inputs.
- Fluent `Badge` colours: success/warning/danger/informative/brand. `Button as="a" href={...}` for
  downloads.
- KPI cards can be drill-through (`onClick` → `navigate('/page?filter=...')`); pages seed filters from
  query params via `useSearchParams`.
- Numeric/short encodings preferred where the user reads at a glance (e.g. Stage number with a Tooltip
  carrying the full phase text).

## Data freshness & exports
- **FDO ingestion is upsert-merge, NOT wipe-and-reload** (Phase 5A). Nominations are matched on the
  FDO **Task Id** (`Nomination.ExternalTaskId`, indexed); FDO-owned fields are refreshed each drop and
  portal-owned fields (`Status`, `BlockedReason`, `BlockedSince`, `FollowUpDate`, `WaveLinks`, edited
  `Remarks`) are **preserved**. Rows missing from a drop are soft-set `NominationStatusType.Withdrawn`
  (`= 8`) — never hard-deleted (keeps history + waves). **Settled states are never auto-withdrawn**: the
  withdraw guard skips `Withdrawn/Closed/Completed/CustomerDeferred`, so completed migrations survive
  in-flight drops. `DataRefreshService` must **not**
  `ExecuteDeleteAsync` nominations. See `NominationImportService` loop + `DEVELOPMENT-PLAN.md` §8.
  (⚠ `ResourceAccounts`/`LeaveFacts`/`EngagementFacts` still wipe-and-rebuild — safe today; fix before
  portal-entered leave.)
- **Nominations are NEVER hard-deleted by imports.** Absent-from-drop rows are soft-set `Withdrawn`;
  hard delete is a manual, user-only action from the UI. Do not add auto-purge of nominations.
- **Import history** (Phase 5C): every upload/refresh records an `ImportRun` (+ `ImportChange` deltas:
  Added / Updated with field-level from→to / Withdrawn) via `NominationImportService`. Surfaced on the
  **Import History** page (`/history`, `GET /api/imports`, `GET /api/imports/{id}/changes`). History rows
  (not nominations) are pruned after `HistoryRetentionDays` (90). Upload filename is stamped onto the run.
- **Nomination enrichment (one-time, keyed by TPID + Task Id)**: `POST /api/admin/import-offerings?apply=`
  reads `Summary of All Offerings.xlsx` (the **single source**) and (a) sets **offering fields**
  (`PrimaryMigrationPath`, `PartnerName`, `TotalCores`, `IsToolAttached`, `IsAutomationUsed`, `ModeOfAccess`,
  `TotalAcr`, `NnrAcr`) + **dates** (`NominatedDate`, `ApprovalDate`, `ActualStartDate`, `ActualEndDate`,
  `PlannedStartDate`, `PlannedEndDate`, derived `TotalDays`) on matched nominations — marking a matched row
  `Completed` once it carries an `ActualEndDate`; (b) **creates a `Completed` nomination for any unmatched row
  that has an `ActualEndDate`** (resolving/creating the account by TPID); rows with no end date are skipped;
  (c) refreshes `Account.Segment` by TPID. On-demand admin op (NOT wired into refresh/seed), serial-date-aware,
  `apply=false` previews. This one import supersedes the retired DE-Completed/DE-Inflight imports (the Summary is
  the superset — 356 rows carry a completion `Actual End Date`).
- `GET /api/admin/status` → counts + `lastRefreshUtc` (stamped by `DataRefreshService` on every
  successful import). Surfaced as "Updated Xm ago" in the header.
- `GET /api/export/{resources|capacity|nominations|performance|summary|analytics}` → `.xlsx` (ClosedXML
  `ExportService` in Infrastructure). Keep export columns in sync when grid columns change. The **capacity**
  export includes an **Assigned Accounts** column (semicolon-joined) matching the on-screen hover. The
  **nominations** export is filter-aware: it accepts the same query params as the grid
  (`approval`, `migrationStatus`, `currentState`, `sla`, `links`, `search`, `region`) so the file
  matches the on-screen view — the Export button passes the live filter state via
  `api.exportUrl('nominations', region, {...})`. It writes a numeric **Stage** column (1–4), a frozen
  header + autofilter, and a second **Analysis** sheet with pivot-style counts (by approval, stage,
  region, SLA stale tier, wave linkage) of the filtered set.
- The **analytics** export (`GET /api/export/analytics`) is the **single page-level** download for the
  Migration Analytics page (one button, top-right; no per-chart buttons). It builds a 4-sheet workbook —
  **Overview** (KPIs), **Distributions** (all count cuts stacked), **Value cuts** (ACR/cores stacked), and
  **Trend** (the live time-series view) — and is Trends-filter-aware: it takes the same params as the Trends
  section (`basis`, `granularity`, `measure`, `splitBy`, `fy`, `region`) so the file matches what's on screen
  (the page owns that filter state and passes it via `api.exportUrl('analytics', region, {...})`).
- **Backup & Restore** (`/backup` page, database icon in nav; `BackupService` in Infrastructure,
  `BackupController`). `GET /api/backup/download` → a consistent, zipped SQLite snapshot via
  `VACUUM INTO` (`cafdb_backup_<utc>.zip`, entry `cafdb.sqlite`); safe to run anytime. `POST
  /api/backup/restore` (multipart, 100 MB cap) **replaces the live DB**: it extracts the DB from the
  zip, **validates** it (`PRAGMA integrity_check` + confirms a `Nominations` table), keeps a
  server-side safety copy `cafdb.sqlite.prerestore_<utc>` next to the DB, releases EF/SQLite handles
  (`ChangeTracker.Clear` + `CloseConnection` + `SqliteConnection.ClearAllPools()` + GC), swaps the
  file, deletes stale `-wal`/`-shm` sidecars, then runs `MigrateAsync` and returns post-restore
  counts. The validation connection uses `Pooling=false` so the temp file can be deleted; an invalid
  zip returns **400** with a message. ⚠ Restore is **unauthenticated** (like the rest pre-Entra) and
  `prerestore_*` copies accumulate in `App_Data` (gitignored) — add auth + a keep-last-N prune later.

## Factory Operating System (P1–P5, shipped)
The portal is now a gated, SA-owned **Factory Operating System** on top of the visibility portal. Blueprint:
`FACTORY-OPERATING-SYSTEM.md`. All FOS data is **portal-owned** (never overwritten by FDO imports). Governance
entities live in `Domain/Entities/Governance/`; maps in `Persistence/Configurations/GovernanceMaps.cs`;
`GovernanceService` + `NominationService` compute the derived scores.

- **Auth (custom logins, not Entra)** — cookie auth (`caf.auth`, HttpOnly, 8h sliding), PBKDF2 in
  `PasswordHashing`, roles **Admin/Lead/SA**. `AppUser` entity; `AuthController` (login/logout/me/change-password),
  `UsersController` (`[Authorize(Roles="Admin")]` CRUD + reset). `ICurrentUser`/`CurrentUser` read claims.
  Frontend `auth.tsx` gates the router (`LoginPage`, forced password change). **Admin login `admin` / `admin@1234`.**
  Default admin seeded idempotently in `DbInitializer`. `ACCESS.md` documents it. ⚠ Backup/restore still
  unauthenticated.
  - **Bulk-provision SA logins** — Configuration → Users & access → *Provision SA logins* (`POST /api/users/provision-sa`,
    Admin-only) creates an SA account for every distinct `Nomination.SolutionArchitect` (idempotent by display name;
    username slug from the name; temp password `Sa@12345`, forced change). Display name = exact SA name so the SA view matches.
  - **SA self-scoped view** — on `/nominations`, SA-role users default to a **My nominations** switch (their own book of
    work, matched by display name via `matchesPerson`); they can toggle it off to see all. **Page-level (role-based)
    route access is the next step** (not yet enforced — all signed-in users can reach every page).
- **Gate engine (P1)** — 8-gate SA template (`GateDefinition` + `GateItemDefinition`, seeded idempotently by
  `GateTemplateSeed`: G1 Discovery w10 · G2 Prerequisites w15 · G3 Assessment w15 · G4 Scope w20 · G5 Architecture
  w10 · G6 Delivery Readiness w8 · G7 Delivery Governance w7 (17 items by SubStage) · G8 Closure w5). The template is
  **editable in the Admin-only Gate Template page** (`/gates`, `GateTemplateController` at `api/gate-template`):
  add/rename/reweight/reorder/activate-deactivate/delete gates and items; delete is blocked (409) once a nomination has
  captured progress on it (deactivate instead — deactivating hides it from new work). Per-nomination
  state = `NominationGateItem` (Pending/Done/NotApplicable). **Readiness compliance %** = weight-weighted gate
  completion. **Real SA Workspace** at `/nominations/:id` (`NominationWorkspacePage`): 3-column — gate stepper
  (red "!" on current gate) │ current-gate checklist (checkbox + **N/A toggle**, kind chips, doc-ref link, mandatory
  ⚑, **Advance ▸** button enabled only when the gate is Green) │ **Blockers + Milestones + Timeline** rail. Header:
  account **· short-name**, classification + **FDO Stage** + TPID badges, **PM/CFTL/SA · Age · clock-stopped** meta,
  **Readiness + MSI** cards. Account cell on `/nominations` links here.
- **Blockers & clock-aware SLA (P2)** — `NominationBlocker` (category · **clock-stopped** · owner · ETA · notes ·
  raised/resolved-by). Raise/resolve in the workspace; **Governance Board** at `/governance` (open blockers, KPIs,
  by-category, aging table, resolve). **Clock-aware SLA**: `NominationService` subtracts clock-stopped windows from
  stage age → `EffectiveAgeDays`; `StaleTier` runs off effective age (a live clock-stop freezes it). Grid shows a
  **Paused** chip. `GET /api/governance/blockers|blocker-categories|blocker-owners`.
- **Audit log (P2)** — append-only `NominationEvent` (type · field · old→new · by · at). Logged on gate-item change,
  blocker raise/resolve, milestone add. **Timeline** panel on the workspace; `GET .../governance/events`.
- **Classification & strategic register (P3)** — `Nomination.Classification` (vocab) + `VelocityImpact` +
  strategic **time-threshold tier** (A.13 60/90/120 → Green/Amber/Red/Exec on days-in-flight). Grid **Class** column
  + Classification filter; Manage-dialog fields. **Strategic Investment Register** at `/strategic` (tier KPIs,
  %-of-active-pipeline, by-classification, aging table). `isStrategic` = classification ≠ Standard Factory.
- **GHCP adoption (P4)** — `Nomination.GhcpAdoptionLevel` (0–7, A.10), Manage-dialog dropdown. **GHCP Adoption**
  page at `/adoption` (licensed L4+ · awaiting L1–3 · used% · tool-attached% · 0–7 distribution · by-region).
- **MSI (P5a)** — **Migration Success Index** (`MsiCalculator` in `Application/Common`): `30% Readiness (G1–3) +
  20% Scope (G4–5) + 20% Delivery (G6–7) + 10% Risk (blockers) + 10% GHCP (level/7) + 10% Sign-off (G8)`, bands
  Green >80 / Amber 60–80 / Red <60. Grid **MSI** column (component tooltip) + workspace header card; computed in
  both `NominationService` (grid) and `GovernanceService` (workspace) so they agree.
- **Migration Flow (P5b)** — `/flow` (`FlowPage`): funnel conversion/drop-off across the journey + bottleneck
  analytics (in-flight by stage × age, blockers by category, SA workload, by migration type). Derived client-side.
- **Executive Dashboard 4 views (P5c)** — `/` is a `TabList`: **Leadership** (totals · ACR influenced · avg MSI +
  band rollup · adoption rate) · **Operational** (by-stage · **Standard-only** clock-aware SLA · blockers · avg
  effective age · SA load) · **GHCP Adoption** (F4 embedded) · **Factory Productivity** (completed · ACR realized ·
  cores · tool/automation adoption; value-realization KPIs await outcome capture). Composed from
  dashboard + nominations + blockers feeds.
- **Milestones & dates** — `NominationMilestone` (type · **date** · **tool used** · notes · recorded-by). Dated
  app-factory events captured in the workspace **Milestones** panel (add/list/delete), logged to the Timeline.
  Handles the lift-n-shift exception: **kick-off** and **actual-migration-start** are separate dated milestones.
  `GET /api/governance/milestone-types`, `POST/DELETE .../governance/milestones`.
- **Editable lookup master** — one generic `LookupValue` table (category + value + order) powers all governance
  vocabularies: **BlockerCategory · BlockerOwner · Classification · VelocityImpact · Milestone** — plus the existing
  Tool/Skill/Segment configs. Seeded idempotently per category by `LookupSeed`; admin CRUD via
  `GET/POST/PUT/DELETE /api/configuration/lookups`; edited in **Configuration** (add/rename/delete panels).
  Vocab endpoints (`blocker-categories/owners`, `nominations/vocab`, `milestone-types`) read from it (fallback to
  defaults). `ILookupService.ValuesAsync(category)`. Existing rows keep their stored string, so editing/deleting a
  value never orphans data.
- **ACR rate master + estimator** — editable FDO ACR thumb-rules stored as `ApplicationSettings` (`Acr*` keys, with
  documented defaults: App Service $98/core/mo · 1 app = 2 cores · AKS Linux $30 · AKS Windows $56 · 1 containerized app = 4 cores ·
  ACA = $/core/hr × cores × utilization × hours/mo · ×12 annualization). `IAcrService` reads/writes them and estimates
  ACR from apps or cores: `GET/PUT /api/acr/rates`, `POST /api/acr/estimate`. Edited in **Configuration → ACR
  calculation rates** (rate fields + a live estimator). Example: 10 App Service apps × 2 cores × $98 × 12 = $23,520/yr.
- **Help & FAQ** page (`/help`, Overview nav, all roles): FAQ accordion; first entry explains ACR calculation, rendered
  from the **live** ACR rate master (thumb-rules · per-core rates · formula · worked example) so it never goes stale.

## Editing rules for agents
- Read a file before editing; keep changes minimal and scoped to the request.
- Frontend-only change → `npm run build` + reload; backend change → rebuild + restart API.
- Seeds/migrations must stay **idempotent** so existing imported data (resources/accounts/nominations)
  is preserved.
- Do **not** commit `*.xlsx` source data or `*.log` (already in `.gitignore`). Never auto-commit or
  push without explicit user confirmation.
- Don't create extra markdown docs unless asked. This file is the exception (living guide).
- **FOS data is portal-owned** — governance/gates/blockers/milestones/classification/adoption/short-name are edited
  in the portal and must **never** be overwritten by FDO/offering imports. Verify authenticated APIs via a browser
  `page.evaluate(fetch(...))` (carries the auth cookie); Fluent form-submit clicks can time out under Playwright.

## Roadmap
See `DEVELOPMENT-PLAN.md` and `FACTORY-OPERATING-SYSTEM.md`. **Shipped:** data-freshness banner, filter-aware
Excel exports (+ Analysis sheet), dashboard drill-through, capacity heat-band, Nominations restructure, approval
default-Approved, DB backup/restore, Migration Analytics + Trends + FY filter, Attainment, and the full **Factory
Operating System P1–P5** (custom auth · 8-gate engine + SA Workspace · blockers + clock-aware SLA · audit timeline ·
classification + strategic register · GHCP adoption · MSI · migration flow · 4-view exec dashboard), the
workspace-mock-aligned redesign, the editable **lookup master**, **milestones & dates**, **role-based page access +
User & Access page**, **CSV exports + light filters on Adoption/Strategic/Flow**, **Admin-gated Backup/Restore
(+ prune)**, the **kick-off → actual-start milestone lag** on the grid + analytics, the **grouped collapsible
left nav** (Overview / Delivery / Resourcing / Admin — only the active group expanded, so the sidebar never scrolls),
and **Migration Capability Utilization** (Tool × Activity masters + per-nomination usage + leadership rollups).
**Backlog:** `NominationOutcome` value-capture (hours saved · defects · CSAT → real Factory Productivity);
config-editable strategic time-thresholds + PV-01 stale-basis decision; **ACR estimator on the Analytics page + a
per-nomination "estimate ACR" action** (populate `TotalAcr` from a wave's app count); retire the `/workspace` P0 mock
(kept as reference); `ResourceAccounts`/`LeaveFacts`/`EngagementFacts` wipe-and-rebuild fix before portal-entered
leave; Entra auth; in-app upload; API smoke tests.
- **Migration Capability Utilization** (the GHCP/AppMod/accelerator value story): two portal-owned masters —
  **`MigrationTool`** (Name · Category · Vendor; seeded 15-tool taxonomy — **Assessment** (Azure Migrate, AppCAT) ·
  **GHCP** (GHCP-CI, GHCP Agent Mode, GHCP Custom Prompts, AppMod .NET/Java/CLI, Upgrade Assistant — AppMod is a
  GHCP-powered capability, not its own category) · **Accelerator** (AKS/ACA/App Service) · **Other** (Partner Tooling,
  Customer Tooling, Internal Automation)) and **`MigrationActivity`** (Name · Stage; 20 activities across Assessment /
  Planning & Architecture / Modernization / Engineering Automation / Migration / Operations) — plus the
  **`NominationToolUsage`** junction (Tool × Activity × optional date/notes) capturing *which capability was actually
  accelerated by which tool*. A separate configurable **`MigrationToolActivity`** mapping defines *which activities a
  tool CAN support* (capability, not usage): empty mapping = supports **any** activity (Partner/Customer/Internal
  tooling). Seeded by `MigrationCapabilitySeed`, which **reconciles the masters + capability mapping to the canonical
  lists while no usage exists** (taxonomy still malleable) and is additive-only once usage is captured. `MigrationToolDto`
  carries `SupportedActivityIds` (from the mapping) so the SA Workspace **Activity picker only shows activities the
  selected tool supports**. Captured in the SA Workspace panel (full-width, below the gates; Tool · Activity · Date ·
  Notes; add/list/delete, logged to the audit timeline); rolled up on the **GHCP Adoption** page
  (`GET /api/governance/capability?region=`): **tool adoption · by activity · most-used tool per activity** (distinct
  nominations) + a **GHCP-accelerated (usage)** KPI beside the coarse FDO **Tool-attached %** + CSV export. The masters
  + mapping are edited in the Admin-only **Capability Masters** page (`/capability`, `MigrationCapabilityController` at
  `api/capability-master`): CRUD for tools (name/category/vendor/active) and activities (name/stage/active) + a per-tool
  supported-activity checklist; delete is blocked once usage exists (deactivate instead). Endpoints:
  `GET governance/migration-tools|migration-activities|capability`, `POST/DELETE nominations/{id}/governance/tool-usages`,
  `api/capability-master/tools|activities` (+ `tools/{id}/activities`).
- **Role-based page access** (`/access` **User & Access** page, Admin-only): a `PageAccess` matrix (17 pages × roles,
  stored as `ApplicationSettings` `PageAccess:<key>` = csv roles via `AccessService`/`AccessController`). `Layout`
  filters the nav and **gates the route** by the signed-in role; **Admin bypasses everything**. The Users panel (create/
  disable/reset · **Provision SA logins**) moved out of Configuration onto this page. Backup/Restore is now
  `[Authorize(Roles=Admin)]` and restore keeps only the newest 5 `prerestore_*` safety copies.
- **Capacity page cockpit**: Headroom column, Available-capacity + Bench KPIs, Bench filter,
  per-row leave-clash flag, Resource→Nominations drill-through (`?person=`), capacity export account list.
- **Dashboard data-source alignment**: `CapacityFact` is nomination-derived (matches the Capacity page);
  **Active Nominations** = Approved & in-flight (not deprecated `Status==Open`); **Total Accounts** relabel;
  **Strategic** KPI + coverage use `Segment == 'Strategic'` (68 canonical customers, not the seed flag);
  capacity doughnut uses heat-band colours.
- **Account data quality**: de-dup merge (`/api/admin/merge-accounts`) + reversible no-TPID parking
  (`/api/admin/park-accounts` / `unpark-accounts`, `ParkedAccount` table); Accounts hub Segment/Status/TPID filters.
- **Migration Analytics** (`/analytics`, `GET /api/analytics?region=`): headline KPIs (in-flight, Total/NNR ACR,
  cores, tool/automation adoption) + distributions (stage, current-state health, SLA, region, segment, path,
  mode, waves) + value cuts (ACR by region/segment/path, cores by stage, top partners). Built by
  `AnalyticsService`, which reuses `INominationService` (so it inherits stage/stale/wave + offering/date fields)
  and joins `Segment` by AccountId; scoped to Approved nominations.
- **Trends (time-series)** on the Analytics page (`GET /api/analytics/timeseries` +
  `/api/analytics/timeseries/detail`): a **fiscal-year-aware** pivot of Approved nominations over time.
  Configurable **basis** (which date buckets a record — `nominated`/`approved`/`started`/`completed`, default
  `completed`), **granularity** (`week` = ISO calendar week · `month` default · `quarter`/`year` = **fiscal**),
  **measure** (`count` default · `acr`/`nnr`/`cores` sums), and **splitBy** (`none`/`region`/`segment`/`path`/
  `stage`/`status` → stacked series). Optional `fy` (4-digit fiscal year, e.g. `2027`; default **All**) and
  `from`/`to` (DateOnly). Returns `TimeSeriesDto` (chronological `Buckets` of `NameValueDto[]` + per-bucket/overall
  totals + `RecordsWithoutDate` + `FiscalYears` present, which drives the data-driven **Fiscal year** filter).
  The UI renders a stacked
  `TimeSeriesChart` (charts.tsx) + an aggregated `DataTable<TimeBucket>` whose **Period** cell is a `Link` that
  drills into an **in-page** `DataTable<Nomination>` for that bucket (via `/timeseries/detail?bucket=<key>`).
  **Fiscal convention** (`CafPortal.Application/Common/FiscalCalendar.cs`): FY starts **Jul 1**, labelled by its
  **end year** — Jul 2026–Jun 2027 = **FY27** (we are in FY27 now). `Bucket(date, granularity)` yields a
  chronological sort key + stable key + display label (e.g. `FY27 Q1`, `Sep 2026`, `2026-W38`).
- **Nomination enrichment**: offering fields + dates via `import-offerings` (Summary, single source; also
  creates Completed nominations from Actual End Date); FDO withdraw guard protects settled states.
Backlog: cycle-time measure (avg days / nominated→completed), bucket→Nominations date-range drill, trend CSV
  export, weekly view is sparse; Leave intake data source, my-view, global search, Entra auth, in-app upload,
  API smoke tests.
