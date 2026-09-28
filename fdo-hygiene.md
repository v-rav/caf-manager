
FDO Hygiene FAQ for Solution Architects
Purpose
This page provides a simple, practical reference for Solution Architects (SAs) responsible for maintaining technical information in FDO across CAF/App Factory engagements.

Core principle: The PM owns overall operational FDO hygiene and project status. The SA owns the accuracy, completeness, and currency of the technical information and validates technical artifacts and completion data with the PM.




Quick SA Checklist
For every active wave, confirm the following:

- [ ] Technical Summary is current
- [ ] Tool Used is recorded
- [ ] Automation Used is recorded, when applicable
- [ ] Product Feedback is captured, when applicable
- [ ] Intellectual Property is captured, when applicable
- [ ] Scope document is technically reviewed
- [ ] Architecture and migration approach are documented
- [ ] Technical risks, blockers, dependencies, and next actions are current
- [ ] Application count, migration path, environments, and core data are accurate
- [ ] App–DB and relevant dependent waves are linked
- [ ] Technical artifacts, runbooks, and execution evidence are uploaded
- [ ] Quality Gate review checkbox is completed where required
- [ ] POE is validated before wave closure


Frequently Asked Questions
1. What is FDO hygiene?
FDO hygiene means keeping the engagement record complete, accurate, current, and supported by evidence. A hygienic record should allow another stakeholder to understand:

- What is in scope
- What has been completed
- Which tools and automations were used
- What is currently blocked
- What happens next
- What evidence supports the reported outcome
FDO should remain the authoritative record for execution visibility, audit readiness, quality-gate compliance, and outcome reporting.
2. Who owns FDO hygiene: PM or SA?
Ownership is shared, but responsibilities are different:
PM owns

- Overall engagement status and stage
- Planned dates and follow-up dates
- Customer communications and operational comments
- Blocked or Deferred status processing
- Project documentation and operational evidence
- Overall closure coordination
SA owns

- Technical Summary
- Tool Used
- Automation Used
- Product Feedback
- Intellectual Property
- Technical risks and dependencies
- Technical validation of scope, architecture, migration path, application count, cores, and technical evidence
- Technical Quality Gate review
Shared PM and SA responsibility

- Validate that FDO reflects the actual engagement state
- Confirm application and core data before closure
- Confirm mandatory artifacts are present
- Confirm blockers, ownership, next actions, and evidence
- Validate the wave before it is marked complete
3. When should an SA start updating FDO?
The SA should start technical hygiene once assigned and actively update it as scope and tooling become clear, normally during prerequisite execution and scope finalization.
Do not wait until closure to populate all technical fields. Update information as it becomes known and validate the complete record before the wave closes.
4. How often should the Technical Summary be updated?
Update the Technical Summary:

- After every customer or partner technical session
- Whenever there is a major technical decision, blocker, or scope change
- At least once per week for an active wave
The Technical Summary is separate from the PM's project summary.
5. What should a good Technical Summary contain?
Use this structure:
Completed: What was achieved since the last update.
Issues/Blockers: Technical or customer dependencies affecting progress.
Actions: Follow-ups, owners, and expected outputs.
Next Session: What will be addressed next.
Example

Completed AppCAT assessment for 3 applications and reviewed findings with the customer. Repository access is available; lower-environment access is pending. Customer to provide access by 30 Sep. Next session will finalize remediation scope and migration sequencing.


6. Is Tool Used mandatory?
Yes. Tool Used must be recorded at wave level for every applicable wave.
Capture:

- Tool or product name
- Specific feature, where relevant
- How it was used in the wave
Examples

- Azure Migrate: Discovery and assessment
- AppCAT: Application cloud-readiness assessment
- GHCP App Modernization: Code assessment and remediation
- GitHub Actions: CI/CD pipeline implementation
- Bicep or Terraform: Infrastructure provisioning
If no listed tool applies, follow the current FDO guidance for unavailable or manual options and provide meaningful details.
7. When should Tool Used be updated?
Update it when the execution tool is selected or begins to be used. In most App Modernization cases, this becomes clear during prerequisites or scope finalization.
Do not leave the field blank until closure. Revalidate it before closing the wave.
8. What should be entered under Automation Used?
Capture any approved automation, script, or accelerator used during execution, including:

- PowerShell scripts
- Approved assessment or migration automation
- CI/CD automation
- Infrastructure-as-code automation
- Factory accelerators
Include what the automation did and, where required, the effort saved. If the automation is not available in the selectable list, use the applicable unavailable option and provide its details.
9. When is Product Feedback required?
Product Feedback is required when the team identifies:

- A product defect or bug
- A tool limitation
- Incorrect or incomplete assessment output
- A recurring manual workaround
- A feature request
- A successful workaround that could improve the product
Do not use Product Feedback merely to record that a tool was used. Use the Tool Used category for normal tool tracking.
10. What should Product Feedback contain?
Record:

- Product or tool name
- Feature or component
- Problem or observation
- Customer or execution impact
- Workaround, if any
- Supporting evidence or document reference
Where the process requires escalation, notify the CFTL so the relevant UAT/TFT tracking ID can be created and mapped back to the FDO feedback entry.
11. When should Intellectual Property be recorded?
Record IP when a Factory, Microsoft, partner, or practice-owned reusable asset is used, such as:

- Script
- Tool
- Process
- Runbook
- Agent
- Reusable accelerator
Include its category and a short description of how it supported execution.
12. Is the SA responsible for the scope document?
The PM coordinates and maintains the document, while the SA is responsible for technical validation.
The SA should confirm that the scope document correctly identifies:

- Applications and environments in scope
- Source and target platforms
- Migration or modernization activities
- In-scope and out-of-scope items
- Customer, partner, Factory, and account-team responsibilities
- Assumptions and dependencies
- Acceptance and exit criteria
After reviewing the artifact, complete the applicable SA Quality Gate confirmation in FDO.
13. What technical documents should be available in FDO?
Depending on the stage and offering, validate the presence of applicable artifacts such as:

- Scope confirmation document
- Assessment output
- Target architecture or design
- Migration approach and project plan
- Prerequisite checklist
- Runbooks
- Risk and dependency log
- Test or validation evidence
- Deployment evidence
- POE and closure evidence
Uploading a document alone is not enough. The corresponding Quality Gate status must also be updated where required.
14. What is the SA's responsibility for FDO status?
The PM updates the official stage and status. The SA must promptly tell the PM when the displayed state does not match technical reality.
Examples:

- Prerequisites are incomplete, but the wave shows Executing Migration
- Execution has stopped because access is missing, but the wave remains On Track
- Migration is complete, but technical validation or POE is missing
- Scope is not finalized, but the wave is shown in a later stage
The SA should not silently allow an inaccurate status to remain.
15. What should happen when work cannot progress?
Document the blocker clearly and work with the PM to apply the approved Blocked or Customer Deferred process.
The entry should state:

- Exact dependency
- Owner
- Date identified
- Impact on execution
- Action taken
- Next follow-up date
- Evidence or customer communication
Operational status changes are PM-owned. The SA provides the technical blocker details and validates that the status reflects reality.
16. Does “Waiting Action on Follow-up Date” stop the velocity clock?
Under the current guidance captured in internal communications, this waiting state does not stop the velocity clock. When execution cannot continue because of a customer or external dependency, the PM should use the documented Blocked or Customer Deferred process rather than leave the nomination in an inaccurate waiting state.
17. Who is responsible for App–DB linkage?
The SA must identify whether a database dependency exists and validate linkage with the PM.
The SA should:

- Confirm whether a DB wave is required
- Confirm that App and DB nominations are linked
- Validate the relationship before closure
- Document why linkage is not applicable, if applicable
The same principle applies to other dependent waves, such as ALZ Dispatch or Security, where required.
18. What should the SA validate for application counts and cores?
Before closure, validate with the PM:

- Number of applications completed or remediated
- Application technology, where supported
- Primary migration path
- Target Azure service
- Number of environments
- Reported core or ACR-related values
- Whether one reported application contains multiple independently deployed components or containers
Do not rely only on the core value. Application count and technical scope must also be correct.
19. What is POE, and what is the SA's responsibility?
Proof of Execution demonstrates that the agreed technical work was completed.
The SA should validate that POE:

- Matches the approved scope
- Identifies the workload or application
- Demonstrates the target Azure outcome
- Supports the reported application and core data
- Includes relevant screenshots, logs, deployment evidence, or customer validation
- Is available before technical closure
The PM coordinates closure; the SA validates the technical evidence.
20. Can a wave be closed if technical hygiene is incomplete?
No wave should be considered technically ready for closure while mandatory technical information or evidence is missing.
Before closure, confirm:

- Technical Summary is current
- Tool Used is captured
- Product Feedback and Automation are captured when applicable
- Scope and technical artifacts are present
- Risks and blockers are resolved or dispositioned
- Application counts and core values are validated
- Related nominations are linked
- POE is complete
- Quality Gate reviews are updated
21. What if FDO does not provide the required option?
Do not leave the record blank without explanation.
Use the currently approved unavailable, other, or manual option where applicable, add meaningful details, and raise the gap through the appropriate FDO/CFTL support process.
22. What are the most common FDO hygiene mistakes?

- Technical Summary is missing or stale
- Tool Used is blank or contains only a tool name without usage details
- Product Feedback is not raised for known limitations
- Scope document is uploaded, but the Quality Gate is not checked
- Architecture or migration plan is missing
- Status does not match actual execution
- Blockers have no owner or next action
- Application count is blank or zero despite completed work
- App–DB or ALZ relationships are missing
- POE does not support the reported result
- Updates are postponed until closure
23. What does “audit-ready” mean?
An audit-ready FDO record contains current data, approved artifacts, clear ownership, customer communication history, documented status decisions, technical evidence, and no unexplained gaps.
A reviewer should not need to contact the SA or PM to understand what happened.
24. What is the simplest rule for SAs?

After every meaningful technical activity, update FDO. Before every stage exit, validate FDO. Before closure, verify the evidence.




Technical Summary Template
Copy and adapt this format:
Completed: [technical activities completed]
Decision/Outcome: [technical decision or result]
Blocker/Dependency: [issue, owner, impact]
Action: [next action, owner, target date]
Next Session: [planned activity]


Wave Closure Checklist
Scope and design

- [ ] Scope is finalized and technically accurate
- [ ] In-scope and out-of-scope items are explicit
- [ ] Target architecture and migration path are validated
- [ ] Customer and Factory responsibilities are clear
Technical data

- [ ] Application count is correct
- [ ] Technology and migration path are correct
- [ ] Target service and environment count are correct
- [ ] Core and ACR-related data are validated
Tools and feedback

- [ ] Tool Used is complete
- [ ] Automation is recorded, when applicable
- [ ] Product Feedback is recorded, when applicable
- [ ] UAT/TFT ID is mapped when required
- [ ] IP is recorded, when applicable
Dependencies and linked work

- [ ] Technical blockers are resolved or formally dispositioned
- [ ] App–DB linkage is complete or justified as not applicable
- [ ] ALZ Dispatch and other dependent waves are linked where applicable
Evidence and closure

- [ ] Technical artifacts are uploaded
- [ ] Validation or test evidence is available
- [ ] POE supports the claimed outcome
- [ ] Technical Summary reflects the final state
- [ ] SA Quality Gate review is complete
- [ ] PM and SA have jointly validated closure data


Governance note
This FAQ separates technical ownership from operational ownership. Current internal standards position the PM as the overall FDO hygiene owner, while specific technical fields and reviews are assigned to the SA. Where process guidance changes, the latest approved Factory process communication takes precedence.
Internal references used

- FY27 CAF PM Operational Standards SOP
- FDO Hygiene & Compliance for Tech Community guidance
- FDO Product Feedback, Automation and IP walkthrough
- App Modernization Leads and Architect Sync guidance
- Blocked and Customer Deferred process communications
- FDO Hygiene recurring review checklist
