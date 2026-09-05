# Task: Slice 3 - Fullscreen Grab (FSG) Overlay
Status: Ready for Review
Current Subagent: Implementer

## Target Files
- src/Sol/Models/FsgModels.cs
- src/Sol/Services/IScreenCaptureService.cs
- src/Sol/Services/ScreenCaptureService.cs
- src/Sol/Views/FullscreenGrabWindow.xaml
- src/Sol/Views/FullscreenGrabWindow.xaml.cs
- src/Sol/Views/ToolsPage.xaml
- src/Sol/ViewModels/ToolsViewModel.cs
- src/Sol/Services/ISettingsService.cs
- src/Sol/Services/SettingsService.cs
- src/Sol/ViewModels/SettingsViewModel.cs
- src/Sol/Views/SettingsPage.xaml
- src/Sol/MainWindow.xaml.cs
- src/Sol/App.xaml.cs
- src/Sol/Helpers/Strings.cs
- src/Sol.Tests/ScreenCaptureServiceTests.cs
- src/Sol.Tests/FullscreenGrabFormattingTests.cs
- src/Sol.Tests/ToolsNavigationTests.cs
- src/Sol.Tests/ToolsSettingsTests.cs
- src/Sol.Tests/LookupServiceTests.cs
- src/Sol.Tests/ComputerDiagnosticServiceTests.cs

## Checklist
- [x] Phase 1: Spike & API validation (Exit gate: BMP encoder and coordinate crop unit spike test passing)
- [x] Phase 2: Interface contracts, models & core capture service (Exit gate: FsgModels, IScreenCaptureService, ScreenCaptureService implemented with zero warnings)
- [x] Phase 3: UI integration & localization verification (Exit gate: FullscreenGrabWindow, ToolsPage card, MainWindow hotkey registration, settings, and Strings.S.* complete)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0 with 280 passing tests, no GDI/event leaks, dotnet build 0 warnings)
