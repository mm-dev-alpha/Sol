# ADR-002: Grab Frame Viewfinder Transparency and Coordinate Capture
- Date: 2026-09-04
- Status: Accepted
- Context: Porting Text-Grab's Grab Frame into Sol requires a floating, resizable viewfinder window with a see-through/transparent viewport on desktop WinUI 3 / Windows App SDK, capable of capturing the physical desktop pixels underneath and rendering live OCR word borders, search highlights, and draggable table column dividers.
- Decision:
  1. Window Transparency: Utilize WinUI 3's `Microsoft.UI.Xaml.Media.SystemBackdrop` interop with DWM blur-behind configuration (`DwmEnableBlurBehindWindow` with a minimal region and `DwmExtendFrameIntoClientArea` with zero margins) and a transparent composition color brush. This provides a transparent window background where the desktop behind the viewfinder is clearly visible while keeping controls and borders fully opaque and interactive.
  2. Coordinate Mapping: Map the viewfinder's local XAML coordinates to physical desktop screen coordinates via `ClientToScreen` and per-monitor DPI scaling (`GetDpiForWindow` / 96.0). Screen capture executes via `IScreenCaptureService.CaptureRegion(x, y, width, height)` using GDI DIBSection capture.
  3. Interactive Table Extraction: Vertical column dividers are tracked on a XAML `Canvas` as relative X-offsets. When extracting table data, detected `OcrWord` elements are categorized into column buckets determined by these divider boundaries and ordered by row Y-coordinates, producing clean tab-delimited text.
  4. Freeze Mode: Direct bitmap freezing by capturing the viewport bitmap once, setting it as an `Image.Source` in the viewport, and pausing auto-OCR until unfreezing.
- Trade-offs:
  - DirectComposition transparent system backdrop requires Windows 10 (1809+) or Windows 11 with DWM enabled, which matches Sol's target platform requirements (`net10.0-windows10.0.26100.0`, min version 10.0.17763.0).
  - During live transparent mode, clicking inside the viewfinder without selecting a word or divider will interact with the Grab Frame canvas rather than passing through to underlying desktop windows (same behavior as Text-Grab).
