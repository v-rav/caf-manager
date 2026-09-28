# CAF Operations Portal — Audit & Roadmap

_Last updated: 2026-09-28. Living status doc — see `AGENTS.md` for the engineering guide and
`FACTORY-OPERATING-SYSTEM.md` for the governance blueprint._

## 1. Delivery status
The portal is a **Factory Operating System** on top of the EMEA visibility portal. Phases **P1–P5 shipped**
(custom auth · 8-gate SA Workspace · blockers + clock-aware SLA · audit timeline · classification + Strategic
Register · GHCP adoption · MSI · Migration Flow · 4-view Executive Dashboard), plus the workspace-mock-aligned
redesign, the editable **lookup master**, **milestones & dates**, the **ACR rate master + estimator**, bulk **SA
login provisioning**, an SA **My-nominations** view, and a **global Fiscal-Year scope**.

## 2. Page inventory (18 routes)
| Route | Page | Design | Notes |
|-------|------|--------|-------|
| `/` | Executive Dashboard (4 tabs) | U2 | Leadership · Operational · Adoption · Productivity |
| `/analytics` | Migration Analytics + Trends + Attainment | — | own FY/Trends controls |
| `/resources` | Resource Hub | — | filters: role/capacity/active |
| `/accounts` | Account Hub | — | filters: segment/status/TPID |
| `/capacity` | Capacity | — | heat-band, filter: status |
| `/reconciliation` | Reconciliation | — | filters: match/utilization |
| `/leave` | Leave | — | |
| `/nominations` | Nomination Pipeline | U1 | 7 filters + SA My-nominations + FY |
| `/nominations/:id` | SA Workspace | F1 | gates · blockers · milestones · timeline · MSI |
| `/governance` | Governance Board | F2 | open blockers |
| `/strategic` | Strategic Investment Register | F3 | classification-driven |
| `/adoption` | GHCP Adoption | F4 | 0–7 maturity |
| `/flow` | Migration Flow | F6 | funnel + bottleneck |
| `/performance` | Performance | — | reviews |
| `/history` | Import History | — | import deltas |
| `/configuration` | Configuration | U3 | capacity · vocab · lookup master · ACR rates · fiscal targets |
| `/backup` | Backup & Restore | — | ⚠ unauthenticated (to gate) |
| `/workspace` | SA Workspace (P0 mock) | P0 | reference only (kept) |

## 3. Consistency
- **Region scope** — all data pages honor `useRegion()`.
- **FY scope** — Nominations, Dashboard, Strategic, Adoption, Flow honor `nominationInFy(n, fy)` (every nomination
  belongs to one FY: settled by close date, in-flight by nominated date). Analytics keeps its own FY controls;
  Governance Board (open blockers) is inherently current.
- Shared calculators (`nominationInFy`, `MsiCalculator`, capacity engine) keep overlapping views agreeing.

## 4. Overlaps (intentional)
SA workload (Flow + Dashboard), stage distribution (Flow + Dashboard + Nominations), GHCP adoption (page + Dashboard
tab), blockers (Board + Flow + Workspace), ACR (Analytics + Dashboard). Different altitudes, consistent numbers — no
contradictory duplication.

## 5. Recommendations & decisions (2026-09-28)
| # | Recommendation | Decision |
|---|----------------|----------|
| 1 | Enforce **page-level role access** | ✅ **Do it — but Admin sees all pages/nominations/data** |
| 2 | Retire `/workspace` P0 mock | ⏸ **Not yet** (keep as reference) |
| 3 | `NominationOutcome` value-capture | ⏸ **Park for later** |
| 4 | Add exports + light filters to Adoption/Strategic/Flow | ✅ Yes |
| 5 | Gate Backup/Restore behind Admin + prune `prerestore_*` | ✅ Yes |
| 6 | Surface milestone lag (kick-off → actual-start) on grid/analytics | ✅ Yes |
| + | **Separate User & Access page** with role-based page-access provisioning | ✅ New requirement |

## 6. Backlog (open)
- **Role-based page access** (in progress) — config-driven, Admin bypass.
- `NominationOutcome` value-capture (hours saved · defects · CSAT → Factory Productivity). *(parked)*
- Config-editable strategic time-thresholds + PV-01 stale-basis decision.
- ACR estimator on Analytics + per-nomination "estimate ACR". *(parked)*
- Backend role enforcement (frontend gating first), Entra auth, in-app upload, API smoke tests.
- `ResourceAccounts`/`LeaveFacts`/`EngagementFacts` wipe-and-rebuild fix before portal-entered leave.

## 7. Restore points (git tags)
| Tag | Marks |
|-----|-------|
| `v1.0-fos` | FOS P1–P5 + lookup master + milestones + ACR master + SA provisioning + FY scope (pre role-access) |
