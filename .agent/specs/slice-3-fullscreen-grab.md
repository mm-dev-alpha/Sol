# Specification: Slice 3 - Fullscreen Grab (FSG) Overlay

## 1. Overview
Port the Fullscreen Grab (FSG) overlay tool from Text-Grab 4.15.0 into Sol:
- Transparent multi-monitor overlay window in WinUI 3 spanning active displays.
- Instant freeze-frame screen capture of the desktop behind the overlay using high-performance Win32 GDI DIBSection capture.
- Marquee drag-selection rectangle with live visual feedback and border cutout.
- Single-click word detection: clicking on a word without dragging automatically identifies the clicked word using OCR bounding boxes (`OcrWord.BoundingBox.Contains`) and grabs it.
- Floating toolbar with capture mode toggles:
  - Standard multiline OCR (N)
  - Single-line OCR (S)
  - Table OCR mode (T) (tab-delimited `\t`)
  - Language picker (WinRT + Windows AI engines from `IOcrService`)
  - Freeze screen toggle (F)
  - Action buttons: Copy to Clipboard, Send to Edit Window (E), Cancel (Esc)
- Post-grab action pipeline:
  - Instant copy to clipboard with formatting
  - Auto-paste / insertion simulation via `LookupService.SimulatePasteAsync` if enabled
- Global hotkey invocation (`Win + Shift + F`) and integration into Sol's `ToolsPage.xaml` card and `ISettingsService`.
- 100% centralized localization via `Sol.Helpers.Strings` (`Strings.S.*`) with zero hardcoded strings.

## 2. Component Architecture
- **Contracts / Enums / Models**:
  - `Sol.Models.FsgSelectionStyle`: `Region`, `Window`, `Table`
  - `Sol.Models.FsgMode`: `Standard`, `SingleLine`, `Table`
  - `Sol.Models.FullscreenCaptureResult`: `CaptureRegion`, `Mode`, `ExtractedText`, `IsSuccess`
- **Services**:
  - `IScreenCaptureService` / `ScreenCaptureService`:
    - Screen metrics: Virtual desktop bounds `(X, Y, Width, Height)` across all connected monitors.
    - Screen capture: Direct GDI DIBSection capture of full virtual screen or specific rectangle to 32bpp BGRA BMP bytes.
    - Cropping & Padding: High-speed sub-rectangle buffer cropping and minimum 64x64 padding for optimal OCR accuracy.
    - Single-word hit-testing: Extracts clicked word from `OcrResult` based on coordinates.
- **UI & Integration**:
  - `FullscreenGrabWindow.xaml` / `.xaml.cs`:
    - Fullscreen borderless `OverlappedPresenter` window (`IsAlwaysOnTop = true`, `IsShownInSwitchers = false`).
    - Background `Image` rendering frozen desktop screenshot with subtle dark tint overlay (`BackgroundBrush.Opacity = 0.2`).
    - Pointer interaction: `PointerPressed`, `PointerMoved`, `PointerReleased` tracking selection rectangle on `Canvas`.
    - Floating action toolbar centered at top with mode toggles, language combo, freeze toggle, and cancel button.
    - Keyboard navigation: Esc (Cancel), Enter (Commit), S (Single-line), N (Normal), T (Table), F (Freeze).
  - `ToolsPage.xaml` & `ToolsViewModel.cs`: Dedicated Fullscreen Grab tool card with glyph `&#xE898;` (Camera / Crop), status pill, hotkey badge, and launch button.
  - `MainWindow.xaml.cs`: Register global hotkey `Win + Shift + F` (`HOTKEY_FULLSCREEN_GRAB_ID = 0x5346`).
  - `ISettingsService` & `SettingsService.cs`: Preferences for `FullscreenGrabShadeOverlay`, `FullscreenGrabSingleLine`, `FullscreenGrabTableMode`, `FullscreenGrabAutoPaste`, `FullscreenGrabLanguage`.
  - `Strings.cs`: Centralized strings for all tooltips, labels, titles, error notices, and status messages.
- **Testing**:
  - Unit tests covering BMP header serialization, rectangle cropping, image padding, word hit-testing, text post-processing, settings persistence, and tool card registration.

## 3. Exit Gates
- Phase 1: Spike & API validation verifying GDI DIB capture, BMP encoding, and word hit-test calculations.
- Phase 2: Interface contracts, models, and `ScreenCaptureService` implemented with zero compiler warnings.
- Phase 3: `FullscreenGrabWindow`, `ToolsPage` card, `MainWindow` hotkey registration, settings, and `Strings.cs` localization wired up.
- Phase 4: Leak defense verified, full xUnit test suite passing with exit code 0 (`dotnet test`), zero compiler warnings.
