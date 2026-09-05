# Specification: Slice 4 - Grab Frame (GF) Floating Viewfinder Window

## 1. Overview
Port the Grab Frame (GF) floating viewfinder utility from Text-Grab 4.15.0 into Sol:
- A resizable, movable floating window (`GrabFrameWindow`) with a transparent/see-through viewfinder viewport.
- WinUI 3 transparent system backdrop (`TransparentTintBackdrop`) allowing users to position the viewfinder over any screen content (documents, videos, remote sessions, web pages).
- Viewfinder viewport coordinate mapping: Translates local viewport rectangle to physical desktop coordinates via `ClientToScreen` and per-monitor DPI scaling.
- Text & Table Extraction pipeline:
  - Captures viewport pixels via `IScreenCaptureService.CaptureRegion`.
  - Runs OCR via `IOcrService` (supporting both WinRT and Windows AI engines).
  - Word Borders Overlay: Renders subtle interactive bounding boxes (`WordBorder`) over recognized words on the viewfinder canvas. Clicking a word copies it to clipboard.
  - Interactive Table Mode (T): Renders draggable vertical column dividers across the viewfinder. Users can drag dividers, click to add, or right-click to delete. When Grab is triggered in Table mode, detected words are partitioned into column buckets and sorted into rows to generate clean tab-delimited text (`\t`).
  - Search & Highlight: Interactive search bar in bottom toolbar. Typing filters and highlights matching words in the viewfinder, displaying a live match count. Grabbing with search active grabs matched text.
  - Freeze Screen Mode (F): Freezes the current screen content within the viewfinder into an `Image` element, pausing desktop updates so users can inspect, scroll, or grab static text at leisure.
  - Auto-OCR Mode: Optional setting to automatically trigger OCR after window move or resize (with 500ms debounce).
- Global hotkey invocation (`Win + Shift + G`) registered in `MainWindow.xaml.cs`.
- Tools page card in `ToolsPage.xaml` with launch button, status pill, and shortcut badge.
- Settings in `SettingsPage.xaml` and `ISettingsService` for Auto-OCR, Table Mode, Single Line, Always on Top, and Auto Paste.
- 100% centralized localization via `Sol.Helpers.Strings` (`Strings.S.*`) with zero hardcoded strings.
- Deterministic leak defense: deterministic disposal of GDI bitmaps, OCR tokens, timers, and window event handlers.

## 2. Component Architecture
- **Contracts / Enums / Models**:
  - `Sol.Models.GrabFrameModels`:
    - `GrabFrameMode`: `Standard`, `SingleLine`, `Table`
    - `GrabFrameColumnDivider`: Relative X-position, dragging state, index
    - `GrabFrameWordItem`: Word text, bounding rectangle relative to viewfinder, is-matched state (for search), is-selected state
    - `GrabFrameTableResult`: Parsed grid of cells, column count, row count, formatted tab-separated text
- **Services & Helpers**:
  - `Sol.Helpers.TransparentTintBackdrop`: Custom `SystemBackdrop` that enables clean WinUI 3 window transparency via DWM blur-behind and DirectComposition.
  - `Sol.Services.IGrabFrameService` / `GrabFrameService`:
    - Viewport coordinate mapping: Converts XAML element bounds and HWND to physical screen rectangle.
    - Table column partitioning: Groups `OcrWord` elements into columns based on divider X-coordinates and aligns them by line Y-coordinates into tabular rows.
    - Search filtering: Filters words matching a query with case-insensitive and exact-match modes.
- **UI & Views**:
  - `GrabFrameWindow.xaml` / `.xaml.cs`:
    - Floating resizable window with custom compact title bar (Auto-OCR toggle, Pin/Topmost toggle, Title, Close).
    - Center Viewfinder area: Transparent client area with accent border, `Image` element (for Freeze mode), and `Canvas` for word bounding boxes and draggable column dividers.
    - Bottom Toolbar: Search box, match count indicator, OCR language selector, Re-OCR button (Ctrl+R), Freeze toggle (F), Table Mode toggle (T), Single Line toggle (S), Grab button (Ctrl+G / Enter).
  - `ToolsPage.xaml` & `ToolsViewModel.cs`: Dedicated Grab Frame tool card with glyph `&#xE790;` (Photo/Frame), status pill, hotkey badge (`Win + Shift + G`), and launch command.
  - `SettingsPage.xaml` & `SettingsViewModel.cs`: Settings card group for Grab Frame preferences.
  - `MainWindow.xaml.cs`: Register global hotkey `Win + Shift + G` (`HOTKEY_GRAB_FRAME_ID = 0x5347`).
  - `Strings.cs`: Centralized strings for all tooltips, labels, titles, and status messages.
- **Testing**:
  - Unit tests covering physical coordinate calculation with various DPIs, table divider grouping logic, search query filtering, settings persistence, and tools navigation.

## 3. Phased Execution Sequence
- Phase 1: Spike & API validation verifying DWM transparency and viewport-to-screen coordinate math.
- Phase 2: Interface contracts, models (`GrabFrameModels`), and `GrabFrameService` implemented with zero compiler warnings.
- Phase 3: `GrabFrameWindow`, `ToolsPage` card, `MainWindow` hotkey registration, settings, and `Strings.cs` localization wired up.
- Phase 4: Leak defense verified, full xUnit test suite passing with exit code 0 (`dotnet test`), zero compiler warnings.
