This is actually a very good governance model and would make your EMEA dashboard much more actionable than simply tracking accounts and resources.

I would add a GHCP Readiness & Usage Framework to every nomination.

Proposed Nomination Lifecycle Dashboard
Stage 0 - Intake
Field	ValueAccount	
Wave	
PM	
SA	
App Engineer	
DevOps	
Customer Contact	
Region	
Workload Type (.NET/Java/PHP/etc.)	
Stage 1 - GHCP Readiness

Before Factory starts work:

Question	ValueCustomer has GHCP Enterprise License?	Yes / No
Customer willing to procure?	Yes / No
Customer approved GHCP usage?	Yes / No
Customer repo access available?	Yes / No
Assessment can start?	Yes / No
Governance Status
StatusReady
Awaiting License
Awaiting Approval
Awaiting Access
Stage 2 - GHCP Tool Usage

Instead of simply saying "Using GHCP", capture exactly what was used.

Tool	Used?GHCP-CI	
AppMod Extension	
AppMod CLI	
Azure Migrate App Mod CLI	
Copilot Workspace	
AppCAT	
Azure Migrate	
VS Code Agent Mode	
Custom Prompt Framework	
Other	
Stage 3 - Purpose of Usage

One nomination may use multiple capabilities.

Activity	UsedAssessment	
Discovery	
Modernization	
Code Remediation	
Version Upgrade	
Containerization	
IaC Generation	
IaC Validation	
Deployment Pipeline	
CI Pipeline	
CD Pipeline	
Target Architecture Design	
App Service Migration	
AKS Migration	
ACA Migration	
ARO Migration	
DB Migration Support	
Re-architecture	
Documentation Generation	
Hypercare Support	
Stage 4 - Migration Journey
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
Stage 5 - App Factory Deliverables

This is usually where confusion happens.

For every nomination capture:

Deliverable	StatusAssessment Report	
Architecture Review	
Modernization Report	
Remediation Report	
Container Images	
AKS Manifest	
Helm Charts	
Bicep Templates	
Terraform Templates	
Pipeline Configuration	
Deployment Guide	
Runbook	
Handover	
Stage 6 - Outcome Metrics

Leadership view.

Metric	ValueApplications Assessed	
Apps Remediated	
Lines Modernized	
Containers Generated	
Pipelines Generated	
IaC Artifacts Generated	
Deployment Success	
Defects Fixed	
Customer Sign-off	
Example
Account	Stage	GHCP Tool	PurposeAir France	Remediation	GHCP-CI	.NET Upgrade
SITA	Assessment	AppMod CLI	Assessment
Mercedes-Benz	Containerization	GHCP-CI	AKS Migration
SGMR	Remediation	AppMod Extension	Cache Implementation
Executive Dashboard Metrics

For weekly EMEA review:

Metric	CountAccounts with GHCP License	24
Accounts Awaiting License	7
GHCP-CI Usage	18
AppMod CLI Usage	10
Assessments Completed	45
Remediations Completed	21
Containerizations Completed	19
IaC Generated	15
AKS Migrations	12
App Service Migrations	9

This would let you answer questions such as:

Which accounts are blocked because customer hasn't procured GHCP?
How many migrations actually use GHCP-CI?
Which GHCP capability delivers the most value?
Which stage consumes the most effort?
Which engineers are doing assessments vs remediation vs deployment?
How many nominations convert from assessment → remediation → migration?

For your CAF/App Factory governance, this is much closer to a true Factory Operations Dashboard rather than a simple resource tracker.



---
This is actually a great governance data model. Instead of only capturing "GHCP Used = Yes/No", capture the entire maturity journey.

Nomination → GHCP Readiness & Usage Tracker
Customer Readiness
Field	ExampleCustomer Has GHCP Enterprise License	Yes
Customer Has GHCP Standard License	No
Customer Ready To Procure	Yes
License Procured During Project	No
Factory License Used For Assessment Only	Yes
Customer Technical Contact	John Smith
Customer GHCP Champion Identified	Yes
GHCP Capability Utilization
Example Account: SITA
Area	Used	ToolAssessment	Yes	GHCP-CI
Remediation	Yes	GHCP AppMod Extension
Containerization	Yes	GHCP-CI
IAC Generation	Yes	AppMod CLI
Deployment Pipeline	Yes	GHCP-CI
Application CI	Yes	GHCP-CI
Application CD	Yes	GHCP-CI
Architecture Recommendation	Yes	Azure Migrate Agent
Modernization	Yes	AppMod Extension
Version Upgrade	Yes	AppMod Extension
Re-Architecture Recommendation	No	-
Example Account: Air France
Area	Used	ToolAssessment	Yes	GHCP-CI
Remediation	Yes	GHCP AppMod
Containerization	Yes	GHCP-CI
IAC Generation	No	
App CI	Yes	
App CD	Yes	
Modernization	Yes	
Upgrade .NET	Yes	
AKS Target Recommendation	Yes	
Re-Architecture	No	
Example Account: Mercedes-Benz
Area	Used	ToolPHP Analysis	Yes	GHCP-CI
Code Modernization	Yes	GHCP AppMod
ACA Recommendation	Yes	GHCP-CI
Containerization	Yes	
Pipeline Generation	Yes	
Deployment Support	No	
Re-Architecture	No	
Example Account: SGMR
Area	Used	ToolAssessment	Yes	GHCP-CI
Remediation	Limited	
Containerization	No	
IAC	No	
CI/CD	No	
Cache Implementation Guidance	No	
Architecture Review	Yes	
Recommended Master Fields
Customer GHCP Readiness
Customer_GHCP_License
Customer_GHCP_Procurement_Status
Customer_GHCP_Champion
License_Type

GHCP Usage
GHCP_Used
GHCP_Tool


Possible values:

GHCP-CI
GHCP AppMod Extension
GHCP AppMod CLI
Azure Migrate Agent
AppCAT
GitHub Copilot Workspace
Custom Prompt Library

Activity Performed

Multi-select:

Assessment
Cloud Readiness
Dependency Analysis
Tech Stack Discovery
Containerization
Version Upgrade
Remediation
Code Modernization
IAC Generation
Terraform Generation
Bicep Generation
AKS Migration
ACA Migration
App Service Migration
CI Pipeline
CD Pipeline
Deployment Automation
Architecture Recommendation
Target Platform Recommendation
Re-Architecture Recommendation
Documentation Generation
Runbook Generation

Sample Executive View
Account	Customer License	GHCP Used	Tool	PurposeSITA	Procured	Yes	GHCP-CI	Assessment
Air France	Procured	Yes	AppMod	Modernization
Mercedes-Benz	Factory Trial	Yes	GHCP-CI	PHP Migration
SGMR	No	Limited	GHCP-CI	Assessment
KPMG	Yes	Yes	AppMod	Upgrade
BMW	Planned	No	-	Discovery
Additional Metric for Leadership

Add:

GHCP Adoption Level


Values:

Level	Meaning0	Not Discussed
1	Customer Aware
2	Procurement In Progress
3	Licensed
4	Assessment Using GHCP
5	Remediation Using GHCP
6	End-to-End Migration Using GHCP

This gives Rich, Arun, Amit, yourself and leadership a direct view of:

Which customers have GHCP
Which tool is actually being used
Which migration activities are leveraging AI
Where adoption is stuck
Factory AI impact across nominations

This would make an excellent addition to your FDO/CAF governance dashboard and weekly business review deck.