using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;
using Windows.Graphics.Imaging;

namespace Sol.Services;

/// <summary>
/// Service providing on-device optical character recognition via WinRT OCR and Windows AI.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Gets whether Windows AI (NPU-accelerated) recognition is supported on the current hardware.
    /// </summary>
    bool IsWinAiAvailable { get; }

    /// <summary>
    /// Enumerates all available OCR languages for the specified engine or all engines.
    /// </summary>
    IReadOnlyList<OcrLanguageInfo> GetAvailableLanguages(OcrEngineKind? engineFilter = null);

    /// <summary>
    /// Gets the default language for recognition (typically matching current UI or input language).
    /// </summary>
    OcrLanguageInfo GetDefaultLanguage();

    /// <summary>
    /// Performs OCR on a SoftwareBitmap image.
    /// </summary>
    Task<OcrResult> RecognizeAsync(SoftwareBitmap bitmap, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs OCR on an encoded image byte array (PNG, JPEG, BMP).
    /// </summary>
    Task<OcrResult> RecognizeAsync(byte[] imageData, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs OCR on a SoftwareBitmap and structures the output as a tab-delimited table.
    /// </summary>
    Task<string> RecognizeAsTableAsync(SoftwareBitmap bitmap, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs OCR on an encoded image byte array and structures the output as a tab-delimited table.
    /// </summary>
    Task<string> RecognizeAsTableAsync(byte[] imageData, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default);
}
