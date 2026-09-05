# Task: Slice 6 - Window Chrome Fixes, MMC & Admin Tools, Computer Session Diagnostics
Status: Ready for Review
Current Subagent: Reviewer

## Target Files
- `src/Sol/Views/EditTextWindow.xaml`
- `src/Sol/Views/EditTextWindow.xaml.cs`
- `src/Sol/Services/ComputerDiagnosticService.cs`
- `src/Sol.Tests/ComputerDiagnosticServiceTests.cs`
- `src/Sol.Tests/SessionAndWindowChromeSpikeTests.cs`
- `src/Sol/Models/MmcModels.cs`
- `src/Sol/Services/IMmcLookupService.cs`
- `src/Sol/Services/MmcLookupService.cs`
- `src/Sol/Models/AdminCommandModels.cs`
- `src/Sol/Services/IAdminCommandService.cs`
- `src/Sol/Services/AdminCommandService.cs`
- `src/Sol/Views/MmcLookupWindow.xaml`
- `src/Sol/Views/MmcLookupWindow.xaml.cs`
- `src/Sol/Views/AdminCommandsWindow.xaml`
- `src/Sol/Views/AdminCommandsWindow.xaml.cs`
- `src/Sol/Views/ToolsPage.xaml`
- `src/Sol/ViewModels/ToolsViewModel.cs`
- `src/Sol/Views/SettingsPage.xaml`
- `src/Sol/ViewModels/SettingsViewModel.cs`
- `src/Sol/MainWindow.xaml.cs`
- `src/Sol/Helpers/Strings.cs`

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: Session query P/Invoke, WMI regex extraction, and window style spike tests pass)
- [x] Phase 2: Interface contracts, models & core services (Exit gate: IMmcLookupService, IAdminCommandService, ComputerDiagnosticService session fixes compile with 0 warnings)
- [x] Phase 3: UI integration, deprecation removal & localization verification (Exit gate: FSG, Run, QSL removed; MMC & Admin Commands UI integrated; EditTextWindow chrome fixed; zero hardcoded strings)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: `dotnet test` exits 0, 0 build warnings, localization audit green)
