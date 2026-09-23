Below is a GHCP-ready project specification you can paste directly into VS Code and ask GHCP to generate the solution.

Project Name

CAF Operations Portal (EMEA MVP)

Business Goal

Build a lightweight internal CAF Operations Portal for:

EMEA resource governance
Capacity planning
Resource visibility
Leave management
Strategic account coverage
Nomination tracking
Executive reporting

Primary owner:

EMEA Lead: Ravinder Rana

Regional hierarchy:

Global Lead: Arunkumar Azariah koilraj
EMEA Lead: Ravinder Rana
ASIA Lead: Amit Bengali
Constraints
Hosting

Azure App Service Free Tier (F1)

Database

SQLite

No Azure SQL

No Cosmos DB

No PostgreSQL

No Fabric Warehouse

No external dependency.

Technology Stack
Frontend

React 19

TypeScript

Fluent UI React

React Router

Axios

Chart.js

Backend

.NET 9 Web API

Entity Framework Core

SQLite

Hosted background service

Swagger

Authentication

Phase 1

No authentication

Simple internal deployment

Phase 2

Microsoft Entra ID

Source Systems

Application data will come from:

Resource Source

App Migration_Region_wise Mapping.xlsx

Used for:

Resources
Skills
Roles
Regions
Account Mapping
Leave Source

LeaveCal.xlsx

Used for:

Leave
Holidays
Availability
Engagement Source

Time-hunt_Tracking.xlsx

Used for:

Customer engagement
Meeting activity
Strategic account activity
Demand indicators
Capacity Model

No allocation percentages.

Approved model:

1 Resource = 5 Active Accounts


Calculation:

Utilization = Active Accounts / 5


Examples:

Accounts	Utilization1	20%
2	40%
3	60%
4	80%
5	100%
6	120%

Status Rules

0-2 = Available

3-4 = Partially Utilized

5 = Fully Utilized

6+ = Overloaded

Database Schema

Resource

ResourceId PSID Name Email Region Role PrimarySkill Skills ExperienceYears Status DedicatedFlag CapacityLimit ActiveFlag

Default:

CapacityLimit = 5

Account

AccountId AccountName Region StrategicFlag Status

AccountAlias

AliasId Alias StandardAccountName

Examples:

SG → Societe Generale

Soc Gen → Societe Generale

TSN → Skills Network

MB → Mercedes-Benz

KPC → Kuwait Petroleum Corporation

ResourceAccount

Id ResourceId AccountId Source PrimarySecondary

LeaveFact

Id ResourceId LeaveDate LeaveType

Examples:

Leave

Holiday

Regional Holiday

Optional Holiday

Special Leave

EngagementFact

Id Date ResourceId AccountId MeetingName Duration Region Remarks

CapacityFact

Id ResourceId AccountCount CapacityLimit UtilizationPct Status

Nomination

NominationId AccountName Technology Region Status OpenedDate Remarks

Transformation Layer

Create ExcelImportService

File Loaders

ResourceMappingLoader

LeaveLoader

TimeHuntLoader

Transformation Rules

Resource Normalization

Create single resource record.

Preferred key sequence:

PSID

Email

Generated GUID

Account Normalization

Apply AccountAlias table.

Examples:

text SG Soc Gen Societe General Societe Generale ↓

Societe Generale 3. Leave Transformation

Convert calendar view into:

text Resource LeaveDate LeaveType 4. Engagement Transformation

Extract:

Account

Resource

Meeting Date

Region

Strategic Accounts Seed List

Phase 1 strategic accounts:

Air France

SITA

Societe Generale

KOC Holding

Skills Network

Mercedes-Benz

MOE

KPC

Glencore

Simployer

FNZ

CAIT

Repsol

Swiss Re

Bilfinger

Derived from current account mapping and engagement activity.

Web Pages

Executive Dashboard

Cards:

Total Resources

Available Resources

Partially Utilized

Fully Utilized

Overloaded

Resources On Leave

Strategic Accounts

Open Nominations

Charts:

Region Distribution

Resource Capacity Distribution

Resource Hub

Search resources

Filter:

Region

Role

Skill

Status

Display:

Accounts Assigned

Capacity Status

Leave Status

Account Hub

Search accounts

Assigned Resources

Resource Count

Strategic Flag

Recent Activity

Capacity Dashboard

Table:

Resource AccountCount Utilization Status

Heat Map:

Green

Amber

Red

Leave Dashboard

Calendar view

Upcoming 30 days

Upcoming 60 days

Upcoming 90 days

Strategic Accounts Dashboard

Account coverage

Assigned resources

Recent customer activity

Risk indicator

Nomination Pipeline

Open nominations

In progress

Completed

By region

Global Dashboard (Future)

For role based

Roll-up:

EMEA

ASIA

AMER

Background Jobs

Nightly refresh:

02:00 AM

Steps:

Load source files

Run transformations

Update SQLite

Rebuild capacity facts

Refresh caches

Performance Targets

Startup < 10 seconds

Dashboard load < 3 seconds

500 users not required

Target audience:

10-30 users

Deployment

App Service Free Tier

SQLite stored inside App_Data

Static React build served from backend

Single deployment package

Future Roadmap

Phase 2

Entra ID

Role-based access

Nomination workflow

Account ownership

Phase 3

AI recommendation engine

Who can take a new nomination

Skills matching

Leave-aware recommendations

Capacity prediction

Phase 4

Global CAF operations platform

EMEA

ASIA

AMER

single governance portal

My recommendation: feed the entire specification above into GHCP and ask it:

Generate a production-ready .NET 9 + React + SQLite solution with clean architecture, EF Core migrations, Fluent UI, import services for Excel files, capacity engine, REST APIs, dashboards, and deployment instructions for Azure App Service Free Tier.