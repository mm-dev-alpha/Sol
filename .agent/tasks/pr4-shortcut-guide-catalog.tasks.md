# Task: PR 4 - Shortcut Guide Expanded Non-Windows Shortcuts Catalog
Status: Completed
Current Subagent: Implementer

## Target Files
- src/Sol/Helpers/Strings.cs
- src/Sol/Services/ShortcutGuideService.cs
- src/Sol.Tests/ShortcutGuideServiceTests.cs

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: Keyboard shortcut matrix categorized and verified)
- [x] Phase 2: Interface contracts & data flow (Exit gate: Strings.cs and ShortcutGuideService expanded with non-Win shortcuts)
- [x] Phase 3: UI integration & localization verification (Exit gate: Overlay rendering verified with chips and localized descriptions)
- [x] Phase 4: Leak defense & xUnit test pass (Exit gate: dotnet test exits 0, release build 0 warnings)
