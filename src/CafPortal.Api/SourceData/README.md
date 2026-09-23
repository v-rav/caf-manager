# Source Data Folder

Drop the three source workbooks here for automatic nightly ingestion (02:00 local)
or on-demand via the **Refresh Data** button / `POST /api/admin/refresh`.

| File | Purpose |
| ---- | ------- |
| `App Migration_Region_wise Mapping.xlsx` | Resources, skills, regions, roles, account mapping |
| `LeaveCal.xlsx` | Planned leave / holidays (long or calendar layout) |
| `Time-hunt_Tracking.xlsx` | Customer meetings, engagement, demand signals |

Column names are discovered from the header row (case-insensitive, partial match),
so minor layout drift is tolerated. If a file is absent, the importer preserves the
existing seeded/demo data and only rebuilds the capacity snapshot.

> These files are git-ignored because they contain operational data.
