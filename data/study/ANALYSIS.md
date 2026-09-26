# CAF Portal — Study of the FDO Factory Scorecard exports

Analysis of the seven workbooks dropped in `data/study/`. Goal: understand what the
**official FDO App-Modernization Factory dashboard** measures, reconcile it against what the
portal already computes, and turn the gaps into a prioritised backlog.

> Source of all seven files: the FDO Power BI report **"NNR ACR (Landed in Fiscal)"**, scoped to
> `Solution Play = "1. Modernize with Confidence"` + `Factory Name = "App Modernization Nominations"`.
> This is the same programme the portal tracks — so these are the **canonical KPIs** our Analytics
> page should reproduce.

---

## 1. What each file actually contains

Six of the seven are **Power BI *visual* exports that only serialised their filter caption** — the
chart data did not come through. They are still valuable: the **file names + captions define the KPI
vocabulary** the business reports on. Only `data.xlsx` carries real tabular data.

| File | Real data? | What it represents |
|------|-----------|--------------------|
| `data.xlsx` | ✅ full pivot | **Factory attainment scorecard** — Target vs In-flight vs Completed ACR by fiscal month, with a "Nominations Needed" projection. The heart of the pack. |
| `Factory Target Vs. Approved In-Flight and Completed.xlsx` | caption only | The **visual** built on `data.xlsx` — cumulative Target line vs Approved-in-flight vs Completed ACR. |
| `NNR ACR (Landed in Fiscal).xlsx` | caption only | Headline measure: **Net-New-Revenue ACR that *completes/lands* within the current fiscal year**. |
| `Annualized ACR.xlsx` | caption only | ACR normalised to an annualised run-rate (a second ACR lens next to NNR-landed). |
| `Concierge Nomination Status ACR.xlsx` | caption only | **ACR summed by approval step**: Active Concierge · Provisional Approved · Approved · Decline · Cancelled – Customer/Field. |
| `Active and Approved Pipeline.xlsx` | caption only | Pipeline funnel by step: Active Concierge ACR · Pipeline Total · Attained · Remaining Variance · Target ACR. |
| `Velocity (Avg. No. of Business Days  Target Days).xlsx` | caption only | **Cycle time**: average business days in stage vs the per-stage **target days**. |

### KPI vocabulary extracted from the captions- **ACR states** (approval steps): `Active Concierge` · `Provisional Approved` · `Approved` · `Decline` · `Cancelled – Customer / Field`.
- **Funnel measures**: `Pipeline Total` · `Attained` · `Remaining Variance` · `Target ACR`.
- **Headline ACR measure**: `NNR ACR (Landed in Fiscal)` and a parallel `Annualized ACR`.
- **Velocity**: `Avg. No. of Business Days` vs `Target Days` (per stage).

---

## 2. `data.xlsx` decoded (the scorecard)

Fiscal-month rows **Jul → Jun** (confirms FY starts **Jul**, matching our `FiscalCalendar`), plus a `Total`.
All ACR columns are **cumulative** (running totals across the fiscal year).

| Column | Meaning | Reverse-engineered formula (verified all 12 months) |
|--------|---------|------------------------------------------------------|
| `Target` | Cumulative ACR plan curve | Linear early (**305,545.21 / month** Jul–Sep), then accelerates; **FY total ≈ 26.96M**. |
| `Inflight Approved ACR` | Cumulative approved-but-not-completed ACR | source measure |
| `Completed Approved ACR` | Cumulative completed ACR | frozen at **422,353** after Sep (= current month; nothing completes in the future) |
| `VTT` | **Velocity-to-Target** (per the glossary; the ACR gap to plan) | **`VTT = Target − (Completed + Inflight)`** ✔ (positive = behind plan; negative = ahead) |
| `Total Approved Nomination` | Per-month approved-nomination **count** | not cumulative; **FY sum = 340** |
| `Completed Approved Nominations` | Per-month completed **count** | **78 through Sep** this fiscal year |
| `Avg Nomination Size` | ACR per completed nom | **`Completed Approved ACR ÷ Completed Approved Nominations`** (`Infinity` when 0 completed) |
| `Nominations Needed` | Noms to close the gap | **`VTT ÷ Avg Nomination Size`** |

**Snapshot (as of Sep / FY27 Q1):** cumulative Target 916,636 · Completed 422,353 · In-flight 46,522 →
**VTT ≈ +447,760 behind plan**, i.e. ~35 more average-size nominations needed to catch up.

---

## 3. Reconciliation with the portal (important)

- **"Completed" scope differs.** The FDO scorecard's `Completed Approved Nominations` = **78** — that's
  **completed *and landed in the current fiscal year***. The portal's all-time completed is **356**
  (every row with an `Actual End Date`, across FY24–FY27). **Neither is wrong** — they answer different
  questions. Our Analytics "Completed" KPI is **all-time**; the FDO number is **FY-to-date landed**.
  → We should be able to reproduce 78 with **Trends: basis=completed · measure=count · fy=2027** (sum of
  the FY27 buckets). Worth validating as a correctness check.
- **NNR ACR (Landed in Fiscal)** ≈ our **Trends: basis=completed · measure=nnr · fy=current** total.
  We already have the ingredients; we just don't *label* it that way.
- **Approval steps already map** to `Nomination.ApprovalStatus` (Approved / Declined / Provisionally
  Approved / Active Concierge). The one we may not distinguish is **`Cancelled – Customer / Field`**
  (we fold Declined → `Withdrawn`); check whether Cancelled is a separate FDO state we drop.

---

## 4. Gaps → prioritised backlog

### P1 — Factory Attainment scorecard (the biggest missing view)
Reproduce `data.xlsx` as a first-class portal view: cumulative **Target ACR** curve vs **Completed** and
**In-flight** ACR by fiscal month, with **VTT** and **Nominations Needed**. We already have completed/
in-flight ACR by fiscal month (Trends). The **only missing input is the monthly ACR *Target*** — needs a
source (annual FY target + a spread rule, or an imported target column). Surface as either:
- a new **Attainment** section on the Analytics page (target line overlaid on the ACR trend), or
- extra columns on the existing Trends table when `measure=acr`.
**Open question:** where does the Target curve come from? (`305,545.21/mo` early, `~26.96M` FY total — is
that a flat annual target spread, or an imported plan?)

### P2 — Velocity / cycle-time (also closes PV-01)
The `Velocity` file confirms the business tracks **avg business days in stage vs target days per stage** —
exactly the `StageTargetDays1..4` (10/10/5/23) we already store but currently only display. Build a
**Velocity view**: avg days-in-stage vs target, per stage 1–4, and finally wire a **cycle-time measure**
into Trends (avg `nominated → completed` business days). This is the long-standing PV-01 item.
**Note:** "business days" (excl. weekends/holidays) ≠ our calendar `StageAgeDays` — decide whether to
match the FDO business-day basis.

### P3 — ACR-by-approval-status distribution (quick win)
`Concierge Nomination Status ACR` = **ACR summed by approval step**. Our Analytics distributions are all
**count**-based; add an **ACR-by-approval-status** cut (Active Concierge / Provisional / Approved /
Decline / Cancelled). Cheap — the data is already on each nomination.

### P4 — Label the NNR-landed-in-fiscal number
Add an explicit **"NNR ACR — landed in FY{n}"** KPI (basis=completed · nnr · current FY) so the portal
headline matches the FDO headline verbatim, instead of the all-time ACR we show now.

### P5 — Annualized ACR lens
Understand FDO's annualisation rule and, if useful, add it as a second ACR measure. Lower priority until
the definition is confirmed.

### Data-model note
- Confirm whether **`Cancelled – Customer / Field`** is a distinct FDO state we currently collapse into
  `Withdrawn`; if leads report on it, preserve it as its own `ApprovalStatus`/`Status`.

---

## 5. Open questions for the user
1. **Target curve source** — is the monthly ACR `Target` an imported plan, or annual-target ÷ spread?
   Without it we can show attainment shape but not the official VTT.
2. **Business days vs calendar days** — should Velocity/age use business days (FDO basis) or our current
   calendar `StageAgeDays`? This is the crux of **PV-01**.
3. **Completed scope** — do you want the portal "Completed" KPI to stay **all-time**, or switch the
   headline to **FY-landed** (78) to match FDO, keeping all-time as a secondary number?
4. **Re-export the six visuals** — the chart data didn't serialise (only captions did). If you can export
   them as *tables/CSV* (or share the underlying query), we can validate our reproductions against the
   exact FDO numbers instead of approximating.

---

*Derived from the seven `data/study/*.xlsx` files; `data.xlsx` formulas verified numerically across all
12 fiscal months. No portal code changed by this study.*
