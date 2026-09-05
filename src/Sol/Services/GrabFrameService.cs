using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Default implementation of <see cref="IGrabFrameService"/>.
/// </summary>
public sealed class GrabFrameService : IGrabFrameService
{
    private static readonly Regex MultiSpaceRegex = new(@"[^\S\r\n]+", RegexOptions.Compiled);
    private static readonly Regex SingleLineSpaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <inheritdoc/>
    public ScreenBounds CalculateViewportScreenRect(
        int windowScreenX,
        int windowScreenY,
        double localViewportX,
        double localViewportY,
        double localViewportWidth,
        double localViewportHeight,
        double dpiScale,
        ScreenBounds? virtualScreenBounds = null)
    {
        int physX = (int)Math.Round(windowScreenX + (localViewportX * dpiScale), MidpointRounding.AwayFromZero);
        int physY = (int)Math.Round(windowScreenY + (localViewportY * dpiScale), MidpointRounding.AwayFromZero);
        int physWidth = Math.Max(1, (int)Math.Round(localViewportWidth * dpiScale, MidpointRounding.AwayFromZero));
        int physHeight = Math.Max(1, (int)Math.Round(localViewportHeight * dpiScale, MidpointRounding.AwayFromZero));

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

    /// <inheritdoc/>
    public GrabFrameTableResult ParseTable(
        IReadOnlyList<OcrWord> words,
        IReadOnlyList<double> columnDividers,
        double viewportWidth)
    {
        if (words == null || words.Count == 0)
        {
            return new GrabFrameTableResult(0, 0, Array.Empty<IReadOnlyList<string>>(), string.Empty);
        }

        var sortedDividers = (columnDividers ?? Array.Empty<double>())
            .Where(d => d > 0 && d < viewportWidth)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        int columnCount = sortedDividers.Count + 1;

        // Group words into lines/rows based on vertical proximity
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

        // Sort rows by vertical position top-to-bottom
        rows = rows.OrderBy(r => r.Average(w => w.BoundingBox.Top)).ToList();

        var resultRows = new List<IReadOnlyList<string>>();
        var formattedLines = new List<string>();

        foreach (var row in rows)
        {
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

            resultRows.Add(cells);
            formattedLines.Add(string.Join("\t", cells));
        }

        string formattedText = string.Join("\r\n", formattedLines);
        return new GrabFrameTableResult(columnCount, resultRows.Count, resultRows, formattedText);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GrabFrameWordItem> FilterWords(
        IReadOnlyList<OcrWord> words,
        string searchQuery,
        bool exactMatch = false)
    {
        if (words == null || words.Count == 0)
        {
            return Array.Empty<GrabFrameWordItem>();
        }

        bool hasQuery = !string.IsNullOrWhiteSpace(searchQuery);
        string trimmedQuery = searchQuery?.Trim() ?? string.Empty;

        return words.Select(w =>
        {
            bool matched = false;
            if (hasQuery)
            {
                matched = exactMatch
                    ? string.Equals(w.Text, trimmedQuery, StringComparison.Ordinal)
                    : w.Text.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase);
            }

            return new GrabFrameWordItem(w.Text, w.BoundingBox)
            {
                IsMatched = matched
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public string FormatExtractedText(string rawText, GrabFrameMode mode)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        switch (mode)
        {
            case GrabFrameMode.SingleLine:
                // Strip all line breaks and collapse whitespace into single spaces
                return SingleLineSpaceRegex.Replace(rawText, " ").Trim();

            case GrabFrameMode.Standard:
            default:
                // Trim trailing space per line, normalize CRLF
                var lines = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                var cleanedLines = lines.Select(l => MultiSpaceRegex.Replace(l, " ").Trim());
                return string.Join("\r\n", cleanedLines).Trim();
        }
    }

    private const int HOTKEY_ID = 0x4746; // 'G', 'F'
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_G = 0x47;

    private IntPtr _registeredHwnd = IntPtr.Zero;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <inheritdoc/>
    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_G);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_G);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT | MOD_NOREPEAT, VK_G);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT, VK_G);
        }
        return registered;
    }

    /// <inheritdoc/>
    public bool UnregisterGlobalHotkey(IntPtr hWnd)
    {
        if (_registeredHwnd != IntPtr.Zero)
        {
            var res = UnregisterHotKey(_registeredHwnd, HOTKEY_ID);
            _registeredHwnd = IntPtr.Zero;
            return res;
        }
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        GC.SuppressFinalize(this);
    }
}
