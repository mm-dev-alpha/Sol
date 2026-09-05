# Task: Slice 2 - Quick Simple Lookup (QSL)
Status: Ready for Review
Current Subagent: Implementer

## Target Files
- src/Sol/Models/LookupItem.cs
- src/Sol/Services/ILookupService.cs
- src/Sol/Services/LookupService.cs
- src/Sol/Views/QuickLookupWindow.xaml
- src/Sol/Views/QuickLookupWindow.xaml.cs
- src/Sol/Views/ToolsPage.xaml
- src/Sol/ViewModels/ToolsViewModel.cs
- src/Sol/Services/ISettingsService.cs
- src/Sol/Services/SettingsService.cs
- src/Sol/ViewModels/SettingsViewModel.cs
- src/Sol/Views/SettingsPage.xaml
- src/Sol/MainWindow.xaml.cs
- src/Sol/App.xaml.cs
- src/Sol/Helpers/Strings.cs
- src/Sol.Tests/LookupServiceTests.cs
- src/Sol.Tests/ToolsNavigationTests.cs
- src/Sol.Tests/ToolsSettingsTests.cs

## Checklist
- [x] Phase 1: Spike & API validation (Exit gate: CSV format and search algorithm spike tests passing)
- [x] Phase 2: Interface contracts, models & core service (Exit gate: LookupItem, ILookupService, LookupService implemented with zero compiler warnings)
- [x] Phase 3: UI integration & localization verification (Exit gate: QuickLookupWindow, ToolsPage card, MainWindow hotkey registration, settings, and Strings.S.* complete)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0, no unmanaged/event leaks, dotnet build 0 warnings)
