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
- `GET /api/export/{resources|capacity|nominations|performance|summary}` → `.xlsx` (ClosedXML
  `ExportService` in Infrastructure). Keep export columns in sync when grid columns change. The **capacity**
  export includes an **Assigned Accounts** column (semicolon-joined) matching the on-screen hover. The
  **nominations** export is filter-aware: it accepts the same query params as the grid
  (`approval`, `migrationStatus`, `currentState`, `sla`, `links`, `search`, `region`) so the file
  matches the on-screen view — the Export button passes the live filter state via
  `api.exportUrl('nominations', region, {...})`. It writes a numeric **Stage** column (1–4), a frozen
  header + autofilter, and a second **Analysis** sheet with pivot-style counts (by approval, stage,
  region, SLA stale tier, wave linkage) of the filtered set.
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

## Editing rules for agents
- Read a file before editing; keep changes minimal and scoped to the request.
- Frontend-only change → `npm run build` + reload; backend change → rebuild + restart API.
- Seeds/migrations must stay **idempotent** so existing imported data (resources/accounts/nominations)
  is preserved.
- Do **not** commit `*.xlsx` source data or `*.log` (already in `.gitignore`). Never auto-commit or
  push without explicit user confirmation.
- Don't create extra markdown docs unless asked. This file is the exception (living guide).

## Roadmap
See `DEVELOPMENT-PLAN.md` §6 (Phase 4). Shipped: data-freshness banner, Excel export (filter-aware
nominations export + Analysis sheet), dashboard drill-through, capacity heat-band, Nominations
restructure (Stage/Status/Summary/TPID/PM/CFTL/SA), approval-status default-Approved, DB
backup/restore (`/backup`).
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
- **Nomination enrichment**: offering fields + dates via `import-offerings` (Summary) and completed migrations
  via `import-completed` (DE-Completed); FDO withdraw guard protects settled states.
Backlog: Leave intake data source, trends/snapshots, my-view, global search, Entra auth, in-app
upload, API smoke tests.
