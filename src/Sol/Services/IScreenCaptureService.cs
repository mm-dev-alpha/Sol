using System;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service responsible for multi-monitor desktop capture, image cropping, and word hit-testing for Fullscreen Grab.
/// </summary>
public interface IScreenCaptureService : IDisposable
{
    /// <summary>
    /// Gets the bounding rectangle of the virtual desktop spanning all active displays.
    /// </summary>
    ScreenBounds GetVirtualScreenBounds();

    /// <summary>
    /// Captures a specified region of the virtual desktop into a 32bpp BGRA BMP byte array.
    /// </summary>
    byte[] CaptureRegion(int x, int y, int width, int height);

    /// <summary>
    /// Crops a sub-rectangle from a 32bpp BGRA BMP byte array.
    /// </summary>
    byte[] CropBmp(byte[] bmpBytes, int cropX, int cropY, int cropWidth, int cropHeight);

    /// <summary>
    /// Ensures image meets minimum width and height for OCR recognition by padding if necessary.
    /// </summary>
    byte[] PadBmp(byte[] bmpBytes, int minWidth = 64, int minHeight = 64);

    /// <summary>
    /// Creates a valid 54-byte BMP header for 32bpp BGRA raw pixel data.
    /// </summary>
    byte[] CreateBmpFromPixels(byte[] bgraPixels, int width, int height);

    /// <summary>
    /// Hit-tests a point against an OCR result to find the word underneath.
    /// </summary>
    string? FindWordAtPoint(OcrResult ocrResult, double localX, double localY);

    /// <summary>
    /// Formats recognized text, optionally collapsing into a single line.
    /// </summary>
    string FormatExtractedText(string rawText, bool singleLine = false);

    /// <summary>
    /// Registers the global hotkey (Win + Shift + F) targeting the specified window handle.
    /// </summary>
    bool RegisterGlobalHotkey(IntPtr hWnd);

    /// <summary>
    /// Unregisters the global hotkey.
    /// </summary>
    bool UnregisterGlobalHotkey(IntPtr hWnd);
}
