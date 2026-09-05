# Task: Fix MMC Administrative Consoles and Daily Admin Commands
Status: Ready for Review
Current Subagent: Reviewer

## Target Files
- `src/Sol/Views/MmcLookupWindow.xaml`
- `src/Sol/Views/MmcLookupWindow.xaml.cs`
- `src/Sol/Views/AdminCommandsWindow.xaml`
- `src/Sol/Views/AdminCommandsWindow.xaml.cs`
- `src/Sol.Tests/XamlResourceAuditTests.cs`

## Checklist
- [x] Phase 1: Spike & Root Cause Validation (Confirmed XamlParseException from StaticResource AccentButtonStyle and premature CurrentInstance assignment)
- [x] Phase 2: Interface contracts & regression test (Add XamlResourceAuditTests ensuring ThemeResource usage)
- [x] Phase 3: UI & Code-behind fixes (Fix AccentButtonStyle, Window Title binding, and constructor lifecycle in MmcLookupWindow and AdminCommandsWindow)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: `dotnet test` exits 0, 0 build warnings, regression tests green)
