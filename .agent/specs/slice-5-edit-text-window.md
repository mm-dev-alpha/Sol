# Specification: Slice 5 - Edit Text Window (ETW) & In-Place Text Manipulation

## 1. Overview
Port the Edit Text Window (ETW) utility from Text-Grab 4.15.0 into Sol:
- A dedicated utility window (`EditTextWindow`) designed for rapid text inspection, manipulation, transformation, and structured table/spreadsheet formatting.
- Serves as the central text processing workbench receiving grabbed text from Fullscreen Grab (FSG) and Grab Frame (GF), or standalone invocation.
- Text Manipulation & Transformations:
  - Deep integration with `ITextTransformService` (supporting all 40+ transformations built in Slice 1: Make Single Line, Trim Each Line, Try To Make Numbers, Try To Make Letters, Fix GUID, Toggle Case, Remove Duplicate Lines, Shuffle Lines, Replace Reserved Characters, Unstack, Add/Remove at Index, Extract Emails/URLs/Numbers, etc.).
- Structured Table & Spreadsheet Mode:
  - Backed by `EditTextTableDocument`: full model supporting TSV, CSV, Markdown tables, plain text, and XML.
  - Table operations: insert/move/delete rows and columns, transpose rows and columns, wrap text in cells, serialize to text or JSON.
  - Table grid view allowing direct editing and formatting of grabbed tabular data.
- Live Calculation Pane:
  - Line-by-line math expression evaluator displaying real-time calculation results alongside editor lines, with aggregate sum/average in status.
- File & Clipboard Operations:
  - Open & Save files (.txt, .csv, .tsv, .md) using WinUI 3 file pickers with `InitializeWithWindow`.
  - "Copy & Close": Copies all text or table to clipboard and closes window.
  - "Close & Insert": Copies text, closes window, and simulates Win32 `SendInput` (Ctrl+V) into the previously active window.
  - "OCR Paste": Reads image from clipboard, invokes `IOcrService`, and inserts extracted text directly at the cursor position.
- Global Hotkey:
  - `Win + Shift + E` (`HOTKEY_EDIT_TEXT_ID = 0x5345`) registered in `MainWindow.xaml.cs`.
- Tools Page & Settings Integration:
  - Dedicated tool card on `ToolsPage.xaml` with launch button, status pill, and shortcut badge.
  - Settings in `SettingsPage.xaml` and `ISettingsService` for Word Wrap, Always on Top, and Hotkey toggle.
- 100% Centralized Localization:
  - All labels, tooltips, titles, placeholders, and status strings centralized in `Sol.Helpers.Strings` (`Strings.S.*`).
- Leak Defense & Resource Management:
  - Proper disposal of window handles, file streams, timers, and event subscriptions.

## 2. Component Architecture
- **Contracts / Enums / Models**:
  - `Sol.Models.EditTextModels`:
    - `EtwEditorMode`: `Text`, `Table`, `Calculation`
    - `EtwStructuredTextFormat`: `PlainText`, `DelimitedText`, `Csv`, `Tsv`, `Xml`
    - `EditTextTableWrappedCell`: Row and column index record
    - `EditTextTableDocument`: Full structured table document parser, row/column mutator, serializer, and dimension tracker
    - `CalculationResult`: Formatted output, line outputs, error count, and aggregate sum/count
- **Services**:
  - `Sol.Services.IEditTextService` / `EditTextService`:
    - Window management: `OpenWindow(string? text = null, EditTextTableDocument? table = null)`
    - Win32 Global Hotkey registration: `Win + Shift + E`
    - Line-by-line expression calculation engine
    - Text insertion helper via Win32 `SendInput`
- **UI & Views**:
  - `EditTextWindow.xaml` / `.xaml.cs`:
    - Custom WinUI 3 titlebar (`AppTitleBar`, Pin/Always-on-top toggle, Close).
    - Top Menu / CommandBar:
      - File (New, Open, Save, Save As, Copy & Close, Close & Insert)
      - Edit (Undo, Redo, Cut, Copy, Paste, OCR Paste, Select All)
      - Transform (Single Line, Trim, Make Numbers, Make Letters, Fix GUID, Toggle Case, Remove Duplicates, Shuffle, Replace Reserved, Unstack, Sort)
      - Mode / View (Toggle Table Mode, Toggle Calculation Pane, Word Wrap, Font Size)
    - Center Canvas:
      - Multi-line `TextBox` with line numbers / status tracking
      - Tabular grid viewer / editor for `EditTextTableDocument`
      - Aligned Calculation results column when Calc Pane is active
    - Bottom Status Bar:
      - Character count, Word count, Line count, Calculation sum/average aggregate
  - `ToolsPage.xaml` & `ToolsViewModel.cs`: Tool card with glyph `&#xE70F;` (Edit), status pill, hotkey badge (`Win + Shift + E`), and launch command.
  - `SettingsPage.xaml` & `SettingsViewModel.cs`: Settings card group for Edit Text preferences.
  - `MainWindow.xaml.cs`: Register global hotkey `Win + Shift + E`.
  - `Strings.cs`: Centralized strings for all tooltips, menu items, headers, and status messages.
- **Testing**:
  - Unit tests covering `EditTextTableDocument` serialization, TSV/CSV/Markdown table parsing, row/column insert/delete/move/transpose.
  - Line-by-line calculation evaluator tests.
  - Service tests for `IEditTextService`.
  - Navigation and Settings tests.
  - Localization audit ensuring zero hardcoded strings.

## 3. Phased Execution Sequence
- Phase 1: Spike & API validation (Exit gate: `EditTextTableDocument` data parsing & row/column operations and calculation evaluation unit tests passing).
- Phase 2: Interface contracts, models, and `EditTextService` implemented with zero compiler warnings.
- Phase 3: `EditTextWindow` WinUI 3 window, `ToolsPage` card, `SettingsPage` settings, and `MainWindow` hotkey registration (`Win + Shift + E`).
- Phase 4: Verification, leak defense, and full xUnit test suite passing with exit code 0 (`dotnet test`), zero warnings.
