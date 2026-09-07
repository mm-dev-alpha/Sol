using System.Collections.Generic;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service responsible for Grab Frame viewport-to-screen coordinate calculations,
/// table column partitioning, word search filtering, and output text formatting.
/// </summary>
public interface IGrabFrameService : System.IDisposable
{
    /// <summary>
    /// Calculates the physical desktop screen coordinates for the viewfinder viewport,
    /// accounting for window position, local element offsets, and DPI scaling.
    /// </summary>
    ScreenBounds CalculateViewportScreenRect(
        int windowScreenX,
        int windowScreenY,
        double localViewportX,
        double localViewportY,
        double localViewportWidth,
        double localViewportHeight,
        double dpiScale,
        ScreenBounds? virtualScreenBounds = null);

    /// <summary>
    /// Partitions recognized OCR words into rows and columns defined by vertical column divider X-coordinates.
    /// </summary>
    GrabFrameTableResult ParseTable(
        IReadOnlyList<OcrWord> words,
        IReadOnlyList<double> columnDividers,
        double viewportWidth);

    /// <summary>
    /// Filters OCR words matching a search query, tagging matched words.
    /// </summary>
    IReadOnlyList<GrabFrameWordItem> FilterWords(
        IReadOnlyList<OcrWord> words,
        string searchQuery,
        bool exactMatch = false);

    /// <summary>
    /// Formats recognized text according to the specified Grab Frame mode.
    /// </summary>
    string FormatExtractedText(string rawText, GrabFrameMode mode);

    /// <summary>
    /// Calculates the adaptive responsive visibility state of toolbar controls for the specified window width.
    /// </summary>
    GrabFrameToolbarState CalculateToolbarState(double windowWidth);

    /// <summary>
    /// Registers the global hotkey (Win + Shift + G) targeting the specified window handle.
    /// </summary>
    bool RegisterGlobalHotkey(System.IntPtr hWnd);

    /// <summary>
    /// Unregisters the global hotkey.
    /// </summary>
    bool UnregisterGlobalHotkey(System.IntPtr hWnd);
}
