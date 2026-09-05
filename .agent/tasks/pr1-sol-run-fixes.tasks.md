# Task: PR 1 - Sol Run Search & Floating Launcher UX Fixes (Bugfix & Polish)
Status: Completed
Current Subagent: Implementer

## Target Files
- `src/Sol/Services/AppSearchProvider.cs`
- `src/Sol/Services/RunSearchService.cs`
- `src/Sol/Views/RunLauncherWindow.xaml`
- `src/Sol/Views/RunLauncherWindow.xaml.cs`
- `src/Sol/MainWindow.xaml.cs`
- `src/Sol/Helpers/Strings.cs`
- `src/Sol.Tests/RunSearchServiceTests.cs`

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: OverlappedPresenter SetBorderAndTitleBar and command execution tested)
- [x] Phase 2: Interface contracts & data flow (Exit gate: ShellRunSearchProvider implemented, AppSearchProvider improved, tests pass)
- [x] Phase 3: Immediate focus & fixed height launcher (Exit gate: Instant typing on Alt+Space without mouse click, results never clipped)
- [x] Phase 4: Registry App Paths & Start Menu / Desktop comprehensive indexing (Exit gate: matches all installed apps like PowerToys Run)
- [x] Phase 5: Verification & Leak Defense (Exit gate: `dotnet test` exits 0, release build 0 warnings)
