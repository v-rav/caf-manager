# CAF Operations Portal — Development Plan

Derived from the 90-day pain-point analysis of App Factory daily management and tracking
(Microsoft 365 Copilot, Sep 2026) **plus the confirmed follow-up answers** from CAF governance /
FDO enhancement reviews. This plan maps each evidenced pain point to a concrete feature in the
portal, classifies it as **Mandatory / Recommended / Optional**, and groups the work into phases.

> Governance evidence promoted **App↔DB↔Wave linkage** to Mandatory (a stated FDO enhancement
> requirement, not a nice-to-have), and supplied concrete enums + a Day 3/5/10 stale-escalation
> cadence — both folded in below.

Legend — Priority: 🔴 Mandatory · 🟠 Recommended · ⚪ Optional
Status: ☐ Not started · ◐ Partial (already in portal) · ✅ Done

> **Status: Phase 1 + Phase 2 + Phase 3 implemented and verified** (migration `AddOperationsTracking`).
> P3-02 (reminders) is delivered as the in-app Blocked/stale "Needs update" views rather than email;
> P3-03 (comment-mining) is delivered as a heuristic blocker-reason suggestion in the Manage dialog.

---

## 1. Pain point → feature traceability

| # | Pain point (evidence) | Feature | Module | Priority | Status |
|---|-----------------------|---------|--------|----------|--------|
| 1 | PMs leave items "In Progress" while waiting on customer/dependency | Explicit **Blocked state + reason** on nominations (waiting-for: customer / approval / testing / dependency) | Nominations | 🔴 | ✅ |
| 2 | Manual status chasing; no system-driven tracking | **Stale-item flag** — highlight nominations not updated in N days; "Needs update" view | Nominations | 🔴 | ✅ |
| 3 | External blockers hidden in free-text comments | **Blocker reason + since-date** captured as a field; blocked list with age | Nominations | 🔴 | ✅ |
| 4 | Key SMEs overloaded across multiple accounts | Capacity heatmap + utilization bands + overload alert | Capacity | 🔴 | ✅ |
| 5 | No centralized account-to-resource / workload visibility | Resource → active accounts → nominations → utilization view | Capacity / Resources | 🔴 | ✅ |
| 6 | Manual App ↔ DB ↔ related-wave linkage → wrong reporting **(stated FDO governance requirement)** | **Wave linkage model** (Account → Nomination → App / DB / Security / Landing Zone / Dispatch waves) | Accounts / Nominations | 🔴 | ✅ |
| 7 | Leave via email + spreadsheet; coverage gaps | **Leave-clash alert** — resource with active accounts has upcoming leave | Leave / Capacity | 🔴 | ✅ |
| 8 | Free-text entry → inconsistent **tool names** (biggest), skills | Controlled vocabularies: **tool/skill picklists** (Segment pattern) | Configuration | 🟠 | ✅ |
| 9 | Onboarding / access management at scale | Onboarding **status** per resource (access requested/granted/trained) | Resources | ⚪ | ✅ |
| 10 | Coverage concentrated in few specialists (SPOF) | Strategic accounts coverage + **backup owner** on account | Strategic / Accounts | 🟠 | ✅ |

---

## 2. Feature backlog (grouped, with effort)

Effort is relative: **S** ≤ ½ day · **M** ~1 day · **L** 2–3 days.

### Phase 1 — Mandatory (close the biggest daily-tracking gaps)

| ID | Feature | Priority | Effort | Notes |
|----|---------|----------|--------|-------|
| P1-01 | Explicit **Blocked** status + **blocker reason** (enum) + **blocked-since** date on nomination | 🔴 | M | Reason enum from governance (see §3). Adds `BlockedReason`, `BlockedSince`, `FollowUpDate`. |
| P1-02 | **Tiered stale detection** — Warn @ Day 3 · Escalate @ Day 5 · Suggest *Customer Deferred* @ Day 10 (thresholds configurable) | 🔴 | M | Matches documented FDO cadence. Uses existing `updatedAt`; age badge + Needs-update filter. |
| P1-03 | **Blocked / aging dashboard** on Nominations (blocked count, avg age, oldest, by reason) | 🔴 | S | Extends existing stage summary cards. |
| P1-04 | **Leave-clash alert** — flag resources with active accounts + upcoming leave in window | 🔴 | M | Join Leave + Capacity; surface on Capacity page and a KPI. |
| P1-05 | **Overload alert surfacing** — explicit "Overloaded" list/badge from capacity engine | 🔴 | S | Engine already computes utilization; expose as an at-a-glance alert. |
| P1-06 | **Wave linkage model** — link Nomination/Account to App / DB / Security / Landing Zone / Dispatch waves | 🔴 | L | **Promoted from Recommended** — stated FDO governance requirement (App–DB linkage enforcement). |

### Phase 2 — Recommended (data quality + ownership clarity)

| ID | Feature | Priority | Effort | Notes |
|----|---------|----------|--------|-------|
| P2-01 | **Ownership matrix** on account — PM, SA, CFTL, Account Owner, Customer POC (+ **Backup Owner**) | 🟠 | M | PM/SA/CFTL/POC reflect current FDO fields; Backup Owner is the new enhancement for SPOF/handover. |
| P2-03 | **Tool/skill picklists** in Configuration (kill free-text drift; tool names are the worst offender) | 🟠 | M | Mirror the Segment pattern (list + add/rename). |
| P2-04 | **Handover history** on account (who owned it, when) | 🟠 | M | Append-only ownership log. |

### Phase 3 — Optional (nice-to-have / partly out of scope)

| ID | Feature | Priority | Effort | Notes |
|----|---------|----------|--------|-------|
| P3-01 | Onboarding **status tracker** per resource (access granted / trained) | ⚪ | M | Portal tracks status only; access provisioning stays external. |
| P3-02 | Automated reminders / email digest of stale + blocked items | ⚪ | L | Needs mail integration; revisit after Phase 1 proves the data. |
| P3-03 | Comment-mining to auto-suggest Blocked state | ⚪ | L | AI/NLP; only after explicit Blocked field (P1-01) exists. |

---

## 3. Data-model changes (summary)

- **Nomination**
  - `Status` — align to current FDO values: `In Progress`, `Blocked`, `Waiting for Customer Action`, `Customer Deferred`, `Waiting on Follow-up Date`, `Completed/Done`.
  - `BlockedReason` (enum, required when Blocked): **Waiting for Customer Action · Approval Pending · Access Pending · Landing Zone Pending · Testing/Validation Pending · Dependency Pending · Budget/Priority Hold · Internal Alignment**.
  - `BlockedSince` (date), `FollowUpDate` (date); staleness derived from existing `UpdatedAt`.
- **Wave linkage** (new): a `WaveLink` relationship joining a Nomination/Account to related waves — types **App · DB · Security/Defender · Landing Zone · Dispatch · Related workload**.
- **Account**: ownership fields `ProjectManager`, `SolutionArchitect`, `Cftl`, `AccountOwner`, `CustomerPoc`, plus new `BackupOwner`; optional `OwnershipHistory`.
- **Configuration**: picklists `ToolConfiguration`, `SkillConfiguration` (Segment pattern); `ApplicationSetting` keys for **stale tiers** (`StaleWarnDays=3`, `StaleEscalateDays=5`, `StaleDeferDays=10`) and **leave-clash window days** — all editable in the Configuration page.

Each change ships with an EF Core migration (same flow as `AddSegments`).

---

## 4. Delivery approach

1. Build **Phase 1** end-to-end (mandatory, now incl. wave linkage P1-06), verify against real data, demo.
2. Confirm value, then decide Phase 2 scope (Recommended).
3. Treat Phase 3 as backlog; only P3-02 (reminders) and P3-03 (comment-mining) need new infra.

**Recommendation:** start with **Phase 1** — it addresses pain points #1–#7 (the weekly/high-impact
ones plus the governance-required linkage). P1-02/03/05 have no dependencies and can start now;
P1-01 and P1-06 use the confirmed enums/wave types below.

---

## 5. Confirmed decisions (MS 365 Copilot, evidence-backed)

| Topic | Decision |
|-------|----------|
| Blocker reasons | 8-value enum above (Customer action most common; Access & Landing Zone next). |
| Stale cadence | Day 3 warn → Day 5 escalate → Day 10 suggest *Customer Deferred* + set follow-up date. Configurable. |
| Status values | In Progress · Blocked · Waiting for Customer Action · Customer Deferred · Waiting on Follow-up Date · Completed/Done. |
| App↔DB↔Wave linkage | **Mandatory** governance requirement → Phase 1 (P1-06). Wave types: App, DB, Security/Defender, Landing Zone, Dispatch, Related. |
| Leave notice window | No mandated advance period found → keep configurable (default e.g. 14 days); still require tracker update + PM/Lead notify. |
| Ownership fields | Current FDO: PM, SA, CFTL, Account Owner, Customer POC. **Backup Owner** is a new enhancement (Phase 2). |
| Free-text drift | Worst offender = **tool names**; also skills. Replace with controlled dropdowns (Phase 2 P2-03). |

---

## 6. Phase 4 — Effectiveness enhancements (make it a daily tool)

A holistic review of the shipped MVP found the governance features are complete, but the layer
that turns "data tables" into "a tool people open every morning" is missing. Phase 4 closes that
gap. Priority: 🔴 Now · 🟠 Next · ⚪ Later.

| ID | Enhancement | Why it matters | Priority | Status |
|----|-------------|----------------|----------|--------|
| P4-01 | **Data-freshness banner** — header shows last-refresh time + row counts, driven by a new `GET /api/admin/status`; stamp `LastDataRefreshUtc` on every import | Users can't tell if data is stale; imports run server-side with no visible signal | 🔴 | ✅ |
| P4-02 | **Excel export** — `GET /api/export/{resources\|capacity\|nominations\|performance\|summary}` (ClosedXML) + export buttons on each grid, plus a one-click **Executive summary** workbook | "Executive reporting" is a stated goal; leads need a shareable snapshot | 🔴 | ✅ |
| P4-03 | **Dashboard drill-through** — KPI cards and chart segments navigate to the relevant page with the filter pre-applied (Overloaded → Capacity, Open Nominations → Nominations, On Leave → Leave…) | Dashboard was a dead-end snapshot; this makes it the entry point | 🔴 | ✅ |
| P4-04 | **Capacity heatmap** — resource × utilization-band grid (green/amber/red), spec'd but delivered only as a table | At-a-glance overload/SPOF visibility | 🟠 | ✅ |
| P4-05 | **Leave intake that gets used** — in-app quick-add is present, but the imported dataset has 0 leave rows, so On-Leave KPI + clash alerts always read zero; wire `LeaveCal.xlsx` loader / CSV upload | Coverage-planning features are inert without leave data | 🟠 | ☐ |
| P4-06 | **Trends over time** — periodic snapshots (utilization, blocked-age, avg performance) + trend charts | Shows *direction*, not just *state* — the management value | 🟠 | ☐ |
| P4-07 | **"My view"** — a lead's personalized landing filtered to their region/reports with only their action items (stale noms, clashes, training needs) | Turns the portal into a personal worklist | 🟠 | ☐ |
| P4-08 | **Global search** — top-bar people/account/nomination lookup | Faster navigation across 78 resources / 262 accounts | ⚪ | ☐ |
| P4-09 | **Entra ID auth** (spec Phase 2) — protect performance scores + governance data on a public App Service URL | Security before wider rollout | ⚪ | ☐ |
| P4-10 | **In-app import upload** — replace the server-file dependency with a file picker | Self-service refresh without server access | ⚪ | ✅ |
| P4-11 | **API smoke tests** — capacity math, stale tiers, clash window | No automated tests today | ⚪ | ☐ |
| P4-12 | **Repo hygiene** — remove stray root logs; confirm `*.log` ignored | Noise / minor leak risk | 🔴 | ✅ |

**This iteration ships P4-01, P4-02, P4-03, P4-04, P4-12.** P4-05→P4-11 remain backlog: they need a
data decision (leave source), new infra (auth, snapshots background job), or product scoping (my-view),
and are best done as follow-ups once the cockpit changes prove out.

## 7. Pending / to verify (known caveats)

| # | Item | Why it matters | Status |
|---|------|----------------|--------|
| PV-01 | **SLA-breach age semantics vs FDO** — the per-stage day-count in `Detail View.xlsx` ("N days" in the stage column) may **only start accruing after an SLA breach**, not from stage entry. Our current logic treats it as elapsed days-in-stage and derives Warn/Escalate/Defer directly from it. Confirm the FDO source definition; if the day-count is post-breach, the Warn/Escalate/Defer thresholds and the "Age in stage" label likely need re-mapping. | Age drives SLA tier, the SLA-breach filter, and the recommended-action cadence — a wrong basis mis-flags nominations. | ☐ Pending — verify against FDO, then correct `NominationService.StaleTier` / stage-age basis if needed |

> **Note:** `StageAgeDays` is imported per current stage and `StaleTier` is gated to Stage 2–4. Once the
> FDO day-count definition is confirmed, revisit whether age = stage-entry elapsed or post-breach elapsed,
> and adjust thresholds/labels accordingly.

## 8. Phase 5 — Recurring FDO ingestion (the app becomes the system of record)

**Problem.** FDO data is dropped periodically (weekly). The original import **wiped and re-inserted**
nominations on every refresh, which destroyed all portal-owned governance data — blocker reason,
blocked-since, follow-up date, manual status, and wave links — and cascade-deleted `WaveLink` rows.
That is fatal for a recurring drop: every refresh erased the operational context leads had entered.

**Approach.** Split responsibility per field. FDO owns the pipeline facts; the portal owns the
governance/operational overlay. On each drop we **upsert-merge** (never wipe), keyed on the FDO
**Task Id**, and reconcile anything missing without hard-deleting.

### Field-ownership matrix

| FDO-owned (refreshed every drop) | Portal-owned (preserved across drops) |
|----------------------------------|----------------------------------------|
| Account name, TPID, Region | `Status` (manual workflow state) |
| Offering / phase | `BlockedReason`, `BlockedSince` |
| Nominated date | `FollowUpDate` |
| Migration Status → Stage (1–4) | `WaveLinks` (App/Security wave references) |
| Current State | Remarks/notes (seeded from FDO once, then portal-owned) |
| `StageAgeDays` (per-stage day-count) | Ownership history (future) |
| PM / CFTL / SA | |

### Sub-phases

| ID | Sub-phase | What | Status |
|----|-----------|------|--------|
| **A** | **Upsert-merge correctness** | Add `ExternalTaskId` (FDO "Task Id") as the stable key + index. Import matches by Task Id (falls back to `AccountId`+`Offering` to adopt legacy rows once), refreshes FDO-owned fields, and **preserves** portal-owned fields. Rows absent from a drop are **soft-withdrawn** (`NominationStatusType.Withdrawn`), never hard-deleted, so history + waves survive. `DataRefreshService` no longer `ExecuteDeleteAsync` on nominations. | ✅ **Done & verified** |
| **B** | **In-app upload + dry-run preview** | File-picker upload (P4-10) with a preview showing Added / Updated / Unchanged / Missing counts **before** commit. | ◠ **Upload done** (header “Upload FDO” → `POST /api/admin/upload?kind=nominations`, atomic temp-swap then upsert-refresh). Dry-run preview still ☐ |
| **C** | **Import audit + retention** | `ImportRun` table (id, startedUtc, source, counts) + optional `ImportChange` delta rows; prune history older than `ImportHistoryRetentionDays` (default **10**, keep 7–15) to stay small on SQLite/F1. | ✅ **Done** — `ImportRun` + `ImportChange` record Added/Updated(field-level)/Withdrawn per upload; **History** page (`/history`) lists runs date-wise with drill-in to changes; migration `AddImportHistory`; retention prune at 90 days (history deltas only — nominations are never hard-deleted). |
| **D** | **Trend snapshots** | Periodic snapshots of utilization / blocked-age / stage-age for trend charts (P4-06), fed by the same ingestion. | ☐ |

### Phase A — verification (done)

Ran two consecutive refreshes with a portal edit in between:
- Count stable **147 → 147**, **zero duplicate ids**, all **147 TPIDs** present.
- A nomination's `Status=Blocked`, `BlockedReason=Access Pending`, `FollowUpDate`, and a `WaveLink`
  (`WAVE-TEST-001`) **all survived** the second refresh — proving portal edits are no longer wiped.
- New `Withdrawn = 8` status excluded from the SLA stale cadence.
- Migration `AddNominationExternalTaskId` applied.

### Change detection (Phase B/C design)

Compute a content-hash of the FDO-owned fields per Task Id → classify **Added / Updated / Unchanged /
Missing**. Missing → soft-withdraw. Store **deltas only** in `ImportChange` (entityType, externalKey,
changeType, changed-fields JSON) to keep the audit tiny; prune beyond the retention window.

> **Follow-up (out of Phase A scope):** `ResourceAccounts`, `LeaveFacts`, and `EngagementFacts` still
> use wipe-and-rebuild on refresh. That's safe today (no portal edits on those, and no `LeaveCal.xlsx`
> exists so leave isn't wiped), but if leave becomes portal-entered (P4-05) the `LeaveFacts` delete
> must move to the same upsert-merge model before enabling it.
