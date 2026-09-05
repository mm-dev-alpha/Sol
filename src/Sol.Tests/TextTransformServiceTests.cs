using System;
using System.Collections.Generic;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class TextTransformServiceTests
{
    private readonly ITextTransformService _service = new TextTransformService();

    #region Case Conversion Tests

    [Fact]
    public void ToUpperCase_ConvertsCorrectly()
    {
        Assert.Equal("HELLO WORLD", _service.ToUpperCase("hello world"));
    }

    [Fact]
    public void ToLowerCase_ConvertsCorrectly()
    {
        Assert.Equal("hello world", _service.ToLowerCase("HELLO WORLD"));
    }

    [Fact]
    public void ToTitleCase_ConvertsCorrectly()
    {
        Assert.Equal("Hello World", _service.ToTitleCase("hello world"));
        Assert.Equal("Hello World", _service.ToTitleCase("HELLO WORLD"));
    }

    [Fact]
    public void ToCamelCase_ConvertsCorrectly()
    {
        Assert.Equal("Hello World", _service.ToCamelCase("hello world"));
        Assert.Equal("Quick Brown Fox", _service.ToCamelCase("quick brown fox"));
    }

    [Fact]
    public void DetermineCase_IdentifiesCaseCorrectly()
    {
        Assert.Equal(CurrentCase.Lower, _service.DetermineCase("hello world"));
        Assert.Equal(CurrentCase.Upper, _service.DetermineCase("HELLO WORLD"));
        Assert.Equal(CurrentCase.Camel, _service.DetermineCase("HelloWorld"));
        Assert.Equal(CurrentCase.Unknown, _service.DetermineCase("12345!"));
    }

    [Fact]
    public void ToggleCase_CyclesCorrectly()
    {
        Assert.Equal("Hello World", _service.ToggleCase("hello world")); // Lower -> Camel
        Assert.Equal("HELLO WORLD", _service.ToggleCase("Hello World")); // Camel -> Upper
        Assert.Equal("hello world", _service.ToggleCase("HELLO WORLD")); // Upper -> Lower
    }

    #endregion

    #region Line Operations Tests

    [Fact]
    public void MakeSingleLine_RemovesNewlinesAndExtraSpaces()
    {
        string input = "Line 1\r\nLine 2\nLine 3\r  Line 4";
        string result = _service.MakeSingleLine(input);
        Assert.Equal("Line 1 Line 2 Line 3 Line 4", result);
    }

    [Fact]
    public void RemoveDuplicateLines_KeepsFirstOccurrences()
    {
        string input = "Apple\r\nBanana\r\nApple\r\nOrange\r\nBanana";
        string result = _service.RemoveDuplicateLines(input);
        string expected = $"Apple{Environment.NewLine}Banana{Environment.NewLine}Orange";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TrimEachLine_TrimsWhitespaceFromAllLines()
    {
        string input = "  Line 1  \r\n\tLine 2\t";
        string result = _service.TrimEachLine(input);
        string expected = $"Line 1{Environment.NewLine}Line 2";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveEmptyLines_FiltersOutEmptyAndWhitespaceLines()
    {
        string input = "Line 1\r\n\r\n   \r\nLine 2";
        string result = _service.RemoveEmptyLines(input);
        string expected = $"Line 1{Environment.NewLine}Line 2";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShuffleLines_PreservesAllLines()
    {
        string input = $"One{Environment.NewLine}Two{Environment.NewLine}Three{Environment.NewLine}Four";
        string result = _service.ShuffleLines(input, new Random(42));
        var originalLines = new HashSet<string>(input.Split(Environment.NewLine));
        var resultLines = new HashSet<string>(result.Split(Environment.NewLine));
        Assert.Equal(originalLines, resultLines);
    }

    [Fact]
    public void SortLines_AscendingAndDescending()
    {
        string input = $"Cherry{Environment.NewLine}Apple{Environment.NewLine}Banana";
        string asc = _service.SortLines(input, false);
        string desc = _service.SortLines(input, true);

        Assert.Equal($"Apple{Environment.NewLine}Banana{Environment.NewLine}Cherry", asc);
        Assert.Equal($"Cherry{Environment.NewLine}Banana{Environment.NewLine}Apple", desc);
    }

    [Fact]
    public void AddToEachLine_BeginningAndEnd()
    {
        string input = $"One{Environment.NewLine}Two";
        string beginning = _service.AddToEachLine(input, "> ", SpotInLine.Beginning);
        string end = _service.AddToEachLine(input, " <", SpotInLine.End);

        Assert.Equal($"> One{Environment.NewLine}> Two", beginning);
        Assert.Equal($"One <{Environment.NewLine}Two <", end);
    }

    [Fact]
    public void RemoveFromEachLine_BeginningAndEnd()
    {
        string input = $"123Alpha{Environment.NewLine}123Beta";
        string resultStart = _service.RemoveFromEachLine(input, 3, SpotInLine.Beginning);
        string resultEnd = _service.RemoveFromEachLine(input, 4, SpotInLine.End);

        Assert.Contains("Alpha", resultStart);
        Assert.Contains("Beta", resultStart);
        Assert.DoesNotContain("123", resultStart);

        Assert.Contains("123", resultEnd);
    }

    [Fact]
    public void LimitCharactersPerLine_TruncatesCorrectly()
    {
        string input = $"LongLineOne{Environment.NewLine}Short";
        string result = _service.LimitCharactersPerLine(input, 5, SpotInLine.Beginning);
        Assert.Equal($"LongL{Environment.NewLine}Short", result);
    }

    [Fact]
    public void JoinLines_JoinsWithCustomDelimiter()
    {
        string input = $"First{Environment.NewLine}Second{Environment.NewLine}Third";
        string result = _service.JoinLines(input, ", ", false);
        Assert.Equal("First, Second, Third", result);
    }

    #endregion

    #region OCR Error Correction Tests

    [Fact]
    public void TryFixNumberLetterErrors_FixesMostlyNumbersWord()
    {
        // "1234o67" has 'o' which should be '0'
        string input = "1234o67";
        string result = _service.TryFixNumberLetterErrors(input);
        Assert.Equal("1234067", result);
    }

    [Fact]
    public void TryFixNumberLetterErrors_FixesMostlyLettersWord()
    {
        // "he110" has '1' and '0' which should be 'l' and 'o'
        string input = "he110world";
        string result = _service.TryFixNumberLetterErrors(input);
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void TryFixEveryWordLetterNumberErrors_ProcessesAllWords()
    {
        string input = "Price 1234o67 Total he110world";
        string result = _service.TryFixEveryWordLetterNumberErrors(input);
        Assert.Contains("1234067", result);
        Assert.Contains("helloworld", result);
    }

    [Fact]
    public void ReplaceGreekOrCyrillicWithLatin_ReplacesLookalikes()
    {
        // Cyrillic 'В' -> Latin 'B', Cyrillic 'о' -> Latin 'o'
        string input = "Вo";
        string result = _service.ReplaceGreekOrCyrillicWithLatin(input);
        Assert.Equal("Bo", result);
    }

    [Fact]
    public void CorrectCommonGuidErrors_NormalizesGuid()
    {
        string input = "e029ab42-569d-4c32-9c80-448dcd52379o";
        string result = _service.CorrectCommonGuidErrors(input);
        Assert.EndsWith("0", result);
        Assert.DoesNotContain(" ", result);
    }

    [Fact]
    public void ReplaceReservedCharacters_ReplacesInvalidCharsWithDash()
    {
        string input = "file/name:with*invalid?chars";
        string result = _service.ReplaceReservedCharacters(input);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("?", result);
        Assert.Contains("-", result);
    }

    #endregion

    #region Pattern Extraction & Search Tests

    [Fact]
    public void ExtractPattern_FindsFirstMatch()
    {
        string input = "Order #12345 placed on 2026-09-04";
        string pattern = @"\d{4}-\d{2}-\d{2}";
        string match = _service.ExtractPattern(input, pattern);
        Assert.Equal("2026-09-04", match);
    }

    [Fact]
    public void ExtractAllMatches_FindsAllOccurrences()
    {
        string input = "10 items, 20 items, 30 items";
        var matches = _service.ExtractAllMatches(input, @"\d+");
        Assert.Equal(3, matches.Count);
        Assert.Equal("10", matches[0]);
        Assert.Equal("20", matches[1]);
        Assert.Equal("30", matches[2]);
    }

    [Fact]
    public void CountMatches_CountsExactOccurrences()
    {
        string input = "The quick brown fox jumps over the lazy dog";
        int count = _service.CountMatches(input, "the");
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountRegexMatches_CountsPatternOccurrences()
    {
        string input = "Item 1, Item 2, Item 3";
        int count = _service.CountRegexMatches(input, @"Item \d");
        Assert.Equal(3, count);
    }

    [Fact]
    public void ExtractEmails_FindsAllEmailAddresses()
    {
        string input = "Contact us at support@example.com or sales.dept@company.org for help.";
        string result = _service.ExtractEmails(input);
        Assert.Contains("support@example.com", result);
        Assert.Contains("sales.dept@company.org", result);
    }

    [Fact]
    public void ExtractUrls_FindsAllWebUrls()
    {
        string input = "Visit https://github.com/sol or http://example.org/test for docs.";
        string result = _service.ExtractUrls(input);
        Assert.Contains("https://github.com/sol", result);
        Assert.Contains("http://example.org/test", result);
    }

    [Fact]
    public void ExtractNumbers_FindsAllNumbers()
    {
        string input = "Order 42 has 3 items totaling 99.50 dollars";
        string result = _service.ExtractNumbers(input);
        Assert.Contains("42", result);
        Assert.Contains("3", result);
        Assert.Contains("99.50", result);
    }

    #endregion

    #region Structural Formatting Tests

    [Fact]
    public void UnstackToColumns_TransformsStackedLinesToTsv()
    {
        string input = $"A{Environment.NewLine}B{Environment.NewLine}C{Environment.NewLine}D";
        string result = _service.UnstackToColumns(input, 2);
        string expected = $"A\tB{Environment.NewLine}C\tD";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UnstackGroups_TransformsGroupsToRows()
    {
        string input = $"1{Environment.NewLine}2{Environment.NewLine}3{Environment.NewLine}4";
        string result = _service.UnstackGroups(input, 2);
        Assert.Contains("\t", result);
    }

    #endregion

    #region Validation & Character Tests

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("first.last@company.org", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidEmail_ValidatesCorrectly(string email, bool expected)
    {
        Assert.Equal(expected, _service.IsValidEmail(email));
    }

    [Fact]
    public void GetUnicodeCategory_ReturnsExpectedCategories()
    {
        Assert.Equal("Uppercase Letter", _service.GetUnicodeCategory('A'));
        Assert.Equal("Lowercase Letter", _service.GetUnicodeCategory('a'));
        Assert.Equal("Decimal Digit", _service.GetUnicodeCategory('5'));
        Assert.Equal("Space Separator", _service.GetUnicodeCategory(' '));
    }

    #endregion
}
