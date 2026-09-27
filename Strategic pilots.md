Leadership Brief: Strategic Pilot Engagements and Their Impact on Factory Velocity
Executive Summary

Over the past several months, App Factory has supported strategic pilot engagements for key enterprise customers where the objective extended beyond migration execution and focused on establishing customer confidence, validating the Factory delivery model, and unlocking larger transformation opportunities.

While these engagements provide significant strategic value, they are currently governed using the same operating model and success metrics as standard factory nominations. As a result, velocity, utilization, and SLA metrics do not accurately reflect the true business context or investment being made.

Current Challenge

Strategic pilots differ fundamentally from standard migrations:

Pilot applications are often larger, more complex, and highly visible to customer leadership.
Dedicated architects, SMEs, and engineers remain engaged for extended periods to support customer validation, defect remediation, design discussions, and confidence-building activities.
Customer-led reviews, testing cycles, and change requests frequently extend beyond original delivery timelines.
Resources that would normally support multiple nominations become concentrated on a small number of strategic accounts.

This creates a situation where projects remain active for many months despite substantial progress and delivery effort, reducing overall factory throughput.

Business Impact
Positive Outcomes
Strengthens executive customer trust.
Demonstrates App Factory modernization capabilities.
Creates opportunities for larger migration portfolios.
Establishes strategic lighthouse references for future engagements.
Operational Impact
Reduced capacity for new nominations.
Lower apparent factory velocity.
Extended resource dedication on limited accounts.
Increased pressure on shared specialist skillsets.
Difficulty distinguishing genuine delivery issues from intentional strategic investments.
Recommended Governance Changes
1. Introduce a New Project Classification

All nominations should be categorized as:

Standard Factory Delivery
Strategic Pilot
Lighthouse Engagement
Innovation / POC
Recovery Engagement

Strategic Pilots should be measured separately from standard factory executions.

2. Define Mandatory Exit Criteria

Every pilot must have agreed closure conditions before work begins.

A pilot should exit when:

Migration objectives are delivered.
Customer validation is completed.
Documentation and handover are finished.
Hypercare commitments are met.

Open-ended support and enhancement activities should not indefinitely extend pilot status.

3. Establish Time-Based Governance
Duration	Governance Action0–60 Days	Standard monitoring
61–90 Days	Leadership review
91–120 Days	CFTL review and extension justification
>120 Days	Executive decision required

Beyond 120 days, leadership should decide whether to:

Scale the engagement,
Transition to a funded delivery model,
Move ownership to the customer,
Or formally close the pilot.
4. Track Strategic Investment Separately

Create a Strategic Investment Register capturing:

Account
Strategic objective
Dedicated resources
Duration
Velocity impact
Future migration potential
Business outcome achieved

This will provide visibility into where factory capacity is being intentionally invested.

Additional Metrics Recommended

Alongside standard delivery health, track:

Metric	PurposeStrategic Pilot Flag	Identify investment engagements
Dedicated Resource Count	Measure resource commitment
Velocity Impact Score	Quantify effect on factory throughput
Opportunity Pipeline	Measure expected future value
Pilot Age	Highlight prolonged engagements
Exit Criteria Status	Track readiness for closure
Leadership Ask

Approve the introduction of a dedicated Strategic Pilot Governance Model that:

Separates strategic investments from normal factory deliveries.
Establishes formal entry and exit criteria.
Introduces time-bound executive reviews.
Tracks resource and velocity impact transparently.
Enables leadership to balance short-term delivery throughput with long-term strategic account growth.

Key Message: Not all slow-moving engagements indicate delivery inefficiency. Some represent deliberate strategic investments made to secure larger customer transformation opportunities. These engagements require their own governance framework to ensure both customer success and sustainable factory operations.



----------



Strategic Pilot Governance – Executive Decision Brief
Executive Summary

We are seeing a recurring pattern with strategic accounts such as SocGen (SGMR) and Air France (COMET), where pilot modernization engagements were intentionally committed to build customer confidence and unlock a broader migration portfolio. In both cases, dedicated Factory resources have been engaged for extended periods, resulting in reduced throughput for other nominations and a measurable impact on overall Factory velocity.

The challenge is that these engagements are currently governed and reported the same way as standard Factory deliveries, making strategic investments appear as delivery inefficiencies.

What We Are Observing
SocGen (SGMR)
Pilot/POC engagement intended to establish trust and demonstrate Factory capabilities.
Dedicated SMEs and architects assigned over an extended period.
Ongoing discussions around scope alignment, governance, and exit criteria.
Resources remained committed beyond typical Factory delivery timelines.
Air France (COMET)
Strategic pilot intended to create customer confidence for future modernization work.
Significant effort invested in scope alignment, technical discussions, validations, and customer milestones.
Extended engagement has consumed dedicated Factory capacity and impacted velocity of other nominations.
Business Reality

These projects are not blocked.

They are:

Strategic investments
Confidence-building engagements
Future pipeline enablers
Resource-intensive pilot programs

Current reporting does not distinguish these from normal in-flight projects.

Impact
Area	ImpactFactory Velocity	Reduced capacity for new nominations
Resource Utilization	SMEs and Architects dedicated for long durations
SLA Metrics	Appear degraded despite strategic intent
Leadership Visibility	No clear distinction between delivery issues and strategic investments
Forecasting	Difficulty calculating true available capacity
Decisions Requested
Decision 1: Create a New Project Classification

Approve a new project type:

Strategic Pilot

Examples:

SocGen SGMR
Air France COMET
Future lighthouse or confidence-building engagements
Decision 2: Establish Pilot Time Limits

Approve governance thresholds:

Duration	Action0–60 Days	Standard execution
61–90 Days	Leadership review
91–120 Days	CFTL approval required for continuation
>120 Days	Executive decision required
Decision 3: Mandate Exit Criteria

Every strategic pilot must define before execution:

Technical success criteria
Customer validation criteria
Handover requirements
Pilot closure criteria
Future portfolio expansion decision point

No pilot should remain open indefinitely due to additional enhancements, discussions, or customer requests.

Decision 4: Track Strategic Investment Separately

Create a Strategic Investment Register capturing:

Account
Strategic objective
Resource commitment
Duration
Velocity impact
Future migration opportunity unlocked

This enables visibility into where Factory capacity is being intentionally invested.

Executive Recommendation

Approve a dedicated Strategic Pilot Governance Model that separates strategic customer investments from standard Factory deliveries.

This will:

Improve visibility into true Factory velocity.
Protect capacity planning and utilization reporting.
Ensure pilots have defined exit criteria and timelines.
Enable leadership to make informed investment decisions on high-value strategic accounts such as SocGen and Air France.

Key Message:
 Not every long-running project is a delivery problem. Some are deliberate strategic investments to secure larger customer transformation opportunities. These engagements require a different governance model than standard Factory delivery.




 -------

 Fair — let me make the UI and the SA's day-to-day concrete. Short answer: **yes, two new SA-facing pages** (plus small tweaks to the existing grid). Here's exactly what they are and how an SA works in them.

## New page 1 — **Nomination Workspace** (`/nominations/:id`) — the SA's cockpit
Clicking an account in the Nominations grid opens a **full page** (not the cramped Manage modal). This is where the SA drives one nomination end-to-end.

```
┌ SocGen · SGMR  ·  TPID 12345 ────────[Strategic Pilot]──[FDO Stage 3]──[Age 84d · 32d clock-stopped]─┐
│ Readiness Compliance ◕ 62%     PM: A.Rana   CFTL: …   SA: you            [＋ Raise blocker] [Advance ▸] │
├───────────────┬─────────────────────────────────────────────────────────────┬──────────────────────┤
│  GATE STEPPER │  G4 · Scope Governance  (weight 20%)                          │  ▸ Blockers (1 open) │
│  ● G1 Discovery  100%   │  Exit: signed scope · no ownership ambiguity · FDO updated       │  Awaiting Customer   │
│  ● G2 Prereq     100%   │  ┌──────────────────────────────────────────────────────────┐  │  Approval · ⏸ clock  │
│  ● G3 Assess     100%   │  │ ☑ Scope document created      Deliverable·done·[link] 📎 │  │  stopped · since 20  │
│  ◐ G4 Scope  ◀   60%    │  │ ☑ In-scope defined            Task · done                │  │  Aug · owner: Cust.  │
│  ○ G5 Arch        0%    │  │ ☐ Out-of-scope defined        Task · Pending             │  │  [Resolve]           │
│  ○ G6 Deliv-Rdy   0%    │  │ ☐ Customer responsibilities   Task · ⛔ blocked           │  ├──────────────────────┤
│  ○ G7 Delivery    0%    │  │ ☐ Signed scope doc ⚑mandatory Signoff · Pending          │  │  ▸ Timeline (log)    │
│  ○ G8 Closure     0%    │  └──────────────────────────────────────────────────────────┘  │  ▸ Tools / Activities│
└───────────────┴─────────────────────────────────────────────────────────────┴──────────────────────┘
```

**Regions:** header (identity + classification + clock-aware age + compliance ring + actions), left **8-gate stepper** (status dot + % per gate), center **current-gate checklist**, right **Blockers / Timeline / Tools** tabs.

## How the SA interacts — click by click
1. **Open** → lands on the **current gate**, with pending items highlighted. No hunting — the guide tells them what's next.
2. **Tick an item** — toggle `Pending → Done`, paste a doc link (e.g., the Scope Document URL), optional note. **Autosaves** → writes a `NominationGateItem` row + a Timeline event. Compliance ring updates live.
3. **Hit a wall** — click ⛔ on the stuck item → a modal: pick **blocker category** (Awaiting Customer Approval…), **owner**, **expected resolution date**, and **"SLA clock stopped?"** toggle → creates a `NominationBlocker` linked to that item. The gate shows *blocked*; if clock-stopped, the age timer pauses.
4. **Advance** — when the gate's mandatory items are Done + exit criteria met, **"Advance ▸"** turns green. Soft-gated: if items remain, it warns *"3 items pending — advance anyway?"* rather than blocking.
5. **Resolve blocker** — right rail → "Resolve" (stamps resolution date) → clock resumes.
6. **At closure** — G8 shows an **outcome capture** panel (hours saved, CSAT, ACR influenced).
7. Every action is logged in the **Timeline** (who/when/old→new) — the audit + forensic trail.

## New page 2 — **My Work** (SA landing / `/my-work`) — the daily to-do
Because an SA runs *many* nominations, they need a **cross-nomination pending list**, not to open each one:
- **KPIs:** my active nominations · my pending gate items · my open blockers · my avg compliance %.
- **"Pending items" table:** every open item across all my nominations, sorted by gate weight / age, each linking straight into that nomination's Workspace at the right gate.
- **"My blockers"** and **"gates about to breach SLA."**

This is what an SA opens each morning — it literally answers *"what do I need to do, what's pending."*

## Existing grid — small additions (not a new page)
`/nominations` gains columns: **Current Gate**, **Readiness %**, a **blocker chip**, and a **⏸ clock-stopped** icon — so the list view reflects governance at a glance. The quick **Manage modal stays** for one-field tweaks; the **Workspace** is for real driving.

## Why a page, not a modal
The 8-gate checklist + blockers + timeline is far too much for a dialog. A full page also lets the SA live in it during customer calls and update in real time. (One-tap item toggles keep meeting-time updates fast.)

---
So: **Workspace (drive one nomination)** + **My Work (see all my pending)** are the two new SA surfaces; leadership pages (Governance Board, Compliance, Adoption, Strategic Register) are read-mostly.

Want me to **fold this "UI & SA interaction" detail into §7 of the blueprint**, and/or build a **clickable static mock of the Workspace** (seeded, no backend) so you can feel the flow before we commit tables?