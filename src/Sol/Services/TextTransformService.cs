using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Implements text transformations, line manipulations, and OCR error correction.
/// </summary>
public partial class TextTransformService : ITextTransformService
{
    #region Static Maps & Dictionaries

    private static readonly Dictionary<char, char> GreekCyrillicLatinMap = new()
    {
        // Similar Looking Greek characters
        {'Γ', 'r'}, {'Δ', 'A'}, {'Θ', 'O'}, {'Λ', 'A'}, {'Ξ', 'E'},
        {'Π', 'n'}, {'Σ', 'E'}, {'Φ', 'O'}, {'Χ', 'X'}, {'Ψ', 'W'},
        {'Ω', 'O'}, {'α', 'a'}, {'β', 'B'}, {'γ', 'y'}, {'δ', 's'},
        {'ε', 'E'}, {'ζ', 'C'}, {'η', 'n'}, {'θ', 'O'}, {'ι', 'l'},
        {'κ', 'k'}, {'λ', 'A'}, {'μ', 'u'}, {'ν', 'v'}, {'ξ', 'E'},
        {'π', 'n'}, {'ρ', 'p'}, {'ς', 's'}, {'σ', 'o'}, {'τ', 't'},
        {'υ', 'v'}, {'φ', 'O'}, {'χ', 'X'}, {'ψ', 'U'}, {'ω', 'w'},
        {'ö', 'o'}, {'é', 'e'}, {'Å', 'A'}, {'Ö', 'O'}, {'ē', 'e'},
        {'ō', 'o'}, {'Ἀ', 'A'}, {'ό', 'o'},

        // Similar looking Cyrillic characters
        {'В', 'B'}, {'Б', 'B'}, {'Г', 'r'}, {'Д', 'A'}, {'Ё', 'E'}, {'Ж', 'K'},
        {'З', '3'}, {'И', 'N'}, {'Й', 'N'}, {'К', 'K'}, {'Л', 'n'},
        {'П', 'n'}, {'Ф', 'O'}, {'Ц', 'U'}, {'Ч', 'u'}, {'Ш', 'W'},
        {'Щ', 'W'}, {'Ъ', 'b'}, {'Ы', 'b'}, {'Ь', 'b'}, {'Э', '3'},
        {'Ю', 'O'}, {'Я', 'R'}, {'б', '6'}, {'в', 'B'}, {'г', 'r'},
        {'д', 'A'}, {'ё', 'e'}, {'ж', 'x'}, {'з', '3'}, {'и', 'N'},
        {'й', 'N'}, {'к', 'k'}, {'л', 'n'}, {'м', 'M'}, {'н', 'H'},
        {'п', 'n'}, {'т', 'T'}, {'ф', 'o'}, {'ц', 'u'}, {'ч', 'u'},
        {'ш', 'w'}, {'щ', 'w'}, {'ъ', 'b'}, {'ы', 'b'}, {'ь', 'b'},
        {'э', '3'}, {'ю', 'o'}, {'я', 'R'},

        // Other Characters
        {'ø', 'e'},
    };

    private static readonly Dictionary<char, char> NumbersToLetters = new()
    {
        {'0', 'o'}, {'4', 'h'}, {'9', 'g'}, {'1', 'l'}, {'8', 'B'},
        {'5', 'S'}, {'6', 'b'}, {'2', 'z' }
    };

    private static readonly Dictionary<char, char> LettersToNumbers = new()
    {
        {'o', '0'}, {'O', '0'}, {'Q', '0'}, {'c', '0'}, {'C', '0'},
        {'i', '1'}, {'I', '1'}, {'l', '1'}, {'g', '9'}, {'G', '9'},
        {'h', '4'}, {'H', '4'}, {'s', '5'}, {'S', '5'}, {'B', '8'},
        {'b', '6'}, {'z', '2'}, {'Z', '2'}
    };

    private static readonly Dictionary<char, char> GuidCorrections = new()
    {
        {'o', '0'}, {'O', '0'}, {'i', '1'}, {'l', '1'}, {'I', '1'},
        {'h', '4'}, {'z', '2'}, {'Z', '2'}, {'g', '9'}, {'G', '9'},
        {'s', '5'}, {'S', '5'}, {'Ø', '0'}, {'#', 'f'}, {'@', '0'},
        {'Q', '0'}, {'¥', 'f'}, {'£', 'f'}, {'/', '7'}
    };

    private static readonly char[] ReservedChars = [
        ' ', '"', '*', '/', ':', '<', '>', '?', '\\', '|', '+',
        ',', '.', ';', '=', '[', ']', '!', '@'
    ];

    #endregion

    #region Regex Generators

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpacesRegex();

    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex NewlineRegex();

    [GeneratedRegex(@"--+")]
    private static partial Regex MultiDashesRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex ExtractEmailsRegex();

    [GeneratedRegex(@"https?:\/\/[^\s/$.?#].[^\s]*")]
    private static partial Regex ExtractUrlsRegex();

    [GeneratedRegex(@"-?\b\d+(\.\d+)?\b")]
    private static partial Regex ExtractNumbersRegex();

    #endregion

    #region Case Conversions

    public string ToUpperCase(string text) => text.ToUpper();

    public string ToLowerCase(string text) => text.ToLower();

    public string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    public string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder sb = new();
        bool isNextUpper = true;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || c == '\n' || c == '\r')
            {
                isNextUpper = true;
                sb.Append(c);
            }
            else if (isNextUpper && char.IsLetter(c))
            {
                isNextUpper = false;
                sb.Append(char.ToUpper(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public CurrentCase DetermineCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return CurrentCase.Unknown;

        bool isAllLower = true;
        bool isAllUpper = true;
        int letterCount = 0;

        foreach (char c in text)
        {
            if (!char.IsLetter(c))
                continue;

            letterCount++;
            if (char.IsLower(c))
                isAllUpper = false;
            if (char.IsUpper(c))
                isAllLower = false;
        }

        if (letterCount == 0)
            return CurrentCase.Unknown;

        if (!isAllLower && isAllUpper)
            return CurrentCase.Upper;
        if (!isAllUpper && isAllLower)
            return CurrentCase.Lower;

        return CurrentCase.Camel;
    }

    public string ToggleCase(string text)
    {
        CurrentCase current = DetermineCase(text);
        return current switch
        {
            CurrentCase.Lower => ToCamelCase(text),
            CurrentCase.Camel => ToUpperCase(text),
            CurrentCase.Upper => ToLowerCase(text),
            _ => ToLowerCase(text)
        };
    }

    #endregion

    #region Line Operations

    public string MakeSingleLine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (!text.Contains('\n') && !text.Contains('\r'))
            return text;

        string normalized = NewlineRegex().Replace(text, " ");
        string cleaned = MultiSpacesRegex().Replace(normalized, " ").Trim();
        return cleaned;
    }

    public string RemoveDuplicateLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] splitLines = normalized.Split([Environment.NewLine], StringSplitOptions.None);
        List<string> uniqueLines = [];

        foreach (string line in splitLines)
        {
            string trimmed = line.Trim();
            if (!uniqueLines.Contains(trimmed))
                uniqueLines.Add(trimmed);
        }

        return string.Join(Environment.NewLine, uniqueLines);
    }

    public string TrimEachLine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(l => l.Trim()));
    }

    public string RemoveEmptyLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
    }

    public string ShuffleLines(string text, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);
        bool endsWithNewline = text.EndsWith("\n") || text.EndsWith("\r");

        if (endsWithNewline && lines.Length > 0 && string.IsNullOrEmpty(lines[^1]))
            lines = lines[..^1];

        if (lines.Length <= 1)
            return text;

        random ??= Random.Shared;

        for (int i = lines.Length - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (lines[i], lines[swapIndex]) = (lines[swapIndex], lines[i]);
        }

        string shuffled = string.Join(Environment.NewLine, lines);
        return endsWithNewline ? $"{shuffled}{Environment.NewLine}" : shuffled;
    }

    public string SortLines(string text, bool descending = false)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);

        IEnumerable<string> sorted = descending
            ? lines.OrderByDescending(l => l, StringComparer.CurrentCultureIgnoreCase)
            : lines.OrderBy(l => l, StringComparer.CurrentCultureIgnoreCase);

        return string.Join(Environment.NewLine, sorted);
    }

    public string AddToEachLine(string text, string textToAdd, SpotInLine spot)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);

        StringBuilder sb = new();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append(Environment.NewLine);

            if (spot == SpotInLine.Beginning)
                sb.Append(textToAdd).Append(lines[i]);
            else
                sb.Append(lines[i]).Append(textToAdd);
        }

        return sb.ToString();
    }

    public string RemoveFromEachLine(string text, int numberOfChars, SpotInLine spot)
    {
        if (string.IsNullOrEmpty(text) || numberOfChars <= 0)
            return text;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);

        StringBuilder sb = new();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append(Environment.NewLine);

            string line = lines[i];
            if (line.Length <= numberOfChars)
                continue;

            if (spot == SpotInLine.Beginning)
                sb.Append(line[numberOfChars..]);
            else
                sb.Append(line[..(line.Length - numberOfChars)]);
        }

        return sb.ToString();
    }

    public string LimitCharactersPerLine(string text, int characterLimit, SpotInLine spot)
    {
        if (string.IsNullOrEmpty(text) || characterLimit <= 0)
            return string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);

        StringBuilder sb = new();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append(Environment.NewLine);

            string line = lines[i];
            if (line.Length <= characterLimit)
            {
                sb.Append(line);
            }
            else
            {
                if (spot == SpotInLine.Beginning)
                    sb.Append(line[..characterLimit]);
                else
                    sb.Append(line[^characterLimit..]);
            }
        }

        return sb.ToString();
    }

    public string JoinLines(string text, string joiner, bool trimFirst)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        joiner ??= string.Empty;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.None);

        IEnumerable<string> processed = trimFirst ? lines.Select(l => l.Trim()) : lines;
        return string.Join(joiner, processed);
    }

    #endregion

    #region OCR Error Corrections

    public string TryFixNumberLetterErrors(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 5)
            return text;

        int totalNumbers = 0;
        int totalLetters = 0;

        foreach (char c in text)
        {
            if (char.IsNumber(c))
                totalNumbers++;
            else if (char.IsLetter(c))
                totalLetters++;
        }

        float fractionNumber = totalNumbers / (float)text.Length;
        float letterNumber = totalLetters / (float)text.Length;

        if (fractionNumber > 0.6f)
            return TryFixToNumbers(text);
        if (letterNumber > 0.6f)
            return TryFixToLetters(text);

        return text;
    }

    public string TryFixEveryWordLetterNumberErrors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string[] words = text.Split(' ');
        List<string> fixedWords = new(words.Length);

        foreach (string word in words)
            fixedWords.Add(TryFixNumberLetterErrors(word));

        string joined = string.Join(' ', fixedWords);
        joined = joined.Replace("\t ", "\t").Replace("\r ", "\r").Replace("\n ", "\n");
        return joined.Trim();
    }

    public string TryFixToLetters(string text) => ReplaceWithDictionary(text, NumbersToLetters);

    public string TryFixToNumbers(string text) => ReplaceWithDictionary(text, LettersToNumbers);

    public string ReplaceGreekOrCyrillicWithLatin(string text) => ReplaceWithDictionary(text, GreekCyrillicLatinMap);

    public string CorrectCommonGuidErrors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string cleaned = text.Replace(" ", "").Replace("-\r\n", "-").Replace("-\n", "-").Replace("\r\n-", "-").Replace("\n-", "-");
        return ReplaceWithDictionary(cleaned, GuidCorrections);
    }

    public string ReplaceReservedCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder sb = new(text);
        foreach (char reserved in ReservedChars)
            sb.Replace(reserved, '-');

        return MultiDashesRegex().Replace(sb.ToString(), "-");
    }

    private static string ReplaceWithDictionary(string input, Dictionary<char, char> map)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        StringBuilder sb = new(input.Length);
        foreach (char c in input)
            sb.Append(map.TryGetValue(c, out char replacement) ? replacement : c);

        return sb.ToString();
    }

    #endregion

    #region Pattern Extraction & Search

    public string ExtractPattern(string text, string regexPattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(regexPattern))
            return string.Empty;

        try
        {
            Match match = Regex.Match(text, regexPattern);
            return match.Success ? match.Value : string.Empty;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    public IReadOnlyList<string> ExtractAllMatches(string text, string regexPattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(regexPattern))
            return [];

        try
        {
            MatchCollection matches = Regex.Matches(text, regexPattern);
            List<string> results = new(matches.Count);
            foreach (Match m in matches)
            {
                if (m.Success)
                    results.Add(m.Value);
            }
            return results;
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    public int CountMatches(string text, string searchString)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchString))
            return 0;

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(searchString, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += searchString.Length;
        }
        return count;
    }

    public int CountRegexMatches(string text, string regexPattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(regexPattern))
            return 0;

        try
        {
            return Regex.Count(text, regexPattern);
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }

    public string ExtractEmails(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var matches = ExtractEmailsRegex().Matches(text);
        List<string> results = new(matches.Count);
        foreach (Match m in matches)
        {
            if (m.Success)
                results.Add(m.Value);
        }
        return string.Join(Environment.NewLine, results);
    }

    public string ExtractUrls(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var matches = ExtractUrlsRegex().Matches(text);
        List<string> results = new(matches.Count);
        foreach (Match m in matches)
        {
            if (m.Success)
                results.Add(m.Value);
        }
        return string.Join(Environment.NewLine, results);
    }

    public string ExtractNumbers(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var matches = ExtractNumbersRegex().Matches(text);
        List<string> results = new(matches.Count);
        foreach (Match m in matches)
        {
            if (m.Success)
                results.Add(m.Value);
        }
        return string.Join(Environment.NewLine, results);
    }

    #endregion

    #region Structural Formatting

    public string UnstackToColumns(string text, int numberOfColumns)
    {
        if (string.IsNullOrEmpty(text) || numberOfColumns <= 0)
            return text;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.TrimEntries);

        StringBuilder sb = new();
        int col = 0;

        foreach (string line in lines)
        {
            if (col == numberOfColumns)
            {
                sb.Append(Environment.NewLine).Append(line);
                col = 1;
            }
            else
            {
                if (col != 0)
                    sb.Append('\t');
                sb.Append(line);
                col++;
            }
        }

        return sb.ToString();
    }

    public string UnstackGroups(string text, int numberOfRows)
    {
        if (string.IsNullOrEmpty(text) || numberOfRows <= 0)
            return text;

        string normalized = NewlineRegex().Replace(text, Environment.NewLine);
        string[] lines = normalized.Split([Environment.NewLine], StringSplitOptions.TrimEntries);

        int numberOfColumns = (int)Math.Ceiling(lines.Length / (double)numberOfRows);
        StringBuilder sb = new();

        for (int r = 0; r < numberOfRows; r++)
        {
            if (r != 0)
                sb.Append(Environment.NewLine);

            for (int c = 0; c < numberOfColumns; c++)
            {
                int index = r + (c * numberOfRows);
                if (index >= lines.Length)
                    break;

                if (c != 0)
                    sb.Append('\t');
                sb.Append(lines[index]);
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Validation & Characters

    public bool IsValidEmail(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return EmailRegex().IsMatch(text.Trim());
    }

    public string GetUnicodeCategory(char c)
    {
        UnicodeCategory category = char.GetUnicodeCategory(c);
        return category switch
        {
            UnicodeCategory.UppercaseLetter => "Uppercase Letter",
            UnicodeCategory.LowercaseLetter => "Lowercase Letter",
            UnicodeCategory.TitlecaseLetter => "Titlecase Letter",
            UnicodeCategory.ModifierLetter => "Modifier Letter",
            UnicodeCategory.OtherLetter => "Other Letter",
            UnicodeCategory.NonSpacingMark => "Non-Spacing Mark",
            UnicodeCategory.SpacingCombiningMark => "Spacing Mark",
            UnicodeCategory.EnclosingMark => "Enclosing Mark",
            UnicodeCategory.DecimalDigitNumber => "Decimal Digit",
            UnicodeCategory.LetterNumber => "Letter Number",
            UnicodeCategory.OtherNumber => "Other Number",
            UnicodeCategory.SpaceSeparator => "Space Separator",
            UnicodeCategory.LineSeparator => "Line Separator",
            UnicodeCategory.ParagraphSeparator => "Paragraph Separator",
            UnicodeCategory.Control => "Control Character",
            UnicodeCategory.Format => "Format Character",
            UnicodeCategory.Surrogate => "Surrogate",
            UnicodeCategory.PrivateUse => "Private Use",
            UnicodeCategory.ConnectorPunctuation => "Connector Punctuation",
            UnicodeCategory.DashPunctuation => "Dash Punctuation",
            UnicodeCategory.OpenPunctuation => "Open Punctuation",
            UnicodeCategory.ClosePunctuation => "Close Punctuation",
            UnicodeCategory.InitialQuotePunctuation => "Initial Quote",
            UnicodeCategory.FinalQuotePunctuation => "Final Quote",
            UnicodeCategory.OtherPunctuation => "Other Punctuation",
            UnicodeCategory.MathSymbol => "Math Symbol",
            UnicodeCategory.CurrencySymbol => "Currency Symbol",
            UnicodeCategory.ModifierSymbol => "Modifier Symbol",
            UnicodeCategory.OtherSymbol => "Other Symbol",
            UnicodeCategory.OtherNotAssigned => "Not Assigned",
            _ => "Unknown"
        };
    }

    #endregion
}
