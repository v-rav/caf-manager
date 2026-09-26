Ravinder, this is heading in the right direction, but after comparing it with recent Factory governance discussions, FDO hygiene requirements, nomination lifecycle definitions, and leadership reporting needs, I would recommend one major evolution.

Current model = Nomination Tracker
Recommended model = Factory Operating System

Your proposed model already covers:

GHCP readiness
GHCP adoption
Migration stages
Deliverables
Outcomes
Executive reporting

But there is one missing dimension that repeatedly appears in Factory governance discussions:

Blockers, Dependencies, and SLA impact tracking.

Recommended Final Data Model
1. Nomination Master

Keep your Stage 0 exactly as-is.

Additional fields:

Field	PurposeNomination ID	Unique tracking
Wave ID	Multi-wave engagement
FDO Link	Drill-down
MSX Opportunity ID	Pipeline linkage
ACR Value	Business impact
Offering Type	Rehost/Replatform/Refactor
Customer Segment	Enterprise/SMB/Public Sector
Strategic Account	Y/N

This aligns with existing nomination governance and wave tracking.

2. Prerequisites & Readiness Gate

This becomes the most important governance section.

Customer Readiness
FieldGHCP Enterprise License
GHCP Standard License
Customer Approved GHCP
Repository Access
Customer Environment Access
KT Documents Available
Landing Zone Ready
Security Review Completed

Recent App Mod discussions are repeatedly highlighting:

GHCP license
Repo access
Customer approval
KT/completion prerequisites

as the biggest causes of delays.

3. Blocker Governance

This is the biggest gap today.

Block CategoryAwaiting GHCP License
Awaiting Customer Approval
Awaiting Repository Access
Awaiting Environment Access
Awaiting Landing Zone
Awaiting Security Review
Awaiting Customer Testing
Awaiting PM
Awaiting Partner
Internal Factory Dependency

Also capture:

FieldBlocked Since
Blocked Owner
SLA Clock Stopped?
Expected Resolution Date

This becomes one of the most valuable leadership views.

4. Tool Usage Framework

Your model is already strong.

Suggested standardization:

Tool Category
ToolGHCP-CI
GHCP AppMod Extension
GHCP AppMod CLI
GHCP-IaC
AppCAT
Azure Migrate
Copilot Workspace
Azure Architecture Center
AKS Accelerator
ACA Accelerator
App Service Accelerator
Custom Factory Automation

These are directly aligned with the tooling names currently being tracked in Factory governance conversations.

5. Activity Tracking

Multi-select.

CategoryAssessment
Cloud Readiness
Dependency Analysis
Version Upgrade
Code Remediation
Containerization
IaC Generation
Terraform Generation
Bicep Generation
AKS Migration
ACA Migration
App Service Migration
CI Pipeline
CD Pipeline
Deployment Automation
Documentation Generation
Runbook Generation
Architecture Review

This matches the actual Factory delivery lifecycle.

6. Migration Lifecycle

Keep but make stage-gated.

StageIntake
Qualification
Discovery
Assessment
Planning
Remediation
Containerization
IaC
Deployment
UAT
Hypercare
Closure

These are already reflected in App Factory lifecycle definitions.

7. Factory Deliverables

This should become governance-controlled.

DeliverableAssessment Report
Scope Document
Architecture Review
Modernization Report
Remediation Report
Container Images
AKS Manifest
Helm Charts
Bicep Templates
Terraform Templates
CI/CD Pipeline
Runbook
Migration Plan
Handover Document

Scope Document should be mandatory because leadership is emphasizing scope governance and approval tracking.

8. GHCP Adoption Score

I really like your maturity framework.

I would slightly refine it.

Level	Description0	Not Discussed
1	Customer Aware
2	Evaluation
3	Procurement In Progress
4	Licensed
5	Assessment using GHCP
6	Remediation using GHCP
7	End-to-End Migration using GHCP

Provides an adoption KPI for EMEA, Asia, and Americas.

9. Outcome Metrics

Keep yours.

Add:

MetricHours Saved
Estimated Effort Reduction
ACR Influenced
Defects Prevented
SLA Compliance
Customer CSAT

Leadership is increasingly asking for value realization and cycle-time improvement.

Executive Dashboard (My Recommended Layout)
Page 1 - Leadership
Total Nominations
Active Nominations
Blocked Nominations
Completed Nominations
GHCP Adoption Rate
Assessment Count
Modernization Count
Containerization Count
AKS Migrations
App Service Migrations
ACR Influenced
Page 2 - Operational Review
Nominations by Stage
Nominations by Region
Nominations by SA
Aging Analysis
SLA Breaches
Blockers
Readiness Status
Page 3 - GHCP Adoption
Accounts With License
Accounts Awaiting License
GHCP Tool Usage Trend
Top GHCP Capabilities Used
Adoption Level Distribution
Page 4 - Factory Productivity
Assessments Completed
Remediations Completed
IaC Generated
Pipelines Generated
Deliverables Produced
Hours Saved
Bottom Line

Your proposed model is already much stronger than a simple resource dashboard. My recommendation is to add three governance pillars:

Prerequisites & Readiness
Blockers & SLA Management
GHCP Adoption Maturity

Once these are added, it becomes a true CAF/App Factory Operations Dashboard that can support EMEA weekly reviews, leadership reporting, FDO hygiene, GHCP adoption tracking, and nomination-to-delivery governance end-to-end.



-----


Yes. Based on what has happened with both SocGen SGMR and Air France COMET, I think a simple "Blocked" category is not enough. The problem is different. These projects are active, consuming resources, but not progressing at planned factory velocity.

What we have observed

Common pattern across both accounts:

Pilot/POC accepted to gain customer confidence and unlock larger migration opportunities.
Pilot apps turned out to be re-architecture efforts rather than standard modernization.
Duration extended from normal factory timelines to 4-6+ months.
Dedicated SMEs/architects became tied to a single account.
New defects, enhancements, testing requests and scope expansion continued after migration completion.
Broader factory velocity suffered because shared resources could not support other nominations.

This is not a blocker.

It is a Strategic Investment Engagement.

Recommendation: Introduce a new category

Instead of Blocked, create a category such as:

Category	MeaningStrategic Pilot	Customer confidence building engagement expected to unlock future migration portfolio
Strategic Investment	Dedicated resources assigned beyond standard factory model
Velocity Impacted	Account consuming shared capacity and impacting other nominations
Scope Expansion	Pilot has moved beyond original modernization scope
Customer Validation Loop	Waiting for customer testing/defect validation rather than engineering work
What leadership really needs visibility into

Today all projects look the same.

A normal 4-week migration and a 6-month strategic pilot are treated equally.

Add governance fields:

Field	ExampleStrategic Account	Yes
Pilot Engagement	Yes
Future Pipeline Potential	200+ apps
Dedicated Resources	4 SMEs + 2 Architects
Velocity Impact Score	High
Duration Beyond Standard	+120 days
Unlock Condition	Customer confidence / POC completion
Resource Opportunity Cost	15 nominations delayed

This tells leadership:

"We are not slow because of execution issues. We are intentionally investing capacity to unlock a large future opportunity."

Even better: create a "Factory Investment Register"

Like finance tracks investments.

Track:

Account
Original scope
Strategic reason
Resources committed
Additional effort consumed
Opportunity pipeline unlocked
Expected ROI (Apps, ACR, NNR)

Example:

Account	InvestmentSocGen	SGMR pilot to unlock 200+ app roadmap
Air France	COMET pilot to establish AI modernization credibility
Mercedes	Strategic lighthouse account
BMW	Strategic modernization reference

Then when utilization is challenged, you can show:

30% of factory Java capacity is currently allocated to strategic investments, not standard nominations.

My view

Do not create only a Blocked category.

Create three governance dimensions:

Waiting For (Customer, Environment, Access, Testing, Approval)
Strategic Investment Account (Yes/No)
Velocity Impact (Low/Medium/High)

For EMEA, Air France and SocGen would clearly have:

Account	Strategic	Velocity Impact	Dedicated ResourcesSocGen	Yes	High	Yes
Air France	Yes	High	Yes

This gives leadership a much more accurate picture than "In Progress" or "Blocked", and helps justify why certain strategic accounts require a different operating model. Based on the discussions, several leaders have already raised the need for dedicated-resource models and explicit scope governance for these strategic engagements.



----

Strategic Pilot Governance Model (CAF / App Factory)
Executive Intent

Strategic pilots are not standard factory deliveries.

They are customer confidence-building investments undertaken to unlock a larger migration portfolio, establish credibility, validate the factory model, or create lighthouse references. These engagements require a separate governance model because applying normal delivery KPIs often creates misleading perceptions around velocity, utilization, and SLA performance.

Examples include pilot modernization engagements where the expected outcome extends beyond delivery of a single application and aims to influence future migration opportunities.

1. Strategic Pilot Classification Framework

Every nomination should be classified at intake.

Classification	Description	Standard KPI AppliesStandard Factory	Typical migration within existing scope	Yes
Strategic Pilot	Confidence-building engagement intended to unlock future work	Partially
Lighthouse Engagement	High-visibility reference account	No
Innovation / POC	Technology validation exercise	No
Recovery Engagement	Escalation-driven customer save activity	No

Only projects approved as Strategic Pilot should be allowed to deviate from normal factory operating metrics.

2. Mandatory Entry Criteria

A pilot cannot start unless all of the following are documented.

Business Justification
Strategic account identified
Expected follow-on opportunity documented
Customer sponsorship identified
Success criteria defined
Delivery Readiness
Scope document signed
CAF deliverables agreed
Customer responsibilities documented
Exit criteria approved
Leadership Approval
CFTL approval
Regional Lead approval
Factory Leadership approval (if dedicated resource model required)
3. Strategic Pilot Operating Model
Phase 1 - Pilot Execution

Duration Target:

6-8 weeks

Objective:

Demonstrate capability
Prove migration path
Validate tooling
Build customer confidence
Phase 2 - Customer Validation

Duration Target:

Maximum 4 weeks

Objective:

UAT
Testing
Signoff
Production validation
Phase 3 - Portfolio Expansion Decision

Duration Target:

Maximum 2 weeks

Decision required:

Scale
Transition
Close

No pilot should remain indefinitely in validation mode.

4. Pilot Exit Criteria

A pilot is considered complete when ALL conditions are met:

Technical Exit

✅ Migration completed

✅ Application deployed

✅ Functional validation completed

✅ Known risks documented

Customer Exit

✅ Customer testing completed

✅ Customer signoff received

OR

✅ Customer failed to provide feedback within agreed timeline

Factory Exit

✅ Documentation delivered

✅ Handover completed

✅ Hypercare completed

Once exit criteria are achieved, resources should be released.

5. Time Limits and Escalation Thresholds
Green

0-60 Days

Normal operations
Amber

61-90 Days

Required:

Monthly leadership review
Revalidate value proposition
Red

91-120 Days

Required:

CFTL review
Dedicated resource justification
Executive Review

120 Days

Required:

Leadership decision

One of:

Scale engagement
Create funded extension
Transition to customer team
Close project

A pilot should never continue indefinitely because additional enhancement requests keep arriving.

6. Resource Governance
Resource Commitment Caps

Strategic Pilots should have limits such as:

Role	Recommended Max CommitmentArchitect	25-50%
SME	50%
Engineer	Dedicated if approved

Avoid:

Multiple architects tied full-time for months
SMEs operating as customer support teams
Unlimited enhancement support
7. Velocity Impact Governance

Introduce a new tracked metric.

Factory Velocity Impact Score
Low

Impacting < 5 nominations

Medium

Impacting multiple shared resources

High

Blocking capacity for strategic skillsets

Critical

Preventing acceptance of new work

Leadership should see this metric for every strategic pilot.

8. New Status Categories

Current statuses are insufficient.

Recommended additions:

Status	MeaningStrategic Pilot Active	Confidence-building engagement
Customer Validation	Awaiting customer testing/signoff
Strategic Investment	Dedicated resource allocation approved
Portfolio Expansion Pending	Awaiting next-wave decision
Exit Criteria Met	Delivery complete, waiting closure
Executive Review Required	Exceeded duration threshold
Leadership Summary: Strategic Pilot Impact Assessment
Current Observation

Recent strategic pilot engagements demonstrate a recurring pattern:

Pilots initiated to build customer confidence and unlock future migration opportunities.
Effort duration significantly exceeds standard factory delivery timelines.
Dedicated architects, SMEs, and engineers remain attached to a single account for extended periods.
New enhancement requests, validations, and customer-led activities continue after migration objectives are achieved.
Factory capacity available for new nominations is reduced.
Standard velocity metrics no longer reflect actual delivery performance.
Business Value

Strategic pilots provide value through:

Creation of executive customer trust
Increased likelihood of portfolio-scale migrations
Reference architectures and reusable patterns
Improved positioning for future transformation opportunities
Risks

Without governance controls:

Resource lock-in
SLA degradation across other nominations
Utilization imbalance
Reduced intake capacity
Difficulty forecasting delivery timelines
Leadership perception that factory productivity has decreased
Recommendations
Immediate
Introduce "Strategic Pilot" project classification.
Track dedicated-resource consumption separately from standard factory utilization.
Add velocity-impact reporting at account level.
Establish formal entry and exit criteria.
Near Term
Cap pilot duration at 90 days before executive review.
Require business justification for extensions.
Enforce customer signoff timelines.
Transition successful pilots into portfolio migration programs.
Long Term

Create a Strategic Investment Register for all pilot engagements showing:

Account
Strategic objective
Resources committed
Duration
Opportunity unlocked
Velocity impact
Outcome

This enables leadership to distinguish between:

Delivery inefficiency
Customer dependency
Intentional strategic investment

and provides a defensible explanation when strategic accounts consume disproportionate factory capacity.