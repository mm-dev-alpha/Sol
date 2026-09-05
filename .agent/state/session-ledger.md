# Session Ledger

> Structured, append-mostly project memory. This file is the source of truth once
> older conversation turns are compacted — treat it as more reliable than anything
> paraphrased in-context. Keep entries short, factual, and one-per-line. Do not
> collapse this file into prose; add rows/lines, don't rewrite existing ones except
> to mark status changes (e.g. "Open" -> "Resolved").

## Active Goal
- Porting Text-Grab 4.15.0 into Sol (following GEMINI.md). Slices 1, 2, 3, 4, and 5 complete and verified. Ready for next slice or final audit.

## Constraints
<!-- APPEND-ONLY. Verbatim, never paraphrased, never deleted, never reworded.
     Every line here must trace back to something the user actually said. -->
- "please integrate/port the full functionality of \"D:\Antigravity\Projekte\Sol\samples\Text-Grab-4.15.0\" into Sol. Please stick to the guidelines and rules of GEMINI.md"
- "1. Yes, include it, we use both WinAI and WinRT OCR 2. Barcode is not needed."
- Strict centralized strings (zero hardcoded strings): ALL user-facing text maintained centrally via Sol.Helpers.Strings (Strings.S.*).
- WinUI 3 & XAML compilation: Inside <DataTemplate>, always use fully qualified static namespace (e.g. {x:Bind local:Strings.S.PropertyName}).
- Keep <XamlCompilerUILanguage>en-US</XamlCompilerUILanguage> in Sol.csproj at all times.
- Zero compiler warnings on dotnet build (Sol.csproj and Sol.Tests.csproj).
- Win32 resource leak defense: immediate verification and deterministic disposal of GDI bitmaps, DCs, SafeHandles, and hotkey unregistration.
- Never combine multiple complex utilities into a single mega-plan: infrastructure-first progression (Shared Base/Contracts -> Low-hanging/Lightweight features -> Complex engines/Overlay systems -> Unified configuration).
- Phased structure per slice: Phase 1 (Spike & API Validation) -> Phase 2 (Contracts & Data Flow) -> Phase 3 (UI & Integration) -> Phase 4 (Verification & Leak Defense).

## Decisions Log
<!-- One line per decision: what was decided + one-line rationale + rough timestamp/turn marker. Append new entries; don't edit old ones. -->
- [x] Excluded Tesseract OCR engine — external binary distribution complexity, WinRT + Windows AI sufficient — 2026-09-04
- [x] Excluded barcode/QR decoding — explicit user instruction ("Barcode is not needed") — 2026-09-04
- [x] Slice 1: Implemented headless OCR engine and 40+ text transformation methods (IOcrService, ITextTransformService) with zero WPF dependencies — verified with 260 tests — 2026-09-04
- [x] Slice 2: Implemented Quick Simple Lookup (QSL) launcher window with acronym matching, CSV storage, and Win32 SendInput auto-paste (Win + Shift + Q) — verified with tests — 2026-09-04
- [x] Slice 3: Implemented Fullscreen Grab (FSG) multi-monitor desktop capture, marquee selection, word hit-testing, and floating toolbar (Win + Shift + F) — verified with tests — 2026-09-04
- [x] Removed ElevationShadow from FullscreenGrabWindow.xaml — non-existent in WinUI 3, threw runtime XamlParseException — 2026-09-04
- [x] Wrapped AppWindow.IsShownInSwitchers = false in try-catch — throws NotImplementedException on desktop Windows App SDK — 2026-09-04
- [x] Replaced StoreAsync().GetAwaiter().GetResult() with MemoryStream.AsRandomAccessStream() in FullscreenGrabWindow.xaml.cs — prevents UI dispatcher deadlock — 2026-09-04
- [x] Added 400ms activation grace period in overlay windows — prevents mouse-up focus restoration from MainWindow instantly closing overlay — 2026-09-04
- [x] Used Win32 SetWindowPos instead of AppWindow.MoveAndResize for fullscreen overlay — correctly handles negative virtual desktop multi-monitor coordinates — 2026-09-04
- [x] Added MOD_NOREPEAT (0x4000) and German keyboard layout fallback for VK_OEM_4 (? on Shift + ß) across all global hotkey services — 2026-09-04
- [x] Slice 4: Implemented Grab Frame (GF) floating viewfinder window with transparent backdrop (DWM blur-behind / margins), live word bounding box overlays, draggable table column dividers, and auto-OCR debouncing (Win + Shift + G) — verified with 298 tests — 2026-09-05
- [x] Slice 5: Implemented Edit Text Window (ETW) text workbench with custom title bar, multi-line editor, full ITextTransformService operations, structured table/spreadsheet document editor (TSV/CSV/Markdown/XML/transposition), line-by-line math expression evaluator with aggregate sum/avg, OCR clipboard pasting, Win32 SendInput insertion, ToolsPage card, Settings, and global hotkey (Win + Shift + E) — verified with 329 tests — 2026-09-05
- [x] Slice 6: Completed Phase 1 spike tests verifying native wtsapi32.dll session queries, WMI Win32_LoggedOnUser regex extraction for both standard/swapped properties, Win32_ComputerSystem.UserName parser, and window drag/style constants — verified with 346 tests — 2026-09-05
- [x] Slice 6: Completed Phase 2 implementing MmcModels, IMmcLookupService, MmcLookupService (40+ tools catalog & favorites), AdminCommandModels, IAdminCommandService, AdminCommandService (50+ commands & favorites), and ComputerDiagnosticService session discovery fixes (WMI regex + Win32_ComputerSystem.UserName + native WTS local enumeration fallback) — verified with 364 tests — 2026-09-05

## Rejected Approaches
<!-- So they don't get silently re-proposed later. One line each: what was considered + why it was rejected. -->
- Referencing WPF/System.Drawing assemblies — Rejected because Sol is pure WinUI 3 / Windows App SDK.
- AppWindow.IsShownInSwitchers on desktop WinUI 3 — Throws NotImplementedException at runtime.
- Calling async WinRT APIs synchronously via GetAwaiter().GetResult() on the UI thread — Deadlocks the WinUI dispatcher.
- Instant close on WindowActivationState.Deactivated without grace period — Closed popups immediately when clicked via Launch button.
- AppWindow.MoveAndResize for multi-monitor overlay spanning negative coordinates — Fails or crops incorrectly on virtual desktop bounds.
- Custom SystemBackdrop with Microsoft.UI.Composition brush assigned to Windows.UI.Composition target — Fails due to WinRT projection type mismatch in Windows App SDK 1.x. Use DwmExtendFrameIntoClientArea with null backdrop brush instead.

## Open Questions / Unresolved Issues
<!-- Anything still outstanding: unresolved errors, undecided design questions, blocked work. Move an item to Decisions Log (or delete) once it's actually resolved. -->
- None. Slices 1, 2, 3, 4, and 5 are complete and verified. Ready for user review or next tasks.

## File / Module Touch Map
<!-- path -> current state -> last action taken. Keep this current. -->
| Path | State | Last action |
|---|---|---|
| `src/Sol/Models/OcrEnums.cs` | Valid | Added OCR engine and casing enums |
| `src/Sol/Models/OcrResult.cs` | Valid | Added OcrWord, OcrLine, OcrResult (with Words property), OcrOutput models |
| `src/Sol/Models/OcrLanguageInfo.cs` | Valid | Added language descriptor model |
| `src/Sol/Models/LookupItem.cs` | Valid | Added lookup model, acronym generator, CSV serialization |
| `src/Sol/Models/FsgModels.cs` | Valid | Added FsgMode, FsgSelectionStyle, ScreenBounds, FullscreenCaptureResult |
| `src/Sol/Models/GrabFrameModels.cs` | Valid | Added GrabFrameMode, GrabFrameColumnDivider, GrabFrameWordItem, GrabFrameTableResult |
| `src/Sol/Models/EditTextModels.cs` | Valid | Added EtwEditorMode, EtwStructuredTextFormat, EditTextTableDocument, CalculationResult |
| `src/Sol/Services/IOcrService.cs` | Valid | OCR interface supporting WinRT + Windows AI + table extraction |
| `src/Sol/Services/OcrService.cs` | Valid | Implemented WinRT OCR, Furigana filtering, ideal scaling, table parsing |
| `src/Sol/Services/ITextTransformService.cs` | Valid | Text transformation interface with email, url, number extraction |
| `src/Sol/Services/TextTransformService.cs` | Valid | Implemented 40+ string transformation methods and error corrections |
| `src/Sol/Services/ILookupService.cs` | Valid | Quick lookup interface with TryInsertTextAsync |
| `src/Sol/Services/LookupService.cs` | Valid | Implemented fuzzy/acronym search, CSV store, Win32 SendInput paste |
| `src/Sol/Services/IScreenCaptureService.cs` | Valid | Screen capture interface |
| `src/Sol/Services/ScreenCaptureService.cs` | Valid | Implemented GDI DIBSection capture, cropping, padding, word hit-testing |
| `src/Sol/Services/IGrabFrameService.cs` | Valid | Grab frame viewport and table calculation interface |
| `src/Sol/Services/GrabFrameService.cs` | Valid | Implemented viewport coordinate mapping, table partitioning, search filtering, hotkey |
| `src/Sol/Services/IEditTextService.cs` | Valid | Edit text interface with calculation engine and SendInput text insertion |
| `src/Sol/Services/EditTextService.cs` | Valid | Implemented math evaluator, SendInput insertion, hotkey (Win+Shift+E), OpenRequested event |
| `src/Sol/Helpers/TransparentTintBackdrop.cs` | Valid | Implemented DWM blur-behind / frame extension backdrop for viewfinder |
| `src/Sol/Services/ISettingsService.cs` | Valid | Added settings for QSL, FSG, GF, ETW, and OCR preferences |
| `src/Sol/Services/SettingsService.cs` | Valid | Implemented persistent storage for all tools settings |
| `src/Sol/Views/QuickLookupWindow.xaml(.cs)` | Valid | WinUI 3 launcher window with 400ms activation grace period |
| `src/Sol/Views/FullscreenGrabWindow.xaml(.cs)` | Valid | Multi-monitor overlay with SetWindowPos, in-memory stream, and grace period |
| `src/Sol/Views/GrabFrameWindow.xaml(.cs)` | Valid | Floating viewfinder window with interactive dividers and live word overlays |
| `src/Sol/Views/EditTextWindow.xaml(.cs)` | Valid | WinUI 3 editor window with table grid, calculation pane, OCR paste, transforms |
| `src/Sol/Views/ToolsPage.xaml` | Valid | Added QSL, FSG, GF, and ETW tool cards with launch buttons |
| `src/Sol/ViewModels/ToolsViewModel.cs` | Valid | Added launch commands for QSL, FSG, GF, and ETW |
| `src/Sol/Views/SettingsPage.xaml` | Valid | Added settings cards for QSL, FSG, GF, and ETW |
| `src/Sol/ViewModels/SettingsViewModel.cs` | Valid | Wired settings properties for QSL, FSG, GF, and ETW |
| `src/Sol/MainWindow.xaml.cs` | Valid | Registered global hotkeys (Win+Shift+Q, Win+Shift+F, Win+Shift+G, Win+Shift+E), WndProc subclass |
| `src/Sol/Models/MmcModels.cs` | Valid | Added MmcToolItem and MmcCategory enums |
| `src/Sol/Models/AdminCommandModels.cs` | Valid | Added AdminCommandItem, AdminShellType, AdminCommandCategory |
| `src/Sol/Services/IMmcLookupService.cs` | Valid | Added MMC lookup service interface |
| `src/Sol/Services/MmcLookupService.cs` | Valid | Implemented 40+ tool catalog, search, favorites persistence |
| `src/Sol/Services/IAdminCommandService.cs` | Valid | Added Admin Command service interface |
| `src/Sol/Services/AdminCommandService.cs` | Valid | Implemented 50+ command catalog, search, favorites persistence |
| `src/Sol/Services/ComputerDiagnosticService.cs` | Valid | Fixed WMI regex parsing, added ComputerSystem.UserName & native WTS |
| `src/Sol/App.xaml.cs` | Valid | Registered IMmcLookupService and IAdminCommandService in DI container |
| `src/Sol/Helpers/Strings.cs` | Valid | Centralized strings for QSL, FSG, GF, ETW, OCR (zero hardcoded strings) |
| `src/Sol.Tests/SessionAndWindowChromeSpikeTests.cs` | Valid | Added 17 spike tests for WTS sessions, WMI parsing, and window styles |
| `src/Sol.Tests/MmcLookupServiceTests.cs` | Valid | Added unit tests for MMC lookup search, filtering, and favorites |
| `src/Sol.Tests/AdminCommandServiceTests.cs` | Valid | Added unit tests for Admin Command search, filtering, and favorites |
| `src/Sol.Tests/` | 364 Tests Passing | Added tests for OCR, TextTransform, Lookup, ScreenCapture, FSG, GF, ETW, Sessions, MMC, AdminCommands, Settings |

## Pending TODOs
- [x] Create .agent/specs/slice-5-edit-text-window.md specification.
- [x] Create .agent/tasks/slice-5-edit-text-window.tasks.md task execution board.
- [x] Create .agent/decisions/ADR-003-edit-text-window.md.
- [x] Receive user approval on implementation plan.
- [x] Implement Phase 1: Spike & Win32/API validation (Spike tests for table document and calculation engine passing).
- [x] Implement Phase 2: Interface contracts, models & EditTextService.
- [x] Implement Phase 3: WinUI 3 editor window (EditTextWindow), ToolsPage card, Settings, hotkey (Win + Shift + E).
- [x] Implement Phase 4: Leak defense and automated xUnit tests (dotnet test exit 0: 329 passing, zero warnings).

---
_Last updated by compaction pass: 2026-09-05 00:08:00_
