using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Sol.Models;

/// <summary>
/// Represents a recognized single word and its bounding box on the input image.
/// </summary>
public record OcrWord(string Text, Rect BoundingBox);

/// <summary>
/// Represents a line of recognized words and its overall bounding box.
/// </summary>
public record OcrLine(string Text, IReadOnlyList<OcrWord> Words, Rect BoundingBox);

/// <summary>
/// Represents the complete output of an OCR operation with layout and orientation metadata.
/// </summary>
public record OcrResult(
    string Text,
    IReadOnlyList<OcrLine> Lines,
    float Angle,
    OcrEngineKind Engine
)
{
    /// <summary>
    /// Gets a flattened list of all recognized words across all lines.
    /// </summary>
    public IReadOnlyList<OcrWord> Words => Lines != null ? Lines.SelectMany(l => l.Words).ToList() : System.Array.Empty<OcrWord>();
}

/// <summary>
/// High-level OCR output container with raw and cleaned variants.
/// </summary>
public record OcrOutput(
    OcrOutputKind Kind,
    string RawText,
    string CleanedText,
    OcrEngineKind Engine
);
