# CAF Operations Portal

A production-ready, **configuration-driven** internal portal for CAF resource governance,
capacity management, leave visibility, strategic account tracking, nomination planning, and
executive reporting.

- **Multi-region ready** (Global / Regional / Practice / Tower views) with no code changes
- **Role-based** and **data-driven** — no hardcoded users, regions, capacity limits, roles, or strategic accounts
- Runs on **Azure App Service Free Tier (F1)** with **SQLite** — zero database cost
- Ships as a **single deployment package** (React SPA served by the .NET API)

---

## Architecture

Clean Architecture, four projects:

```
src/
  CafPortal.Domain          Entities, enums, configuration entities
  CafPortal.Application      DTOs, service interfaces, capacity engine, query services
  CafPortal.Infrastructure   EF Core (SQLite), Excel import services, refresh job, seed data
  CafPortal.Api              REST controllers, Program.cs, Swagger, serves the SPA
client/                      React 19 + TypeScript + Fluent UI + Chart.js (Vite)
```

Key design points:

- `IApplicationDbContext` keeps the Application layer persistence-agnostic.
- **Capacity engine** (`CapacityCalculationService`): `Utilization = ActiveAccounts / CapacityLimit`.
  No allocation percentages. Limits and thresholds come from configuration tables.
- **Nightly background job** (`DailyRefreshHostedService`) runs at the configured `RefreshTime`
  (default `02:00`): load sources → transform → update SQLite → rebuild `CapacityFact` → clear cache.
- **Configuration tables** drive everything: `RegionConfiguration`, `RoleConfiguration` +
  `RolePermission`, `CapacityConfiguration`, `StrategicAccountConfiguration`, `SegmentConfiguration`,
  `ApplicationSetting`, and `AccountAlias` (e.g. `SG → Societe Generale`).
- **Configuration page** (in-app admin): edit the optimal accounts-per-role limits (recomputes
  capacity on save) and manage the account **Segment** list (add / rename) — no redeploy needed.

### Capacity model

| Setting | Default |
| ------- | ------- |
| `DefaultCapacityLimit` | 5 |
| `AvailableThreshold` | 40% |
| `PartiallyUtilizedThreshold` | 80% |
| `OverloadedThreshold` | 100% |

Per-role overrides live in `CapacityConfiguration` (e.g. `Architect Lead = 8`).
Precedence when rebuilding capacity: **role config → per-resource limit → default**.

---

## Prerequisites

- .NET SDK 9
- Node.js 20+ (built with Node 22)

---

## Run locally

### 1. Backend API

```powershell
dotnet run --project src/CafPortal.Api --urls http://localhost:5080
```

On first start it applies EF migrations, seeds configuration + a demo dataset, and builds
the initial capacity snapshot. **Seeding is one-time** — once it has run, subsequent starts skip it
(a `SeedCompletedUtc` marker is recorded), so nothing re-applies to data you manage in the app.
SQLite file: `src/CafPortal.Api/App_Data/cafdb.sqlite`.

- Swagger (Development): <http://localhost:5080/swagger>
- Health: <http://localhost:5080/health>

### 2. Frontend (dev, with hot reload)

```powershell
cd client
npm install
npm run dev   # http://localhost:5173  (proxies /api to :5080)
```

### 3. Single-package build (SPA served by the API)

```powershell
cd client
npm run build         # emits into src/CafPortal.Api/wwwroot
cd ..
dotnet run --project src/CafPortal.Api --urls http://localhost:5080
```

Open <http://localhost:5080> — the API serves the SPA and the JSON endpoints together.

---

## REST API

| Method | Route | Description |
| ------ | ----- | ----------- |
| GET | `/api/dashboard/executive?region=` | Executive KPI cards + chart series |
| GET | `/api/analytics?region=` | Migration analytics — ACR/cores/adoption KPIs + distributions + value cuts over Approved nominations |
| GET | `/api/analytics/timeseries?region=&basis=&granularity=&measure=&splitBy=&fy=&from=&to=` | Fiscal-year Trends pivot — basis×granularity×measure×split, chronological buckets |
| GET | `/api/analytics/timeseries/detail?region=&basis=&granularity=&bucket=&splitBy=&series=` | Nominations behind one Trends bucket (drill-through) |
| GET | `/api/export/analytics?region=&basis=&granularity=&measure=&splitBy=&fy=` | Page-level analytics `.xlsx` — Overview + Distributions + Value cuts + live Trend view |
| GET | `/api/resources?search=&region=&role=&skill=&status=` | Resource hub |
| GET | `/api/resources/{id}` | Resource detail (accounts + upcoming leave) |
| GET | `/api/accounts?search=&region=` | Account hub (name search is case-insensitive) |
| GET | `/api/accounts/{id}` | Account detail (resources + recent activity) |
| GET | `/api/capacity?region=` | Capacity heatmap rows |
| GET | `/api/strategicaccounts?region=` | Strategic account coverage + risk |
| GET | `/api/leave?windowDays=30&region=` | Leave window (30/60/90) |
| POST | `/api/leave` | Add leave; a From→To range expands into one record per day |
| GET | `/api/nominations?region=&status=` | Nomination pipeline |
| GET | `/api/configuration/regions` | Configured regions |
| GET | `/api/configuration/roles` | Configured roles + permissions |
| GET · PUT | `/api/configuration/capacity` | Read / bulk-update optimal accounts per role (rebuilds capacity) |
| GET · POST · PUT | `/api/configuration/segments` | List / add / rename account segments |
| POST | `/api/admin/refresh` | On-demand import of operational data (nominations, accounts, leave, engagement) + capacity rebuild. **Does not touch the resources table.** |
| POST | `/api/admin/upload?kind=` | Upload an `.xlsx` for a source. `kind=resources` imports the resources table directly (one-shot, not staged); other kinds stage into `SourceData/` and refresh. |
| POST | `/api/admin/seed` | Re-run the one-time seed on demand (configuration + resource enrichments). Seeding otherwise runs only once, at first start. |
| POST | `/api/admin/merge-accounts?apply=` | Preview/merge casing-punctuation duplicate accounts into the TPID master (re-points FKs, keeps the variant as an alias). `apply=false` previews. |
| POST | `/api/admin/park-accounts?apply=` | Preview/move no-TPID (non-canonical) accounts out of the master into `ParkedAccount`; skips any a nomination references (no orphans). Reversible. |
| POST | `/api/admin/unpark-accounts` | Restore every parked account (re-creates the account, its resource links, and nomination references). |
| POST | `/api/admin/import-offerings?apply=` | One-time single-source import from "Summary of All Offerings": by TPID+Task Id, enriches matched nominations (offering fields + dates), **creates `Completed` nominations for rows with an Actual End Date**, and refreshes `Account.Segment`. `apply=false` previews. |

---

## Source data ingestion

Place the workbooks in `src/CafPortal.Api/SourceData/` (see the README there). The importer
discovers columns from header text, so it tolerates reordering and minor naming drift, and
supports both **long** and **calendar/matrix** leave layouts. Missing files are skipped and
existing data is preserved.

### Operating model: one-time import, then manage on the web

The intended workflow is a **one-time import** (Resources, account mapping, and Leave) followed by
managing everything directly in the app:

1. Drop the workbooks in `SourceData/`, start the app, and click **Refresh Data** (or `POST /api/admin/refresh`).
2. From then on, add / edit / delete **Resources**, **Accounts**, **resource↔account mappings**, and
   **Leave** in the UI. Capacity is recomputed automatically after any mapping or resource change.

Startup only rebuilds the capacity snapshot — it never re-imports from the workbooks — so web edits
persist across restarts. The nightly import job is disabled by default (`BackgroundRefresh:Enabled`).

**The resources table is never auto-imported.** `Refresh Data` imports only operational data
(nominations, accounts, leave, engagement). Resources are the app's system of record: add / edit /
delete them **manually in the UI**, or do a deliberate **Excel upload** (`POST /api/admin/upload?kind=resources`).
A resources upload is a one-shot import — it is not staged in `SourceData/`, so it never becomes a
recurring source that re-creates deleted or renamed people on the next refresh.

---

## Deploy to Azure App Service (Free Tier / F1)

SQLite lives inside `App_Data`, so no external database is required.

```powershell
# 1. Build the SPA into the API wwwroot
cd client; npm ci; npm run build; cd ..

# 2. Publish the API (includes wwwroot)
dotnet publish src/CafPortal.Api -c Release -o publish

# 3. Create the App Service (Linux F1) and deploy
az group create -n rg-caf-portal -l westeurope
az appservice plan create -g rg-caf-portal -n caf-plan --sku F1 --is-linux
az webapp create -g rg-caf-portal -p caf-plan -n <your-app-name> --runtime "DOTNETCORE:9.0"

# 4. Zip deploy
Compress-Archive -Path publish/* -DestinationPath publish.zip -Force
az webapp deploy -g rg-caf-portal -n <your-app-name> --src-path publish.zip --type zip
```

Notes:

- The app auto-creates `App_Data/` and the SQLite file on first run.
- App Service Free Tier storage is sufficient for 10–30 users.
- To persist source workbooks, upload them to `/home/site/wwwroot/SourceData/` (Kudu) or
  configure `SourceFiles:Directory` to an absolute path under `/home`.

### Configuration (appsettings / App Service settings)

| Key | Default | Purpose |
| --- | ------- | ------- |
| `ConnectionStrings:Default` | `Data Source=App_Data/cafdb.sqlite` | SQLite path |
| `SourceFiles:Directory` | `SourceData` | Workbook folder (relative to content root) |
| `BackgroundRefresh:Enabled` | `false` | Nightly Excel re-import job. Off by default — the web app is the system of record; enable only if you want scheduled re-imports to overwrite web edits. |
| `Cors:Origins` | `http://localhost:5173` | Allowed SPA origins (dev) |

---

## Roadmap / extensibility

- **Phase 2 — Microsoft Entra ID.** `Program.cs` is structured so authentication can be added
  (`AddAuthentication().AddMicrosoftIdentityWebApi(...)`) without redesign; role permissions
  already live in configuration tables.
- **Phase 3 — AI recommendations** (who can take a nomination, skills/leave-aware matching).
- **Phase 4 — Global CAF platform** across EMEA / ASIA / AMER in one governance portal.


DB
src/CafPortal.Api/App_Data/cafdb.sqlite