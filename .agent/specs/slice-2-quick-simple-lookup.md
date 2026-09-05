# Specification: Slice 2 - Quick Simple Lookup (QSL)

## 1. Overview
Port the Quick Simple Lookup (QSL) tool from Text-Grab 4.15.0 into Sol:
- Fast custom snippet recall / dictionary memory tool with instant filtering, multi-word matching, and acronym searching
- Multi-action clipboard workflows: copy value (Enter), copy key (Ctrl+Enter), copy row (Shift+Enter), copy all filtered (Shift+Ctrl+Enter)
- Auto-paste simulation (SendInput Ctrl+V) into the previously active application with configurable delay
- Executable commands (> powershell/cmd command) and web links (http, link icon)
- Persistent CSV storage with import/export capabilities and default IT/admin sample snippets
- WinUI 3 overlay window with Mica backdrop, instant keyboard navigation, and seamless integration into Sol's ToolsPage.xaml
- Global hotkey invocation (Win + Shift + Q) and full integration into Sol's DI container and localization (Strings.S.*)

## 2. Component Architecture
- **Contracts / Enums / Models**:
  - Sol.Models.LookupItemKind: Simple, Link, Command, Dynamic, GrabTemplate, EditWindow
  - Sol.Models.LookupItem: ShortValue, LongValue, Kind, FirstLettersString, CSV serialization / parsing methods
- **Services**:
  - ILookupService / LookupService:
    - Storage management: Local CSV persistence in %LOCALAPPDATA%\Sol\QuickLookupData.csv
    - Search algorithms: Multi-word token matching, acronym/initials matching, regex matching
    - Clipboard & input injection: Win32 SendInput key injection for automated paste into previous window
    - Command execution: Safe CLI execution for command items
    - CSV Import/Export
    - Hotkey & window toggle event dispatching
- **UI & Integration**:
  - QuickLookupWindow.xaml / .xaml.cs: Centered/top-aligned floating launcher with Mica backdrop, responsive search, list view, keyboard accelerators, and contextual actions
  - ToolsPage.xaml & ToolsViewModel.cs: Dedicated Quick Simple Lookup card with status pill, hotkey badge, and launch button
  - MainWindow.xaml.cs: WM_HOTKEY registration and routing for HOTKEY_QUICK_LOOKUP_ID
  - ISettingsService & SettingsService.cs: Settings persistence for auto-paste, delay, and enable toggles
  - Strings.cs: Centralized localization for all labels, placeholders, dialogs, and errors
- **Testing**:
  - Unit tests covering CSV round-tripping, multi-word query filtering, acronym resolution, regex search, collection mutations, and default seeding

## 3. Exit Gates
- Phase 1: Spike & API validation verifying CSV parsing, acronym search, and SendInput struct layouts.
- Phase 2: Complete models, interfaces, and LookupService implemented.
- Phase 3: QuickLookupWindow, ToolsPage card, MainWindow hotkey registration, settings, and Strings.cs localization wired up.
- Phase 4: Full xUnit test suite passing with exit code 0 (dotnet test), zero compiler warnings, no memory or hotkey leaks.
