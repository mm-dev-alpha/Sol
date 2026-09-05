# ADR-003: Edit Text Window Architecture & In-Place Text Processing
- Date: 2026-09-05
- Status: Accepted
- Context: Porting Text-Grab's `EditTextWindow` into Sol requires handling rich multi-line text editing, 40+ text transformations (`ITextTransformService`), structured tabular data parsing (`EditTextTableDocument`), live calculation evaluation, clipboard OCR pasting (`IOcrService`), and Win32 `SendInput` auto-pasting back into the target active application. Sol is pure WinUI 3 / Windows App SDK (net10.0-windows10.0.26100.0) with zero WPF dependencies.
- Decision:
  1. Window Model: Use standalone WinUI 3 `Window` with custom titlebar extending into content area (`ExtendsContentIntoTitleBar = true`), enabling seamless Mica/desktop backdrop and compact header styling.
  2. Data Modeling: Extract and port `EditTextTableDocument` as an isolated pure C# model without UI dependencies, handling TSV, CSV, XML, and Markdown table conversion, dimension management, row/column reordering, and transposing.
  3. Transformation Pipeline: Direct connection between UI command bars and `ITextTransformService` methods built in Slice 1.
  4. Calculation Engine: Implement line-by-line mathematical expression evaluation supporting standard arithmetic and aggregations (sum, average, count) reusing Sol's expression evaluation patterns.
  5. Close & Insert: Implement via Win32 `SendInput` (Ctrl+V) after window deactivation, reusing the proven mechanism from `LookupService`.
  6. File Pickers: Use `Windows.Storage.Pickers.FileOpenPicker` and `FileSavePicker` with `WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd)` to avoid WinUI 3 desktop picker crashes.
  7. Global Hotkey: Register `Win + Shift + E` (`HOTKEY_EDIT_TEXT_ID = 0x5345`) in `MainWindow.xaml.cs` alongside existing hotkeys.
- Trade-offs:
  - Rich text formatting (RTF/WPF FlowDocument) is intentionally excluded in favor of clean plain-text and markdown/structured table support, aligning with modern developer workflow and avoiding heavy legacy WPF dependencies.
  - Calculation engine focuses on high-speed arithmetic and line-by-line expressions without introducing heavy external third-party expression libraries like NCalc.
