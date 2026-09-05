# Specification: Slice 6 - Window Chrome Fixes, MMC & Admin Tools, Computer Session Diagnostics

## 1. Overview & Objectives
This slice delivers four targeted enhancements:
1. **Fix EditTextWindow Top-Right Chrome**:
   - Eliminate overlapping system caption buttons (min/max/close) by configuring `OverlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false)`.
   - Provide custom dragging via `ReleaseCapture` and `WM_NCLBUTTONDOWN` (`HTCAPTION`).
   - Add a dedicated custom close button to the custom title bar.
2. **Deprecate & Remove Fullscreen Grab (FSG)**:
   - Cleanly remove `FullscreenGrabWindow`, related settings, models, tests, hotkeys, and Tools page card.
   - Retain Grab Frame (`Win + Shift + G`) as the sole screen capture and OCR tool.
3. **Replace Sol Run and Quick Simple Lookup**:
   - **MMC (Core Administrative Consoles Lookup)** (`Alt + Space` / `Win + Shift + M`):
     - Fast searchable catalog of 40+ Microsoft administrative tools (`.msc`, `.cpl`, admin `.exe`).
     - Supports marking favorites (pinned to top), launching via `Process.Start`, and copying the command.
   - **Daily Admin Commands (CMD & PowerShell)** (`Win + Shift + C`):
     - Curated searchable catalog of 50+ essential administrative commands across AD, Networking, Group Policy, Diagnostics, Security, and Remote Management.
     - Supports marking favorites (pinned to top) and copying commands directly to clipboard.
4. **Fix Computer Workspace Logged-On Users**:
   - Fix swapped `Antecedent` / `Dependent` regex parsing in `QuerySessionsWmi` for `Win32_LoggedOnUser`.
   - Add `Win32_ComputerSystem.UserName` query for console user detection.
   - Implement native `wtsapi32.dll` (`WTSEnumerateSessions` / `WTSQuerySessionInformation`) for reliable local session discovery.

## 2. Technical Architecture & Phasing
- **Phase 1: Spike & Native Validation (Window Styles & Session Query)**
  - Validate native P/Invoke `WTSEnumerateSessions` and `WTSQuerySessionInformation` on local machine.
  - Validate WMI regex parsing for `Win32_LoggedOnUser` against real WMI output formats (Antecedent = Win32_Account, Dependent = Win32_LogonSession).
  - Validate window style rules and `HTCAPTION` drag behavior.
- **Phase 2: Contracts, Models & Core Services**
  - Implement `IMmcLookupService` & `MmcLookupService`.
  - Implement `IAdminCommandService` & `AdminCommandService`.
  - Fix `ComputerDiagnosticService.cs` session queries (WMI + WTS fallback).
- **Phase 3: Removal of Deprecated Features & UI Implementation**
  - Delete `FullscreenGrabWindow`, `RunLauncherWindow`, `QuickLookupWindow` and related services.
  - Create `MmcLookupWindow.xaml(.cs)` and `AdminCommandsWindow.xaml(.cs)`.
  - Update `EditTextWindow` title bar and caption controls.
  - Wire `ToolsPage`, `SettingsPage`, `MainWindow` hotkeys, and `Strings.cs`.
- **Phase 4: Verification, Test Migration & Leak Defense**
  - Add unit tests for `MmcLookupService` and `AdminCommandService`.
  - Update `ComputerDiagnosticServiceTests`, `ToolsSettingsTests`, `ToolsNavigationTests`.
  - Verify 0 build warnings, exit code 0 on `dotnet test`, and `LocalizationAuditTests` passing.
