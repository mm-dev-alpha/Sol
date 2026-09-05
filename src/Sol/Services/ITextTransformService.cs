using System;
using System.Collections.Generic;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service providing text transformation, cleaning, formatting, and OCR error correction.
/// </summary>
public interface ITextTransformService
{
    #region Case Conversions
    string ToUpperCase(string text);
    string ToLowerCase(string text);
    string ToTitleCase(string text);
    string ToCamelCase(string text);
    CurrentCase DetermineCase(string text);
    string ToggleCase(string text);
    #endregion

    #region Line Operations
    string MakeSingleLine(string text);
    string RemoveDuplicateLines(string text);
    string TrimEachLine(string text);
    string RemoveEmptyLines(string text);
    string ShuffleLines(string text, Random? random = null);
    string SortLines(string text, bool descending = false);
    string AddToEachLine(string text, string textToAdd, SpotInLine spot);
    string RemoveFromEachLine(string text, int numberOfChars, SpotInLine spot);
    string LimitCharactersPerLine(string text, int characterLimit, SpotInLine spot);
    string JoinLines(string text, string joiner, bool trimFirst);
    #endregion

    #region OCR Error Corrections
    string TryFixNumberLetterErrors(string text);
    string TryFixEveryWordLetterNumberErrors(string text);
    string TryFixToLetters(string text);
    string TryFixToNumbers(string text);
    string ReplaceGreekOrCyrillicWithLatin(string text);
    string CorrectCommonGuidErrors(string text);
    string ReplaceReservedCharacters(string text);
    #endregion

    #region Pattern Extraction & Search
    string ExtractPattern(string text, string regexPattern);
    IReadOnlyList<string> ExtractAllMatches(string text, string regexPattern);
    int CountMatches(string text, string searchString);
    int CountRegexMatches(string text, string regexPattern);
    string ExtractEmails(string text);
    string ExtractUrls(string text);
    string ExtractNumbers(string text);
    #endregion

    #region Structural Formatting
    string UnstackToColumns(string text, int numberOfColumns);
    string UnstackGroups(string text, int numberOfRows);
    #endregion

    #region Validation & Characters
    bool IsValidEmail(string text);
    string GetUnicodeCategory(char c);
    #endregion
}
