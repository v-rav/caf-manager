GHCP Implementation Prompt - CAF Operations Portal (Configuration-Driven, Multi-Region Ready)
Objective

Build a production-ready internal CAF Operations Portal for resource governance, capacity management, leave visibility, strategic account tracking, nomination planning, and executive reporting.

The solution must be:

Configuration-driven
Role-based
Multi-region ready
Deployable on Azure App Service Free Tier
Low-cost
Easy to maintain
Extensible to support future regions, practices, towers, and offerings

This is not a customer-facing application.

1. Business Goal

Create a centralized operations platform that provides:

Resource visibility
Capacity planning
Account ownership tracking
Leave visibility
Strategic account coverage
Nomination pipeline visibility
Executive dashboards

The platform should support:

Global View
Regional View
Practice View
Tower View

without requiring code changes.

2. Design Principles
MUST FOLLOW
Configuration-first design
No hardcoded users
No hardcoded regions
No hardcoded capacity limits
No hardcoded strategic accounts
No hardcoded role assignments

Everything should be data-driven.

3. Technology Stack
Frontend
React 19
TypeScript
Fluent UI
React Router
Axios
Chart.js / Recharts
DataGrid
Backend
.NET 9 Web API
Entity Framework Core
Clean Architecture
Repository Pattern
Swagger
Background Hosted Services
Database

Use:

SQLite


Reason:

Works on App Service Free Tier
Zero Azure Database Cost
No SQL Server needed
No Cosmos DB needed
No PostgreSQL needed
No networking complexity

Database file:

cafdb.sqlite

4. Source Files

System must ingest data from:

Resource Source
App Migration_Region_wise Mapping.xlsx


Contains:

Resources
Skills
Regions
Roles
Experience
Account Mapping
Leave Source
LeaveCal.xlsx


Contains:

Planned Leave
Resource Availability
Holidays
Engagement Source
Time-hunt_Tracking.xlsx


Contains:

Customer Meetings
Account Engagement
Demand Signals
Nomination Activity
5. Governance Model
Regions

Must support unlimited future regions.

Initial setup:

EMEA
ASIA
AMER


But regions must come from configuration.

User Roles

Create configurable roles:

Global Lead
Regional Lead
Architect Lead
App Architect
App Engineer
DevOps Engineer
SME
Viewer


No role permissions may be hardcoded.

Store permissions in configuration tables.

6. Capacity Model

DO NOT USE ALLOCATION PERCENTAGES.

Approved model:

1 Resource = Optimal Capacity of 5 Active Accounts/Nominations


Formula:

UtilizationPercent =
ActiveAccountCount / CapacityLimit


Default:

CapacityLimit = 5


Examples:

Accounts	Utilization1	20%
2	40%
3	60%
4	80%
5	100%
6	120%
7	140%
Capacity Status

Use configurable thresholds.

Initial default:

Range	Status0-40	Available
41-80	Partially Utilized
81-100	Fully Utilized
>100	Overloaded

Thresholds should come from configuration.

7. Database Schema
Resource
ResourceId
PSID
Name
Email
Region
Role
PrimarySkill
Skills
ExperienceYears
Status
DedicatedFlag
CapacityLimit
ActiveFlag


Default:

CapacityLimit = 5

Account
AccountId
AccountName
Region
Status
StrategicFlag
PriorityWeight

AccountAlias
AliasId
Alias
StandardAccountName


Examples:

SG -> Societe Generale
Soc Gen -> Societe Generale
TSN -> Skills Network
MB -> Mercedes-Benz
KPC -> Kuwait Petroleum Corporation

ResourceAccount
Id
ResourceId
AccountId
Source
RelationshipType

LeaveFact
Id
ResourceId
LeaveDate
LeaveType

EngagementFact
Id
Date
ResourceId
AccountId
MeetingName
Duration
Region
Remarks

CapacityFact
Id
ResourceId
AccountCount
CapacityLimit
UtilizationPercent
CapacityStatus
LastUpdated

Nomination
Id
AccountId
Technology
Region
Status
OpenedDate
Remarks

8. Configuration Tables
StrategicAccountConfiguration
AccountId
StrategicFlag
PriorityWeight
RiskFlag
ExecutiveVisibilityFlag


Priority:

1 = Standard
2 = Strategic
3 = Executive Critical

CapacityConfiguration
RoleName
CapacityLimit
WarningThreshold
OverloadedThreshold


Default:

Architect = 5
Engineer = 5
DevOps = 5
Architect Lead = 8

ApplicationSettings
Key
Value
Description


Examples:

DefaultCapacityLimit = 5

RefreshTime = 02:00

AvailableThreshold = 40

OverloadedThreshold = 100

9. Transformation Layer

Create services:

ResourceImportService

Loads:

App Migration_Region_wise Mapping.xlsx


Generates:

Resource
Account
ResourceAccount

LeaveImportService

Loads:

LeaveCal.xlsx


Converts calendar layout into:

Resource
Date
Leave Type


records.

EngagementImportService

Loads:

Time-hunt_Tracking.xlsx


Generates:

EngagementFact


records.

CapacityCalculationService

Calculates:

AccountCount
Utilization
Status
CapacityFact

10. Background Jobs

Create hosted service.

Schedule:

02:00 AM Daily


Process:

Load Sources

Transform Data

Update SQLite

Rebuild Capacity Fact

Clear Cache

11. REST APIs

Create endpoints:

Dashboard
GET /api/dashboard/executive


Returns:

Total Resources
Active Accounts
Available Resources
Overloaded Resources
Resources On Leave
Open Nominations

Resources
GET /api/resources
GET /api/resources/{id}

Accounts
GET /api/accounts
GET /api/accounts/{id}

Capacity
GET /api/capacity

Strategic Accounts
GET /api/strategicaccounts

Leave
GET /api/leave

Nominations
GET /api/nominations

12. React Pages
Executive Dashboard

Cards:

Total Resources
Active Accounts
Available Resources
Overloaded Resources
Resources On Leave
Open Nominations


Charts:

Regional Distribution
Capacity Distribution
Strategic Account Coverage

Resource Hub

Grid:

Resource
Region
Role
Account Count
Utilization
Status

Account Hub

Grid:

Account
Region
Resource Count
Strategic Flag

Capacity Dashboard

Heatmap:

Resource vs Accounts


Colors:

Green
Amber
Red

Leave Dashboard

Views:

30 Day
60 Day
90 Day

Strategic Account Dashboard

Show:

Coverage
Assigned Resources
Recent Activity
Risk Indicators

Nomination Pipeline

Show:

Open
In Progress
Closed

13. Authentication

Phase 1:

No authentication


Phase 2:

Microsoft Entra ID


Implement architecture so Entra ID can be plugged in later without redesign.

14. Deployment

Deploy on:

Azure App Service Free Tier


Requirements:

Single deployment package

SQLite database

No Azure SQL

No CosmosDB

No PostgreSQL

No paid services

15. Deliverables

Generate:

Complete solution structure
Clean Architecture implementation
EF Core migrations
SQLite configuration
Entity models
Import services
Capacity engine
REST APIs
React UI
Dashboard components
Swagger
Seed data
App Service deployment guide
README
Future Entra ID integration points

Generate production-ready code, not samples. Use enterprise coding standards, DI, logging, error handling, configuration-driven architecture, and extensibility for future Global/Regional CAF operations.