using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Sol.Tests;

public enum SpikeTextFormat
{
    PlainText,
    DelimitedText,
    Csv,
    Tsv,
    Xml
}

public sealed record SpikeWrappedCell(int RowIndex, int ColumnIndex);

public sealed class SpikeTableDocument
{
    public const int DefaultMinimumRowCount = 5;
    public const int DefaultMinimumColumnCount = 3;

    public SpikeTextFormat Format { get; set; } = SpikeTextFormat.PlainText;
    public string NewLineSequence { get; set; } = Environment.NewLine;
    public string Delimiter { get; set; } = "\t";
    public List<string> ColumnNames { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public int MinimumRowCount { get; set; } = DefaultMinimumRowCount;
    public int MinimumColumnCount { get; set; } = DefaultMinimumColumnCount;
    public List<double?> ColumnWidths { get; set; } = [];
    public List<double?> RowHeights { get; set; } = [];
    public List<SpikeWrappedCell> WrappedCells { get; set; } = [];

    public static SpikeTableDocument CreateFromText(
        string? text,
        int minimumRowCount = DefaultMinimumRowCount,
        int minimumColumnCount = DefaultMinimumColumnCount)
    {
        string safeText = text ?? string.Empty;
        string newlineSequence = safeText.Contains("\r\n") ? "\r\n" : "\n";

        SpikeTableDocument doc;
        if (safeText.Contains('\t'))
        {
            doc = CreateDelimitedDocument(safeText, '\t', SpikeTextFormat.Tsv, newlineSequence, minimumRowCount, minimumColumnCount);
        }
        else if (safeText.Contains(',') && LooksLikeCsv(safeText))
        {
            doc = CreateDelimitedDocument(safeText, ',', SpikeTextFormat.Csv, newlineSequence, minimumRowCount, minimumColumnCount);
        }
        else if (safeText.TrimStart().StartsWith('<'))
        {
            doc = TryCreateXmlDocument(safeText, newlineSequence, minimumRowCount, minimumColumnCount) 
                  ?? CreatePlainTextDocument(safeText, newlineSequence, minimumRowCount, minimumColumnCount);
        }
        else
        {
            doc = CreatePlainTextDocument(safeText, newlineSequence, minimumRowCount, minimumColumnCount);
        }

        doc.EnsureMinimumSize();
        return doc;
    }

    private static bool LooksLikeCsv(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 && lines.All(l => l.Contains(','));
    }

    private static SpikeTableDocument CreateDelimitedDocument(
        string text,
        char delimiter,
        SpikeTextFormat format,
        string newlineSequence,
        int minimumRowCount,
        int minimumColumnCount)
    {
        List<List<string>> rows = ParseDelimited(text, delimiter);
        int maxCols = rows.Count == 0 ? 0 : rows.Max(r => r.Count);

        var doc = new SpikeTableDocument
        {
            Format = format,
            Delimiter = delimiter.ToString(),
            NewLineSequence = newlineSequence,
            ColumnNames = Enumerable.Range(0, maxCols).Select(i => GetDefaultColumnName(i)).ToList(),
            Rows = rows,
            RowCount = rows.Count,
            ColumnCount = maxCols,
            MinimumRowCount = minimumRowCount,
            MinimumColumnCount = minimumColumnCount
        };

        return doc;
    }

    private static SpikeTableDocument? TryCreateXmlDocument(
        string text,
        string newlineSequence,
        int minimumRowCount,
        int minimumColumnCount)
    {
        try
        {
            var xDoc = XDocument.Parse(text);
            var root = xDoc.Root;
            if (root == null) return null;

            var items = root.Elements().ToList();
            if (items.Count == 0) return null;

            var colNames = new List<string>();
            foreach (var item in items)
            {
                foreach (var attr in item.Attributes())
                {
                    string col = "@" + attr.Name.LocalName;
                    if (!colNames.Contains(col)) colNames.Add(col);
                }
                foreach (var child in item.Elements())
                {
                    string col = child.Name.LocalName;
                    if (!colNames.Contains(col)) colNames.Add(col);
                }
            }

            var rows = new List<List<string>>();
            foreach (var item in items)
            {
                var row = new List<string>();
                foreach (var col in colNames)
                {
                    if (col.StartsWith('@'))
                    {
                        var attr = item.Attribute(col[1..]);
                        row.Add(attr?.Value ?? string.Empty);
                    }
                    else
                    {
                        var child = item.Element(col);
                        row.Add(child?.Value ?? string.Empty);
                    }
                }
                rows.Add(row);
            }

            return new SpikeTableDocument
            {
                Format = SpikeTextFormat.Xml,
                NewLineSequence = newlineSequence,
                ColumnNames = colNames,
                Rows = rows,
                RowCount = rows.Count,
                ColumnCount = colNames.Count,
                MinimumRowCount = minimumRowCount,
                MinimumColumnCount = minimumColumnCount
            };
        }
        catch
        {
            return null;
        }
    }

    private static SpikeTableDocument CreatePlainTextDocument(
        string text,
        string newlineSequence,
        int minimumRowCount,
        int minimumColumnCount)
    {
        var rawLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        var rows = rawLines.Select(line => new List<string> { line }).ToList();

        return new SpikeTableDocument
        {
            Format = SpikeTextFormat.PlainText,
            NewLineSequence = newlineSequence,
            ColumnNames = [GetDefaultColumnName(0)],
            Rows = rows,
            RowCount = rows.Count,
            ColumnCount = 1,
            MinimumRowCount = minimumRowCount,
            MinimumColumnCount = minimumColumnCount
        };
    }

    public static List<List<string>> ParseDelimited(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var cells = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            cells.Add(sb.ToString());
            rows.Add(cells);
        }
        return rows;
    }

    public static string GetDefaultColumnName(int index)
    {
        int dividend = index + 1;
        string columnName = string.Empty;
        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }

    public void EnsureMinimumSize()
    {
        int requiredRows = Math.Max(RowCount, MinimumRowCount);
        int requiredCols = Math.Max(ColumnCount, MinimumColumnCount);

        while (ColumnNames.Count < requiredCols)
            ColumnNames.Add(GetDefaultColumnName(ColumnNames.Count));

        while (ColumnWidths.Count < requiredCols)
            ColumnWidths.Add(null);

        while (RowHeights.Count < requiredRows)
            RowHeights.Add(null);

        foreach (var row in Rows)
        {
            while (row.Count < requiredCols)
                row.Add(string.Empty);
        }

        while (Rows.Count < requiredRows)
        {
            Rows.Add(Enumerable.Repeat(string.Empty, requiredCols).ToList());
        }
    }

    public void InsertRow(int rowIndex)
    {
        EnsureMinimumSize();
        int insertIndex = Math.Clamp(rowIndex, 0, RowCount);
        Rows.Insert(insertIndex, Enumerable.Repeat(string.Empty, ColumnNames.Count).ToList());
        RowHeights.Insert(insertIndex, null);
        RowCount++;
        MinimumRowCount = Math.Max(MinimumRowCount, RowCount);
    }

    public void InsertColumn(int columnIndex, string? columnName = null)
    {
        EnsureMinimumSize();
        int insertIndex = Math.Clamp(columnIndex, 0, ColumnCount);
        ColumnNames.Insert(insertIndex, columnName ?? GetDefaultColumnName(ColumnNames.Count));
        ColumnWidths.Insert(insertIndex, null);
        foreach (var row in Rows)
            row.Insert(insertIndex, string.Empty);
        ColumnCount++;
        MinimumColumnCount = Math.Max(MinimumColumnCount, ColumnCount);
    }

    public void DeleteRow(int rowIndex)
    {
        EnsureMinimumSize();
        if (rowIndex < 0 || rowIndex >= RowCount) return;
        Rows.RemoveAt(rowIndex);
        if (rowIndex < RowHeights.Count) RowHeights.RemoveAt(rowIndex);
        RowCount = Math.Max(1, RowCount - 1);
    }

    public void DeleteColumn(int columnIndex)
    {
        EnsureMinimumSize();
        if (columnIndex < 0 || columnIndex >= ColumnCount) return;
        ColumnNames.RemoveAt(columnIndex);
        if (columnIndex < ColumnWidths.Count) ColumnWidths.RemoveAt(columnIndex);
        foreach (var row in Rows)
        {
            if (columnIndex < row.Count)
                row.RemoveAt(columnIndex);
        }
        ColumnCount = Math.Max(1, ColumnCount - 1);
    }

    public void MoveRow(int fromIndex, int toIndex)
    {
        EnsureMinimumSize();
        if (fromIndex < 0 || fromIndex >= RowCount || toIndex < 0 || toIndex >= RowCount || fromIndex == toIndex) return;
        var row = Rows[fromIndex];
        Rows.RemoveAt(fromIndex);
        Rows.Insert(toIndex, row);
        var height = RowHeights[fromIndex];
        RowHeights.RemoveAt(fromIndex);
        RowHeights.Insert(toIndex, height);
    }

    public void MoveColumn(int fromIndex, int toIndex)
    {
        EnsureMinimumSize();
        if (fromIndex < 0 || fromIndex >= ColumnCount || toIndex < 0 || toIndex >= ColumnCount || fromIndex == toIndex) return;
        var colName = ColumnNames[fromIndex];
        ColumnNames.RemoveAt(fromIndex);
        ColumnNames.Insert(toIndex, colName);
        var width = ColumnWidths[fromIndex];
        ColumnWidths.RemoveAt(fromIndex);
        ColumnWidths.Insert(toIndex, width);
        foreach (var row in Rows)
        {
            string val = row[fromIndex];
            row.RemoveAt(fromIndex);
            row.Insert(toIndex, val);
        }
    }

    public void Transpose()
    {
        EnsureMinimumSize();
        int sourceRowCount = Math.Max(1, RowCount);
        int sourceColumnCount = Math.Max(1, ColumnCount);

        var transposedRows = new List<List<string>>();
        for (int c = 0; c < sourceColumnCount; c++)
        {
            var newRow = new List<string>();
            for (int r = 0; r < sourceRowCount; r++)
            {
                newRow.Add(r < Rows.Count && c < Rows[r].Count ? Rows[r][c] : string.Empty);
            }
            transposedRows.Add(newRow);
        }

        Rows = transposedRows;
        RowCount = sourceColumnCount;
        ColumnCount = sourceRowCount;
        ColumnNames = Enumerable.Range(0, ColumnCount).Select(i => GetDefaultColumnName(i)).ToList();
        ColumnWidths.Clear();
        RowHeights.Clear();
        EnsureMinimumSize();
    }

    public string SerializeToText()
    {
        EnsureMinimumSize();
        if (Format == SpikeTextFormat.Tsv) return SerializeDelimited('\t');
        if (Format == SpikeTextFormat.Csv) return SerializeDelimited(',');
        if (Format == SpikeTextFormat.Xml) return SerializeXml();

        if (ColumnCount <= 1)
        {
            return string.Join(NewLineSequence, Rows.Take(RowCount).Select(r => r.FirstOrDefault() ?? string.Empty));
        }
        return SerializeDelimited('\t');
    }

    public string SerializeDelimited(char delimiter)
    {
        var sb = new StringBuilder();
        for (int r = 0; r < RowCount; r++)
        {
            if (r > 0) sb.Append(NewLineSequence);
            for (int c = 0; c < ColumnCount; c++)
            {
                if (c > 0) sb.Append(delimiter);
                string val = c < Rows[r].Count ? Rows[r][c] : string.Empty;
                if (val.Contains(delimiter) || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
                {
                    sb.Append('"').Append(val.Replace("\"", "\"\"")).Append('"');
                }
                else
                {
                    sb.Append(val);
                }
            }
        }
        return sb.ToString();
    }

    public string SerializeToMarkdown()
    {
        EnsureMinimumSize();
        var sb = new StringBuilder();
        sb.Append("| ");
        for (int c = 0; c < ColumnCount; c++)
        {
            sb.Append(c < ColumnNames.Count ? ColumnNames[c] : GetDefaultColumnName(c)).Append(" | ");
        }
        sb.Append(NewLineSequence);

        sb.Append("| ");
        for (int c = 0; c < ColumnCount; c++)
        {
            sb.Append("--- | ");
        }
        sb.Append(NewLineSequence);

        for (int r = 0; r < RowCount; r++)
        {
            sb.Append("| ");
            for (int c = 0; c < ColumnCount; c++)
            {
                string val = c < Rows[r].Count ? Rows[r][c].Replace("|", "\\|") : string.Empty;
                sb.Append(val).Append(" | ");
            }
            sb.Append(NewLineSequence);
        }

        return sb.ToString().TrimEnd();
    }

    private string SerializeXml()
    {
        var root = new XElement("items");
        for (int r = 0; r < RowCount; r++)
        {
            var item = new XElement("item");
            for (int c = 0; c < ColumnCount; c++)
            {
                string colName = c < ColumnNames.Count ? ColumnNames[c] : GetDefaultColumnName(c);
                string val = c < Rows[r].Count ? Rows[r][c] : string.Empty;
                if (colName.StartsWith('@'))
                {
                    item.SetAttributeValue(colName[1..], val);
                }
                else
                {
                    item.Add(new XElement(colName, val));
                }
            }
            root.Add(item);
        }
        return root.ToString();
    }
}

public sealed class SpikeLineCalculator
{
    private readonly Dictionary<string, double> _variables = new(StringComparer.OrdinalIgnoreCase);

    public SpikeCalculationResult Evaluate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SpikeCalculationResult([], 0, 0, 0);
        }

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var results = new List<string>();
        double sum = 0;
        int numericCount = 0;
        int errorCount = 0;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith('#'))
            {
                results.Add(string.Empty);
                continue;
            }

            try
            {
                // Check variable assignment: "x = 42" or "total = 10 + 20"
                var assignMatch = Regex.Match(trimmed, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$");
                if (assignMatch.Success)
                {
                    string varName = assignMatch.Groups[1].Value;
                    string expr = assignMatch.Groups[2].Value;
                    double val = EvaluateSimpleExpression(expr);
                    _variables[varName] = val;
                    results.Add(val.ToString(CultureInfo.InvariantCulture));
                    sum += val;
                    numericCount++;
                    continue;
                }

                double evaluated = EvaluateSimpleExpression(trimmed);
                results.Add(evaluated.ToString(CultureInfo.InvariantCulture));
                sum += evaluated;
                numericCount++;
            }
            catch
            {
                errorCount++;
                results.Add("Error");
            }
        }

        double average = numericCount > 0 ? sum / numericCount : 0;
        return new SpikeCalculationResult(results, sum, average, errorCount);
    }

    private double EvaluateSimpleExpression(string expr)
    {
        // Replace variables with their values
        string replaced = expr;
        foreach (var kvp in _variables.OrderByDescending(k => k.Key.Length))
        {
            replaced = Regex.Replace(replaced, @"\b" + Regex.Escape(kvp.Key) + @"\b", kvp.Value.ToString(CultureInfo.InvariantCulture));
        }

        // Evaluate using standard DataTable compute or basic token arithmetic
        using var dt = new System.Data.DataTable();
        var result = dt.Compute(replaced, "");
        return Convert.ToDouble(result, CultureInfo.InvariantCulture);
    }
}

public sealed record SpikeCalculationResult(
    IReadOnlyList<string> LineResults,
    double Sum,
    double Average,
    int ErrorCount);

public class EditTextSpikeTests
{
    [Fact]
    public void Tsv_RoundTrips_Cleanly()
    {
        const string input = "Name\tValue\r\nAlpha\t42";
        var doc = SpikeTableDocument.CreateFromText(input);

        Assert.Equal(SpikeTextFormat.Tsv, doc.Format);
        Assert.Equal(2, doc.RowCount);
        Assert.Equal(2, doc.ColumnCount);
        Assert.Equal(input, doc.SerializeToText());
    }

    [Fact]
    public void Csv_QuotedFields_RoundTrip()
    {
        const string input = "Name,Notes\r\nJoe,\"Hello, \"\"world\"\"\"";
        var doc = SpikeTableDocument.CreateFromText(input);

        Assert.Equal(SpikeTextFormat.Csv, doc.Format);
        Assert.Equal(2, doc.RowCount);
        Assert.Equal(2, doc.ColumnCount);
        Assert.Equal(input, doc.SerializeToText());
    }

    [Fact]
    public void Row_InsertDeleteMove_Operations()
    {
        var doc = SpikeTableDocument.CreateFromText("A\t1\r\nB\t2\r\nC\t3");
        doc.MoveRow(2, 0); // C\t3 at 0
        doc.DeleteRow(1);  // Delete A\t1 (now at index 1)

        Assert.Equal("C\t3\r\nB\t2", doc.SerializeToText());

        doc.InsertRow(1);
        doc.Rows[1][0] = "X";
        doc.Rows[1][1] = "9";
        Assert.Equal("C\t3\r\nX\t9\r\nB\t2", doc.SerializeToText());
    }

    [Fact]
    public void Column_InsertDeleteMove_Operations()
    {
        var doc = SpikeTableDocument.CreateFromText("A\tB\tC");
        doc.MoveColumn(2, 0); // C A B
        doc.DeleteColumn(1);  // C B

        Assert.Equal("C\tB", doc.SerializeToText());

        doc.InsertColumn(1, "Z");
        doc.Rows[0][1] = "new";
        Assert.Equal("C\tnew\tB", doc.SerializeToText());
    }

    [Fact]
    public void Transpose_SwapsRowsAndColumns()
    {
        var doc = SpikeTableDocument.CreateFromText("A\tB\tC\r\n1\t2\t3");
        doc.Transpose();

        Assert.Equal(3, doc.RowCount);
        Assert.Equal(2, doc.ColumnCount);
        Assert.Equal("A\t1\r\nB\t2\r\nC\t3", doc.SerializeToText());
    }

    [Fact]
    public void Markdown_Serialization_GeneratesTable()
    {
        var doc = SpikeTableDocument.CreateFromText("A\tB\r\n1\t2");
        string md = doc.SerializeToMarkdown();

        Assert.Contains("| A | B |", md);
        Assert.Contains("| --- | --- |", md);
        Assert.Contains("| 1 | 2 |", md);
    }

    [Fact]
    public void Calculation_LineByLine_VariablesAndExpressions()
    {
        var calc = new SpikeLineCalculator();
        string input = "x = 5\r\ny = 10\r\nx * y + 2\r\n// comment\r\n\r\nx + 100";

        var result = calc.Evaluate(input);

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(6, result.LineResults.Count);
        Assert.Equal("5", result.LineResults[0]);
        Assert.Equal("10", result.LineResults[1]);
        Assert.Equal("52", result.LineResults[2]);
        Assert.Equal("", result.LineResults[3]); // comment preserved
        Assert.Equal("", result.LineResults[4]); // empty line preserved
        Assert.Equal("105", result.LineResults[5]);
        Assert.Equal(172, result.Sum);
        Assert.Equal(43, result.Average);
    }
}
