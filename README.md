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
the initial capacity snapshot. SQLite file: `src/CafPortal.Api/App_Data/cafdb.sqlite`.

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
| POST | `/api/admin/refresh` | On-demand import + capacity rebuild |

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
