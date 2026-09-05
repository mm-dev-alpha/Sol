# Task: PR 2 - Awake & Tools Default Settings Policy
Status: Completed
Current Subagent: Implementer

## Target Files
- src/Sol/Services/SettingsService.cs
- src/Sol/Services/AwakeService.cs
- src/Sol/ViewModels/SettingsViewModel.cs
- src/Sol/ViewModels/ToolsViewModel.cs
- src/Sol.Tests/AwakeServiceTests.cs
- src/Sol.Tests/ToolsSettingsTests.cs
- src/Sol.Tests/ToolsNavigationTests.cs
- src/Sol.Tests/ComputerDiagnosticServiceTests.cs

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: Awake service and settings default values verified)
- [x] Phase 2: Interface contracts & state transitions (Exit gate: SettingsService, AwakeService, ViewModels defaulted to false)
- [x] Phase 3: UI integration & localization verification (Exit gate: Zero hardcoded strings, bindings checked)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0, zero compiler warnings)
