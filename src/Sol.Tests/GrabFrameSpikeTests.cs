using System;
using System.Collections.Generic;
using System.Linq;
using Sol.Models;
using Windows.Foundation;
using Xunit;

namespace Sol.Tests;

public class GrabFrameSpikeTests
{
    public static ScreenBounds CalculatePhysicalBounds(
        int windowScreenLeft,
        int windowScreenTop,
        double localX,
        double localY,
        double localWidth,
        double localHeight,
        double dpiScale,
        ScreenBounds? virtualScreenBounds = null)
    {
        int physX = (int)Math.Round(windowScreenLeft + (localX * dpiScale), MidpointRounding.AwayFromZero);
        int physY = (int)Math.Round(windowScreenTop + (localY * dpiScale), MidpointRounding.AwayFromZero);
        int physWidth = Math.Max(1, (int)Math.Round(localWidth * dpiScale, MidpointRounding.AwayFromZero));
        int physHeight = Math.Max(1, (int)Math.Round(localHeight * dpiScale, MidpointRounding.AwayFromZero));

        if (virtualScreenBounds != null)
        {
            var vs = virtualScreenBounds;
            int right = Math.Min(physX + physWidth, vs.X + vs.Width);
            int bottom = Math.Min(physY + physHeight, vs.Y + vs.Height);
            physX = Math.Max(physX, vs.X);
            physY = Math.Max(physY, vs.Y);
            physWidth = Math.Max(1, right - physX);
            physHeight = Math.Max(1, bottom - physY);
        }

        return new ScreenBounds(physX, physY, physWidth, physHeight);
    }

    public static string GenerateTableText(
        IReadOnlyList<OcrWord> words,
        IReadOnlyList<double> columnDividers,
        double viewportWidth)
    {
        if (words == null || words.Count == 0)
        {
            return string.Empty;
        }

        // Sort dividers ascending and add boundary at viewportWidth
        var sortedDividers = columnDividers.Where(d => d > 0 && d < viewportWidth).Distinct().OrderBy(d => d).ToList();
        int columnCount = sortedDividers.Count + 1;

        // Group words into lines/rows by vertical position
        // Words whose vertical center is within half the height of an existing row belong to that row
        var rows = new List<List<OcrWord>>();
        var sortedWords = words.OrderBy(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();

        foreach (var word in sortedWords)
        {
            double wordMidY = word.BoundingBox.Top + (word.BoundingBox.Height / 2.0);
            var matchingRow = rows.FirstOrDefault(r =>
            {
                double rowAvgY = r.Average(rw => rw.BoundingBox.Top + (rw.BoundingBox.Height / 2.0));
                double rowAvgHeight = r.Average(rw => rw.BoundingBox.Height);
                return Math.Abs(wordMidY - rowAvgY) <= (rowAvgHeight * 0.65);
            });

            if (matchingRow != null)
            {
                matchingRow.Add(word);
            }
            else
            {
                rows.Add(new List<OcrWord> { word });
            }
        }

        // Sort rows by their average Y position
        rows = rows.OrderBy(r => r.Average(w => w.BoundingBox.Top)).ToList();

        var outputLines = new List<string>();
        foreach (var row in rows)
        {
            // Split row into columns
            var cells = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                double leftBound = i == 0 ? 0 : sortedDividers[i - 1];
                double rightBound = i < sortedDividers.Count ? sortedDividers[i] : viewportWidth;

                var columnWords = row
                    .Where(w =>
                    {
                        double wordMidX = w.BoundingBox.Left + (w.BoundingBox.Width / 2.0);
                        return wordMidX >= leftBound && wordMidX < rightBound;
                    })
                    .OrderBy(w => w.BoundingBox.Left)
                    .Select(w => w.Text);

                cells[i] = string.Join(" ", columnWords).Trim();
            }

            outputLines.Add(string.Join("\t", cells));
        }

        return string.Join("\r\n", outputLines);
    }

    [Theory]
    [InlineData(100, 200, 10, 40, 500, 300, 1.0, 110, 240, 500, 300)]
    [InlineData(100, 200, 10, 40, 500, 300, 1.25, 113, 250, 625, 375)]
    [InlineData(100, 200, 10, 40, 500, 300, 1.5, 115, 260, 750, 450)]
    [InlineData(100, 200, 10, 40, 500, 300, 2.0, 120, 280, 1000, 600)]
    public void CalculatePhysicalBounds_ScalesCoordinatesAccurately(
        int winX, int winY,
        double localX, double localY, double localW, double localH,
        double dpi,
        int expectedX, int expectedY, int expectedW, int expectedH)
    {
        var bounds = CalculatePhysicalBounds(winX, winY, localX, localY, localW, localH, dpi);

        Assert.Equal(expectedX, bounds.X);
        Assert.Equal(expectedY, bounds.Y);
        Assert.Equal(expectedW, bounds.Width);
        Assert.Equal(expectedH, bounds.Height);
    }

    [Fact]
    public void CalculatePhysicalBounds_ClampsAgainstVirtualScreenBounds()
    {
        var virtualScreen = new ScreenBounds(0, 0, 1920, 1080);
        // Window extends beyond right and bottom edge
        var bounds = CalculatePhysicalBounds(1800, 1000, 0, 0, 200, 200, 1.0, virtualScreen);

        Assert.Equal(1800, bounds.X);
        Assert.Equal(1000, bounds.Y);
        Assert.Equal(120, bounds.Width); // Clamped from 200 to 1920 - 1800 = 120
        Assert.Equal(80, bounds.Height);  // Clamped from 200 to 1080 - 1000 = 80
    }

    [Fact]
    public void GenerateTableText_ExtractsMultiColumnRowsCorrectly()
    {
        double viewportWidth = 600;
        // Two dividers at X=200 and X=400 -> creates 3 columns: [0..200), [200..400), [400..600)
        var dividers = new List<double> { 200, 400 };

        var words = new List<OcrWord>
        {
            // Row 1 (Header)
            new("ID", new Rect(50, 20, 30, 16)),
            new("Name", new Rect(250, 20, 40, 16)),
            new("Status", new Rect(450, 20, 50, 16)),

            // Row 2 (Data 1)
            new("001", new Rect(50, 60, 30, 16)),
            new("Alpha", new Rect(250, 60, 45, 16)),
            new("Beta", new Rect(300, 60, 35, 16)), // Part of Column 2
            new("Active", new Rect(450, 60, 45, 16)),

            // Row 3 (Data 2)
            new("002", new Rect(50, 100, 30, 16)),
            new("Gamma", new Rect(250, 100, 50, 16)),
            new("Pending", new Rect(450, 100, 55, 16))
        };

        string tableResult = GenerateTableText(words, dividers, viewportWidth);

        var lines = tableResult.Split("\r\n");
        Assert.Equal(3, lines.Length);

        // Header check
        Assert.Equal("ID\tName\tStatus", lines[0]);

        // Row 1 check (Name column has two words merged with space)
        Assert.Equal("001\tAlpha Beta\tActive", lines[1]);

        // Row 2 check
        Assert.Equal("002\tGamma\tPending", lines[2]);
    }

    [Fact]
    public void GenerateTableText_HandlesEmptyCellsGracefully()
    {
        double viewportWidth = 400;
        // One divider at X=200 -> 2 columns: [0..200), [200..400)
        var dividers = new List<double> { 200 };

        var words = new List<OcrWord>
        {
            // Row 1 has both columns
            new("Key", new Rect(50, 20, 30, 16)),
            new("Value", new Rect(250, 20, 40, 16)),

            // Row 2 has only column 1
            new("SoloKey", new Rect(50, 60, 60, 16)),

            // Row 3 has only column 2
            new("SoloValue", new Rect(250, 100, 70, 16))
        };

        string tableResult = GenerateTableText(words, dividers, viewportWidth);
        var lines = tableResult.Split("\r\n");

        Assert.Equal(3, lines.Length);
        Assert.Equal("Key\tValue", lines[0]);
        Assert.Equal("SoloKey\t", lines[1]);
        Assert.Equal("\tSoloValue", lines[2]);
    }

    [Fact]
    public void SearchFiltering_MatchesWordsAccurately()
    {
        var words = new List<OcrWord>
        {
            new("System", new Rect(10, 10, 40, 16)),
            new("Information", new Rect(60, 10, 80, 16)),
            new("Operating", new Rect(10, 40, 60, 16)),
            new("system", new Rect(80, 40, 40, 16))
        };

        // Case-insensitive search for "system"
        string query = "system";
        var matchedIndices = words
            .Select((w, idx) => (w, idx))
            .Where(item => item.w.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.idx)
            .ToList();

        Assert.Equal(2, matchedIndices.Count);
        Assert.Contains(0, matchedIndices); // "System"
        Assert.Contains(3, matchedIndices); // "system"
    }
}
