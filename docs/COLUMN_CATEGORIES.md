# Column Categories

This document captures the current category model and related localization/UI behavior.

## Data Model

- Definition model:
  `src/Contracts/ReportManager.DefinitionModel/Models/ReportDefinition/ReportColumnJson.cs`
- Field:
  `CategoryPath : List<string>`

Meaning:
- `[]` -> no category.
- `["Contract"]` -> one-level category.
- `["Customer", "Contract"]` -> nested category path.

In ReportAdmin UI:
- Category path is edited as text using `/` as separator.
- Parsing logic is in:
  `tools/ReportAdmin.App/ViewModels/ColumnViewModel.cs`
  (`ParseCategoryPath`).

## Text Keys for Category Localization

Key builder:
- `src/Contracts/ReportManager.Shared/KnownTextKeys.cs`
- `GetColumnCategoryKey(categorySegment)` -> `colcat.<segment>`

Rule:
- Every segment in `CategoryPath` has its own text key.
- Keys are not generated from full path concatenation.

Example:
- Category path: `["Contract", "Invoice"]`
- Expected keys:
  - `colcat.Contract`
  - `colcat.Invoice`

## Where Keys Are Generated/Validated

- Text regeneration in ReportAdmin:
  `tools/ReportAdmin.App/ViewModels/TextsViewModel.cs`
  (`GetExpectedTexts`).
- Save-time definition validation:
  `tools/ReportAdmin.App/ViewModels/ReportViewModel.cs`
  (`ValidateReportDefinition`).

Both places derive expected category keys from all `CategoryPath` segments.

## Server Resolution and Manifest Output

File:
- `src/Server/ReportManager.Server/Services/ReportService.cs`

Behavior in `GetReportManifest`:
- For each column, iterate category segments in `CategoryPath`.
- Resolve each segment via `TextsResolver.ResolveText(..., colcat.<segment>, ...)`.
- If key is missing, fallback to raw segment.
- Emit final localized list into `ReportColumnManifestDto.CategoryPath`.

DTO:
- `src/Contracts/ReportManager.Shared/Dto/ReportColumnManifestDto.cs`

## Client Behavior (Condition/Sort Column Picker)

Manifest mapping:
- `src/Client/ReportManager.Client/ViewModels/ReportViewModel.cs`
  maps manifest `CategoryPath` into `ColumnOption.CategoryPath`.

Picker tree:
- `src/Client/ReportManager.Client/ViewModels/ColumnPickerDialogViewModel.cs`
  builds hierarchical nodes from `CategoryPath`.
- `src/Client/ReportManager.Client/Views/ColumnPickerDialog.xaml`
  renders searchable tree UI.

Inline editor:
- `src/Client/ReportManager.Client/Views/ReportConditionsView.xaml`
- `src/Client/ReportManager.Client/Views/ReportSortsView.xaml`
- Each row shows selected column label and a `Select column...` button.

## Compatibility Note

The current concept assumes segment-based localization keys (`colcat.<segment>`).
If old definitions contain path-based keys (for example `colcat.Contract/Invoice`),
they should be migrated to segment keys for consistent behavior.
