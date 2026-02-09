# Repository Map

This file is a quick orientation map for future work in this repository.

## High-Level Structure

- `src/Client/ReportManager.Client`:
  WPF end-user app for report browsing, filtering, sorting, paging, and export.
- `src/Server/ReportManager.Server`:
  Core server logic (manifest building, query execution, export services).
- `src/Server/ReportManager.Host`:
  Console host for WCF/REST endpoints.
- `src/Contracts/ReportManager.DefinitionModel`:
  JSON models used for report definitions and presets.
- `src/Contracts/ReportManager.Shared`:
  DTOs and shared constants used by Client/Server/Admin.
- `src/Lib/ReportManager.Lib.Wpf`:
  Shared WPF MVVM helpers (`NotificationObject`, `RelayCommand`, etc.).
- `tools/ReportAdmin.App`:
  WPF admin app for editing report SQL/definition files.
- `tools/ReportAdmin.Core`:
  SQL parsing/generation and DB introspection for ReportAdmin.
- `db`:
  SQL schema and sample report scripts.

## Main Runtime Flow

1. Admin (`tools/ReportAdmin.App`) edits report definition and texts, then generates SQL file.
2. SQL is applied to DB (`ReportDefinitions` + system presets data).
3. Server (`src/Server/ReportManager.Server`) loads definition JSON from DB and builds manifest/query results.
4. Client (`src/Client/ReportManager.Client`) loads manifest, then sends query requests (conditions, sorting, paging).

## Important Entry Points

- Manifest loading:
  `src/Client/ReportManager.Client/ViewModels/ReportViewModel.cs`
- Condition editor:
  `src/Client/ReportManager.Client/ViewModels/QueryConditionsViewModel.cs`
  `src/Client/ReportManager.Client/ViewModels/QueryConditionViewModel.cs`
  `src/Client/ReportManager.Client/Views/ReportConditionsView.xaml`
- Sorting editor:
  `src/Client/ReportManager.Client/ViewModels/SortSpecsViewModel.cs`
  `src/Client/ReportManager.Client/ViewModels/SortSpecViewModel.cs`
  `src/Client/ReportManager.Client/Views/ReportSortsView.xaml`
- Column picker dialog:
  `src/Client/ReportManager.Client/ViewModels/ColumnPickerDialogViewModel.cs`
  `src/Client/ReportManager.Client/Views/ColumnPickerDialog.xaml`
- Report definition model:
  `src/Contracts/ReportManager.DefinitionModel/Models/ReportDefinition/ReportColumnJson.cs`
- Text key naming:
  `src/Contracts/ReportManager.Shared/KnownTextKeys.cs`
- Server manifest mapping:
  `src/Server/ReportManager.Server/Services/ReportService.cs`
- Admin validation + expected texts:
  `tools/ReportAdmin.App/ViewModels/ReportViewModel.cs`
  `tools/ReportAdmin.App/ViewModels/TextsViewModel.cs`

## Category Path and Localization (Current Behavior)

- Column categories are modeled as path segments:
  `ReportColumnJson.CategoryPath : List<string>`.
- Each segment is localized independently using:
  `colcat.<segment>`.
- Example:
  Category path `["Contract", "Invoice"]` maps to:
  - `colcat.Contract`
  - `colcat.Invoice`
- Server resolves each segment and sends localized `CategoryPath` in manifest.
- Client builds category trees from `CategoryPath`.

See also: `docs/COLUMN_CATEGORIES.md`.

## UI Behavior for Condition/Sort Column Selection

- Condition and sort rows use a simple inline editor:
  current selected column label + `Select column...` button.
- The button opens a searchable tree dialog with nested categories.
- This replaced the previous in-row category ComboBox approach.

## Practical Notes (Environment)

- WPF projects target Windows desktop stack.
- On macOS, full restore/build for WPF projects may not work.
- For this repo, macOS sessions often skip `restore/build` and focus on code edits/review.

## Useful Search Shortcuts

- Category path usage:
  `rg -n "CategoryPath|colcat\\.|GetColumnCategoryKey" src tools`
- Condition/sort picker usage:
  `rg -n "SelectColumnCommand|ColumnPickerDialog|SelectedColumnLabel" src/Client/ReportManager.Client`
- Text generation and validation:
  `rg -n "GetExpectedTexts|ValidateReportDefinition|TextsResolver" tools src`
