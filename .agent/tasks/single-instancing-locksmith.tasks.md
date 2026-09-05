# Task: Single-Instancing and Command Redirection
Status: Ready for Review
Current Subagent: Implementer

## Target Files
- `src/Sol/Sol.csproj`
- `src/Sol/Program.cs`
- `src/Sol/Helpers/CommandLineHelper.cs`
- `src/Sol/App.xaml.cs`
- `src/Sol/Views/FileLocksmithWindow.xaml.cs`
- `src/Sol.Tests/CommandLineHelperTests.cs`

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: DISABLE_XAML_GENERATED_MAIN and custom entry point compiles)
- [x] Phase 2: Interface contracts & data flow (Exit gate: CommandLineHelper implemented and xUnit tests pass)
- [x] Phase 3: UI integration & localization verification (Exit gate: Program.cs, App.xaml.cs, FileLocksmithWindow integrated)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: `dotnet test` exits 0, zero warnings)
