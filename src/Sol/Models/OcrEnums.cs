namespace Sol.Models;

/// <summary>
/// Supported OCR recognition engines.
/// </summary>
public enum OcrEngineKind
{
    /// <summary>
    /// Native Windows Runtime OCR (Windows.Media.Ocr.OcrEngine). Available on Windows 10+.
    /// </summary>
    Windows = 0,

    /// <summary>
    /// Windows AI NPU-powered recognition (Microsoft.Windows.AI.Imaging.TextRecognizer). Available on Copilot+ PCs.
    /// </summary>
    WindowsAi = 1
}

/// <summary>
/// Output granularity or layout kind.
/// </summary>
public enum OcrOutputKind
{
    None = 0,
    Line = 1,
    Paragraph = 2,
    Table = 3
}

/// <summary>
/// Text casing styles for toggle and formatting operations.
/// </summary>
public enum CurrentCase
{
    Lower = 0,
    Camel = 1,
    Upper = 2,
    Title = 3,
    Unknown = 4
}

/// <summary>
/// Position relative to a line of text for insertion or trimming.
/// </summary>
public enum SpotInLine
{
    Beginning = 0,
    End = 1
}
