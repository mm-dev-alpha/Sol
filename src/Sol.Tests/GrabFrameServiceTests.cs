using System;
using System.Collections.Generic;
using Sol.Models;
using Sol.Services;
using Windows.Foundation;
using Xunit;

namespace Sol.Tests;

public class GrabFrameServiceTests
{
    private readonly IGrabFrameService _service = new GrabFrameService();

    [Theory]
    [InlineData(100, 200, 10, 40, 500, 300, 1.0, 110, 240, 500, 300)]
    [InlineData(100, 200, 10, 40, 500, 300, 1.25, 113, 250, 625, 375)]
    [InlineData(100, 200, 10, 40, 500, 300, 1.5, 115, 260, 750, 450)]
    [InlineData(100, 200, 10, 40, 500, 300, 2.0, 120, 280, 1000, 600)]
    public void CalculateViewportScreenRect_ScalesAccurately(
        int winX, int winY,
        double localX, double localY, double localW, double localH,
        double dpi,
        int expectedX, int expectedY, int expectedW, int expectedH)
    {
        var bounds = _service.CalculateViewportScreenRect(winX, winY, localX, localY, localW, localH, dpi);

        Assert.Equal(expectedX, bounds.X);
        Assert.Equal(expectedY, bounds.Y);
        Assert.Equal(expectedW, bounds.Width);
        Assert.Equal(expectedH, bounds.Height);
    }

    [Fact]
    public void CalculateViewportScreenRect_ClampsToVirtualScreen()
    {
        var vs = new ScreenBounds(0, 0, 1920, 1080);
        var bounds = _service.CalculateViewportScreenRect(1800, 1000, 0, 0, 200, 200, 1.0, vs);

        Assert.Equal(1800, bounds.X);
        Assert.Equal(1000, bounds.Y);
        Assert.Equal(120, bounds.Width);
        Assert.Equal(80, bounds.Height);
    }

    [Fact]
    public void ParseTable_ExtractsTabSeparatedColumns()
    {
        double viewportWidth = 600;
        var dividers = new List<double> { 200, 400 };

        var words = new List<OcrWord>
        {
            new("Code", new Rect(50, 20, 30, 16)),
            new("Description", new Rect(250, 20, 70, 16)),
            new("Price", new Rect(450, 20, 40, 16)),

            new("A1", new Rect(50, 60, 20, 16)),
            new("Blue", new Rect(230, 60, 30, 16)),
            new("Widget", new Rect(270, 60, 45, 16)),
            new("$10.00", new Rect(450, 60, 50, 16)),

            new("B2", new Rect(50, 100, 20, 16)),
            new("Red", new Rect(230, 100, 25, 16)),
            new("Gear", new Rect(265, 100, 35, 16)),
            new("$25.50", new Rect(450, 100, 50, 16))
        };

        var result = _service.ParseTable(words, dividers, viewportWidth);

        Assert.NotNull(result);
        Assert.Equal(3, result.ColumnCount);
        Assert.Equal(3, result.RowCount);
        Assert.Equal(3, result.Rows.Count);

        // Header Row
        Assert.Equal("Code", result.Rows[0][0]);
        Assert.Equal("Description", result.Rows[0][1]);
        Assert.Equal("Price", result.Rows[0][2]);

        // Row 1
        Assert.Equal("A1", result.Rows[1][0]);
        Assert.Equal("Blue Widget", result.Rows[1][1]);
        Assert.Equal("$10.00", result.Rows[1][2]);

        // Formatted Text
        var lines = result.FormattedText.Split("\r\n");
        Assert.Equal(3, lines.Length);
        Assert.Equal("Code\tDescription\tPrice", lines[0]);
        Assert.Equal("A1\tBlue Widget\t$10.00", lines[1]);
        Assert.Equal("B2\tRed Gear\t$25.50", lines[2]);
    }

    [Fact]
    public void ParseTable_WithNoDividers_TreatsAsSingleColumn()
    {
        double viewportWidth = 400;
        var words = new List<OcrWord>
        {
            new("Line", new Rect(20, 20, 30, 16)),
            new("One", new Rect(60, 20, 30, 16)),
            new("Line", new Rect(20, 60, 30, 16)),
            new("Two", new Rect(60, 60, 30, 16))
        };

        var result = _service.ParseTable(words, Array.Empty<double>(), viewportWidth);

        Assert.Equal(1, result.ColumnCount);
        Assert.Equal(2, result.RowCount);
        Assert.Equal("Line One", result.Rows[0][0]);
        Assert.Equal("Line Two", result.Rows[1][0]);
    }

    [Fact]
    public void FilterWords_TagsMatchedWordsCorrectly()
    {
        var words = new List<OcrWord>
        {
            new("Search", new Rect(10, 10, 40, 16)),
            new("Target", new Rect(60, 10, 40, 16)),
            new("searching", new Rect(10, 40, 60, 16)),
            new("Other", new Rect(80, 40, 35, 16))
        };

        // Case-insensitive partial search
        var items = _service.FilterWords(words, "search", exactMatch: false);
        Assert.Equal(4, items.Count);
        Assert.True(items[0].IsMatched);  // "Search"
        Assert.False(items[1].IsMatched); // "Target"
        Assert.True(items[2].IsMatched);  // "searching"
        Assert.False(items[3].IsMatched); // "Other"

        // Exact match search
        var exactItems = _service.FilterWords(words, "Search", exactMatch: true);
        Assert.True(exactItems[0].IsMatched);  // "Search"
        Assert.False(exactItems[2].IsMatched); // "searching"
    }

    [Fact]
    public void FormatExtractedText_FormatsAccordingToMode()
    {
        string raw = "First Line\r\nSecond   Line\r\n\r\nThird Line";

        string standard = _service.FormatExtractedText(raw, GrabFrameMode.Standard);
        Assert.Equal("First Line\r\nSecond Line\r\n\r\nThird Line", standard);

        string singleLine = _service.FormatExtractedText(raw, GrabFrameMode.SingleLine);
        Assert.Equal("First Line Second Line Third Line", singleLine);
    }
}
