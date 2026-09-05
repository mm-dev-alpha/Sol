# Task: Slice 4 - Grab Frame (GF) Floating Viewfinder Window
Status: Complete
Current Subagent: Reviewer/Verifier

## Target Files
- `src/Sol/Models/GrabFrameModels.cs`
- `src/Sol/Helpers/TransparentTintBackdrop.cs`
- `src/Sol/Services/IGrabFrameService.cs`
- `src/Sol/Services/GrabFrameService.cs`
- `src/Sol/Views/GrabFrameWindow.xaml`
- `src/Sol/Views/GrabFrameWindow.xaml.cs`
- `src/Sol/Views/ToolsPage.xaml`
- `src/Sol/ViewModels/ToolsViewModel.cs`
- `src/Sol/Services/ISettingsService.cs`
- `src/Sol/Services/SettingsService.cs`
- `src/Sol/ViewModels/SettingsViewModel.cs`
- `src/Sol/Views/SettingsPage.xaml`
- `src/Sol/MainWindow.xaml.cs`
- `src/Sol/App.xaml.cs`
- `src/Sol/Helpers/Strings.cs`
- `src/Sol.Tests/GrabFrameSpikeTests.cs`
- `src/Sol.Tests/GrabFrameServiceTests.cs`
- `src/Sol.Tests/ToolsNavigationTests.cs`
- `src/Sol.Tests/ToolsSettingsTests.cs`
- `src/Sol.Tests/LocalizationAuditTests.cs`

## Checklist
- [x] Phase 1: Spike & API validation (Exit gate: Viewport coordinate translation & table column partitioning spike unit tests passing)
- [x] Phase 2: Interface contracts, models & core grab frame service (Exit gate: GrabFrameModels, IGrabFrameService, GrabFrameService implemented with zero compiler warnings)
- [x] Phase 3: UI integration & localization verification (Exit gate: GrabFrameWindow with TransparentTintBackdrop, ToolsPage card, MainWindow hotkey registration Win+Shift+G, settings, and Strings.S.* complete)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0 with all 298 tests passing, no timer/GDI leaks, dotnet build 0 warnings)
