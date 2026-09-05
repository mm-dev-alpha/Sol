using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sol.Models;
using Sol.Services;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Xunit;

namespace Sol.Tests;

public class OcrServiceTests
{
    private readonly IOcrService _ocrService = new OcrService();

    [Fact]
    public void GetAvailableLanguages_ReturnsLanguagesWithoutThrowing()
    {
        var languages = _ocrService.GetAvailableLanguages();
        Assert.NotNull(languages);
        // On Windows 10/11, at least one OCR language pack or the user profile language is present
        Assert.True(languages.Count > 0, "Expected at least one OCR language available on Windows.");
        
        foreach (var lang in languages)
        {
            Assert.False(string.IsNullOrWhiteSpace(lang.LanguageTag));
            Assert.False(string.IsNullOrWhiteSpace(lang.DisplayName));
        }
    }

    [Fact]
    public void GetDefaultLanguage_ReturnsValidLanguage()
    {
        var defaultLang = _ocrService.GetDefaultLanguage();
        Assert.NotNull(defaultLang);
        Assert.False(string.IsNullOrWhiteSpace(defaultLang.LanguageTag));
        Assert.False(string.IsNullOrWhiteSpace(defaultLang.DisplayName));
    }

    [Fact]
    public void IsWinAiAvailable_DoesNotThrow()
    {
        // Must evaluate gracefully on any architecture (ARM64 or x64) without throwing
        bool isAvailable = _ocrService.IsWinAiAvailable;
        // On non-Copilot+ PCs, this will be false, which is expected
        Assert.True(isAvailable || !isAvailable);
    }

    [Fact]
    public void FormatOcrResultAsTable_ReconstructsTabularColumns()
    {
        // Arrange: 2 lines with 2 columns aligned horizontally
        // Line 1: Col 1 at X=10, Col 2 at X=150
        // Line 2: Col 1 at X=12, Col 2 at X=155
        var wordsLine1 = new List<OcrWord>
        {
            new("Item1", new Rect(10, 10, 40, 15)),
            new("$10.00", new Rect(150, 10, 50, 15))
        };
        var line1 = new OcrLine("Item1 $10.00", wordsLine1, new Rect(10, 10, 190, 15));

        var wordsLine2 = new List<OcrWord>
        {
            new("Item2", new Rect(12, 30, 40, 15)),
            new("$20.00", new Rect(155, 30, 50, 15))
        };
        var line2 = new OcrLine("Item2 $20.00", wordsLine2, new Rect(12, 30, 193, 15));

        var ocrResult = new OcrResult(
            "Item1 $10.00\nItem2 $20.00",
            [line1, line2],
            0f,
            OcrEngineKind.Windows
        );

        // Act
        string table = OcrService.FormatOcrResultAsTable(ocrResult);

        // Assert
        Assert.Contains("\t", table);
        string[] tableLines = table.Split(Environment.NewLine);
        Assert.Equal(2, tableLines.Length);

        string[] colsLine1 = tableLines[0].Split('\t');
        string[] colsLine2 = tableLines[1].Split('\t');

        Assert.Equal(2, colsLine1.Length);
        Assert.Equal("Item1", colsLine1[0]);
        Assert.Equal("$10.00", colsLine1[1]);

        Assert.Equal(2, colsLine2.Length);
        Assert.Equal("Item2", colsLine2[0]);
        Assert.Equal("$20.00", colsLine2[1]);
    }

    [Fact]
    public async Task RecognizeAsync_EmptyByteArray_ReturnsEmptyResult()
    {
        var result = await _ocrService.RecognizeAsync([]);
        Assert.NotNull(result);
        Assert.Empty(result.Text);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task RecognizeAsync_WithSoftwareBitmap_ExecutesAndDisposesCleanly()
    {
        // Create a 100x60 blank Bgra8 SoftwareBitmap
        using SoftwareBitmap bitmap = new(BitmapPixelFormat.Bgra8, 100, 60, BitmapAlphaMode.Premultiplied);

        var result = await _ocrService.RecognizeAsync(bitmap);
        Assert.NotNull(result);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task RecognizeAsTableAsync_WithSoftwareBitmap_ExecutesCleanly()
    {
        using SoftwareBitmap bitmap = new(BitmapPixelFormat.Bgra8, 100, 60, BitmapAlphaMode.Premultiplied);

        string table = await _ocrService.RecognizeAsTableAsync(bitmap);
        Assert.NotNull(table);
    }
}
