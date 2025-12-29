# ReportManager

Simple implementation of:
- WCF (.NET Framework 4.8) report service
- WPF client (DataGrid) with separate *server query panel* (DB filters/sort) + grid still supports local UX
- SQL scripts to create DB + sample view + report definition/preset in JSON

## 1) Create DB
Run scripts in order on SQL Server:

- db/01_CreateDb.sql
- db/02_Schema.sql
- db/03_Seed.sql
- db/Reports/Contracts.sql

## 2) Configure connection string
Edit:
- src/ReportManager.Host/App.config
Set `connectionStrings/ReportDb`.

## 3) Run
1. Start `ReportManager.Host` (console). It self-hosts WCF at:
   - http://localhost:8733/ReportService
   - http://localhost:8733/ReportDownloadService
2. Start `ReportManager.Client` (WPF).

Default ReportKey is `Contracts`.

## Notes
- Query paging is implemented with OFFSET/FETCH and hard limit PageSize <= 500.
- IN uses individual parameters (demo). For production, switch to TVP for larger lists.
- SQL lookup is evaluated inside GetReportManifest (as requested).

# ReportAdmin (.NET Framework 4.8 WPF)

Admin tool to manage report definitions + system presets as **one SQL file per report**.
Edits should be done **only via this tool**, not by hand.

## Key decisions
- Reports are versioned SQL files (`*.sql`), one per report
- SQL is generated in a strict template so it can be parsed reliably using ScriptDom.
- JSON persistence uses Newtonsoft.Json with:
  - camelCase JSON
  - enums as strings (stable and readable)

## Features
- Open folder with report SQL files
- Parse `@ReportKey`, `@ViewName`, `@DefinitionJson`, and system presets (`@PresetKey_1`, `@PresetJson_1`, ...)
- Edit:
  - Header (key/name/view/version)
  - Columns (grid)
  - Lookups (SQL)
  - Texts per culture (dictionary editor)
  - System presets (key/name/default + preset JSON editor)
- Generate SQL (deterministic ordering) and save `{ReportKey}.sql`
- Apply the generated SQL to a DB (for local testing)

## Presets identity
PresetKey is editable (generated as `{ReportKey}_{Guid}` by default).
PresetId is derived deterministically from PresetKey during generation (clean git diffs).

## Run
Open `ReportAdmin.sln` and run `ReportAdmin.App`.
A sample `Reports/Contracts.sql` is included for quick start.
