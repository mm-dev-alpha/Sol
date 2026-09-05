using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graphics.Imaging;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Imaging;
using Sol.Models;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRtOcr = Windows.Media.Ocr;
using OcrResult = Sol.Models.OcrResult;

namespace Sol.Services;

/// <summary>
/// Provides on-device optical character recognition via native Windows Runtime OCR and Windows AI.
/// </summary>
public class OcrService : IOcrService
{
    private bool? _isWinAiAvailable;

    public bool IsWinAiAvailable
    {
        get
        {
            if (_isWinAiAvailable.HasValue)
                return _isWinAiAvailable.Value;

            try
            {
                AIFeatureReadyState readyState = TextRecognizer.GetReadyState();
                _isWinAiAvailable = readyState != AIFeatureReadyState.NotSupportedOnCurrentSystem;
            }
            catch
            {
                _isWinAiAvailable = false;
            }

            return _isWinAiAvailable.Value;
        }
    }

    public IReadOnlyList<OcrLanguageInfo> GetAvailableLanguages(OcrEngineKind? engineFilter = null)
    {
        List<OcrLanguageInfo> languages = [];

        if (engineFilter is null || engineFilter == OcrEngineKind.Windows)
        {
            foreach (Windows.Globalization.Language lang in WinRtOcr.OcrEngine.AvailableRecognizerLanguages)
            {
                bool spaceJoining = IsSpaceJoiningLanguage(lang.LanguageTag);
                languages.Add(new OcrLanguageInfo(
                    lang.LanguageTag,
                    lang.DisplayName,
                    lang.NativeName,
                    OcrEngineKind.Windows,
                    spaceJoining
                ));
            }
        }

        if ((engineFilter is null || engineFilter == OcrEngineKind.WindowsAi) && IsWinAiAvailable)
        {
            languages.Add(new OcrLanguageInfo(
                "ai-auto",
                Sol.Helpers.Strings.S.OcrEngineWindowsAi,
                "Windows AI",
                OcrEngineKind.WindowsAi,
                true
            ));
        }

        return languages;
    }

    public OcrLanguageInfo GetDefaultLanguage()
    {
        IReadOnlyList<OcrLanguageInfo> available = GetAvailableLanguages(OcrEngineKind.Windows);
        if (available.Count == 0)
        {
            return new OcrLanguageInfo("en-US", "English (United States)", "English", OcrEngineKind.Windows, true);
        }

        string currentCulture = CultureInfo.CurrentUICulture.Name;
        OcrLanguageInfo? match = available.FirstOrDefault(l =>
            l.LanguageTag.Equals(currentCulture, StringComparison.OrdinalIgnoreCase) ||
            l.LanguageTag.StartsWith(currentCulture.Split('-')[0], StringComparison.OrdinalIgnoreCase));

        return match ?? available[0];
    }

    public async Task<OcrResult> RecognizeAsync(SoftwareBitmap bitmap, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        language ??= GetDefaultLanguage();

        if (language.Engine == OcrEngineKind.WindowsAi && IsWinAiAvailable)
        {
            OcrResult? aiResult = await TryRecognizeWithWinAiAsync(bitmap, cancellationToken);
            if (aiResult is not null)
                return aiResult;
        }

        return await RecognizeWithWinRtAsync(bitmap, language, cancellationToken);
    }

    public async Task<OcrResult> RecognizeAsync(byte[] imageData, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        if (imageData.Length == 0)
            return new OcrResult(string.Empty, [], 0f, language?.Engine ?? OcrEngineKind.Windows);

        using InMemoryRandomAccessStream stream = new();
        await stream.WriteAsync(imageData.AsBuffer());
        stream.Seek(0);

        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
        using SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        return await RecognizeAsync(softwareBitmap, language, cancellationToken);
    }

    public async Task<string> RecognizeAsTableAsync(SoftwareBitmap bitmap, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default)
    {
        OcrResult result = await RecognizeAsync(bitmap, language, cancellationToken);
        return FormatOcrResultAsTable(result);
    }

    public async Task<string> RecognizeAsTableAsync(byte[] imageData, OcrLanguageInfo? language = null, CancellationToken cancellationToken = default)
    {
        OcrResult result = await RecognizeAsync(imageData, language, cancellationToken);
        return FormatOcrResultAsTable(result);
    }

    #region Internal WinRT Recognition

    private static async Task<OcrResult> RecognizeWithWinRtAsync(SoftwareBitmap bitmap, OcrLanguageInfo language, CancellationToken cancellationToken)
    {
        Windows.Globalization.Language winRtLang = new(language.LanguageTag);
        WinRtOcr.OcrEngine? ocrEngine = WinRtOcr.OcrEngine.TryCreateFromLanguage(winRtLang);
        ocrEngine ??= WinRtOcr.OcrEngine.TryCreateFromUserProfileLanguages();

        if (ocrEngine is null)
        {
            var fallbackLangs = WinRtOcr.OcrEngine.AvailableRecognizerLanguages;
            if (fallbackLangs.Count > 0)
                ocrEngine = WinRtOcr.OcrEngine.TryCreateFromLanguage(fallbackLangs[0]);
        }

        if (ocrEngine is null)
            throw new InvalidOperationException(Sol.Helpers.Strings.OcrEngineInitError(language.LanguageTag));

        SoftwareBitmap workingBitmap = bitmap;
        bool createdNewBitmap = false;

        // WinRT OCR requires SoftwareBitmap in Gray8 or Bgra8 pixel format with Premultiplied or Ignore alpha
        if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || bitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
        {
            workingBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            createdNewBitmap = true;
        }

        // WinRT OCR requires image dimensions between 40x40 and 2600x2600
        if (workingBitmap.PixelWidth < 40 || workingBitmap.PixelHeight < 40)
        {
            workingBitmap = PadSoftwareBitmap(workingBitmap, Math.Max(40, workingBitmap.PixelWidth), Math.Max(40, workingBitmap.PixelHeight));
            createdNewBitmap = true;
        }

        try
        {
            Windows.Media.Ocr.OcrResult winRtResult = await ocrEngine.RecognizeAsync(workingBitmap).AsTask(cancellationToken);

            List<OcrLine> lines = new(winRtResult.Lines.Count);
            foreach (Windows.Media.Ocr.OcrLine winLine in winRtResult.Lines)
            {
                List<OcrWord> words = new(winLine.Words.Count);
                double minX = double.MaxValue, minY = double.MaxValue, maxX = 0, maxY = 0;

                foreach (Windows.Media.Ocr.OcrWord winWord in winLine.Words)
                {
                    Rect wordRect = winWord.BoundingRect;
                    words.Add(new OcrWord(winWord.Text, wordRect));

                    if (wordRect.X < minX) minX = wordRect.X;
                    if (wordRect.Y < minY) minY = wordRect.Y;
                    if (wordRect.X + wordRect.Width > maxX) maxX = wordRect.X + wordRect.Width;
                    if (wordRect.Y + wordRect.Height > maxY) maxY = wordRect.Y + wordRect.Height;
                }

                Rect lineRect = words.Count > 0
                    ? new Rect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY))
                    : Rect.Empty;

                lines.Add(new OcrLine(winLine.Text, words, lineRect));
            }

            float angle = (float)(winRtResult.TextAngle ?? 0.0);
            return new OcrResult(winRtResult.Text, lines, angle, OcrEngineKind.Windows);
        }
        finally
        {
            if (createdNewBitmap && workingBitmap != bitmap)
            {
                workingBitmap.Dispose();
            }
        }
    }

    #endregion

    #region Internal WinAI Recognition

    private static async Task<OcrResult?> TryRecognizeWithWinAiAsync(SoftwareBitmap bitmap, CancellationToken cancellationToken)
    {
        try
        {
            AIFeatureReadyState readyState = TextRecognizer.GetReadyState();
            if (readyState == AIFeatureReadyState.NotReady)
            {
                AIFeatureReadyResult readyResult = await TextRecognizer.EnsureReadyAsync().AsTask(cancellationToken);
                if (readyResult.Status != AIFeatureReadyResultState.Success)
                    return null;
            }

            using TextRecognizer recognizer = await TextRecognizer.CreateAsync().AsTask(cancellationToken);
            using ImageBuffer imageBuffer = ImageBuffer.CreateForSoftwareBitmap(bitmap);

            RecognizedText? recognizedText = recognizer.RecognizeTextFromImage(imageBuffer);
            if (recognizedText?.Lines is null)
                return null;

            StringBuilder fullText = new();
            List<OcrLine> ocrLines = [];

            foreach (RecognizedLine line in recognizedText.Lines)
            {
                if (line is null) continue;
                fullText.AppendLine(line.Text);

                List<OcrWord> words = [];
                double minX = double.MaxValue, minY = double.MaxValue, maxX = 0, maxY = 0;

                if (line.Words is not null)
                {
                    foreach (RecognizedWord word in line.Words)
                    {
                        if (word is null) continue;
                        Rect wordRect = new(
                            word.BoundingBox.TopLeft,
                            word.BoundingBox.BottomRight
                        );
                        words.Add(new OcrWord(word.Text, wordRect));

                        if (wordRect.X < minX) minX = wordRect.X;
                        if (wordRect.Y < minY) minY = wordRect.Y;
                        if (wordRect.X + wordRect.Width > maxX) maxX = wordRect.X + wordRect.Width;
                        if (wordRect.Y + wordRect.Height > maxY) maxY = wordRect.Y + wordRect.Height;
                    }
                }

                Rect lineRect = new(
                    line.BoundingBox.TopLeft,
                    line.BoundingBox.BottomRight
                );

                ocrLines.Add(new OcrLine(line.Text, words, lineRect));
            }

            return new OcrResult(fullText.ToString().TrimEnd(), ocrLines, recognizedText.TextAngle, OcrEngineKind.WindowsAi);
        }
        catch
        {
            return null; // Fall back to WinRT
        }
    }

    #endregion

    #region Helpers & Table Reconstruction

    private static bool IsSpaceJoiningLanguage(string languageTag)
    {
        if (string.IsNullOrEmpty(languageTag))
            return true;

        string prefix = languageTag.Split('-')[0].ToLowerInvariant();
        return prefix != "zh" && prefix != "ja";
    }

    private static SoftwareBitmap PadSoftwareBitmap(SoftwareBitmap source, int minW, int minH)
    {
        int targetW = Math.Max(source.PixelWidth + 16, minW + 16);
        int targetH = Math.Max(source.PixelHeight + 16, minH + 16);

        SoftwareBitmap padded = new(BitmapPixelFormat.Bgra8, targetW, targetH, BitmapAlphaMode.Premultiplied);

        byte[] sourceBuffer = new byte[source.PixelWidth * source.PixelHeight * 4];
        source.CopyToBuffer(sourceBuffer.AsBuffer());

        byte[] destBuffer = new byte[targetW * targetH * 4];
        // Fill background with white (255, 255, 255, 255)
        Array.Fill(destBuffer, (byte)255);

        int offsetX = 8;
        int offsetY = 8;

        for (int row = 0; row < source.PixelHeight; row++)
        {
            int sourceRowStart = row * source.PixelWidth * 4;
            int destRowStart = ((row + offsetY) * targetW + offsetX) * 4;
            int copyBytes = source.PixelWidth * 4;

            System.Buffer.BlockCopy(sourceBuffer, sourceRowStart, destBuffer, destRowStart, copyBytes);
        }

        padded.CopyFromBuffer(destBuffer.AsBuffer());
        return padded;
    }

    public static string FormatOcrResultAsTable(OcrResult result)
    {
        if (result.Lines.Count == 0)
            return string.Empty;

        // Collect all words across all lines
        List<OcrWord> allWords = result.Lines.SelectMany(l => l.Words).ToList();
        if (allWords.Count == 0)
            return result.Text;

        // Determine distinct column buckets by analyzing X-coordinates
        // Group words by horizontal alignment
        List<double> xStarts = allWords.Select(w => w.BoundingBox.X).OrderBy(x => x).ToList();
        List<double> columnBoundaries = [];

        double gapThreshold = 20.0; // Minimum pixel gap between distinct columns
        if (xStarts.Count > 0)
        {
            columnBoundaries.Add(xStarts[0]);
            for (int i = 1; i < xStarts.Count; i++)
            {
                if (xStarts[i] - columnBoundaries[^1] > gapThreshold)
                {
                    columnBoundaries.Add(xStarts[i]);
                }
            }
        }

        StringBuilder tableSb = new();

        foreach (OcrLine line in result.Lines)
        {
            if (line.Words.Count == 0)
                continue;

            // Map each word in this line to its nearest column index
            var wordColumns = new SortedDictionary<int, List<string>>();

            foreach (OcrWord word in line.Words)
            {
                int colIdx = 0;
                double minDiff = double.MaxValue;

                for (int c = 0; c < columnBoundaries.Count; c++)
                {
                    double diff = Math.Abs(word.BoundingBox.X - columnBoundaries[c]);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        colIdx = c;
                    }
                }

                if (!wordColumns.TryGetValue(colIdx, out var list))
                {
                    list = [];
                    wordColumns[colIdx] = list;
                }
                list.Add(word.Text);
            }

            int maxCol = wordColumns.Keys.LastOrDefault();
            StringBuilder lineSb = new();

            for (int c = 0; c <= maxCol; c++)
            {
                if (c > 0)
                    lineSb.Append('\t');

                if (wordColumns.TryGetValue(c, out var wordsInCol))
                {
                    lineSb.Append(string.Join(' ', wordsInCol));
                }
            }

            tableSb.AppendLine(lineSb.ToString());
        }

        return tableSb.ToString().TrimEnd();
    }

    #endregion
}
