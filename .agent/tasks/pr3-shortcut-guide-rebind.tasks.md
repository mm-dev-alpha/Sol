# Task: PR 3 - Shortcut Guide Hotkey Rebind ('Win + Shift + ?')
Status: Completed
Current Subagent: Implementer

## Target Files
- src/Sol/Helpers/Strings.cs
- src/Sol/ViewModels/ToolsViewModel.cs
- src/Sol/Views/ShortcutGuideOverlayWindow.xaml.cs
- src/Sol/MainWindow.xaml.cs
- src/Sol.Tests/ToolsNavigationTests.cs

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: VK_OEM_2 hotkey combination and overlay toggle verified)
- [x] Phase 2: Interface contracts & state transitions (Exit gate: CurrentInstance tracking and toggle-close implemented)
- [x] Phase 3: UI integration & localization verification (Exit gate: Strings updated to 'Win + Shift + ?', zero hardcoded strings)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0, release build 0 warnings)
