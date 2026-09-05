namespace Sol.Models;

/// <summary>
/// Describes a language supported by an OCR engine.
/// </summary>
/// <param name="LanguageTag">BCP-47 language tag (e.g. "en-US", "de-DE", "zh-Hans").</param>
/// <param name="DisplayName">Localized display name in the current user's UI language.</param>
/// <param name="NativeName">Native display name in the target language.</param>
/// <param name="Engine">Which engine supports this language.</param>
/// <param name="IsSpaceJoining">Whether words in this language are joined by spaces (false for Chinese/Japanese).</param>
public record OcrLanguageInfo(
    string LanguageTag,
    string DisplayName,
    string NativeName,
    OcrEngineKind Engine,
    bool IsSpaceJoining
);
