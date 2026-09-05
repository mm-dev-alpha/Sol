# Task: Slice 5 - Edit Text Window (ETW) & In-Place Text Manipulation
Status: Ready for Review
Current Subagent: Reviewer

## Target Files
- `src/Sol/Models/EditTextModels.cs`
- `src/Sol/Services/IEditTextService.cs`
- `src/Sol/Services/EditTextService.cs`
- `src/Sol/Views/EditTextWindow.xaml`
- `src/Sol/Views/EditTextWindow.xaml.cs`
- `src/Sol/Views/ToolsPage.xaml`
- `src/Sol/ViewModels/ToolsViewModel.cs`
- `src/Sol/Services/ISettingsService.cs`
- `src/Sol/Services/SettingsService.cs`
- `src/Sol/ViewModels/SettingsViewModel.cs`
- `src/Sol/Views/SettingsPage.xaml`
- `src/Sol/MainWindow.xaml.cs`
- `src/Sol/App.xaml.cs`
- `src/Sol/Helpers/Strings.cs`
- `src/Sol.Tests/EditTextTableDocumentTests.cs`
- `src/Sol.Tests/EditTextCalculationTests.cs`
- `src/Sol.Tests/EditTextServiceTests.cs`
- `src/Sol.Tests/ToolsNavigationTests.cs`
- `src/Sol.Tests/ToolsSettingsTests.cs`
- `src/Sol.Tests/LocalizationAuditTests.cs`

## Checklist
- [x] Phase 1: Spike & API validation (Exit gate: Table document data parsing, transposing, and line calculation spike tests passing)
- [x] Phase 2: Interface contracts, models & core edit text service (Exit gate: EditTextModels, IEditTextService, EditTextService implemented with zero compiler warnings)
- [x] Phase 3: UI integration & localization verification (Exit gate: EditTextWindow with custom title bar, menu/transforms, ToolsPage card, MainWindow hotkey registration Win+Shift+E, settings, and Strings.S.* complete)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0, no window/stream leaks, dotnet build 0 warnings)
