using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class CommandLineHelperTests
{
    [Fact]
    public void SplitCommandLine_EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Empty(CommandLineHelper.SplitCommandLine(null));
        Assert.Empty(CommandLineHelper.SplitCommandLine(""));
        Assert.Empty(CommandLineHelper.SplitCommandLine("   "));
    }

    [Fact]
    public void SplitCommandLine_StandardArguments_SplitsCorrectly()
    {
        var result = CommandLineHelper.SplitCommandLine("Sol.exe --unlock C:\\test.txt");
        Assert.Equal(3, result.Length);
        Assert.Equal("Sol.exe", result[0]);
        Assert.Equal("--unlock", result[1]);
        Assert.Equal("C:\\test.txt", result[2]);
    }

    [Fact]
    public void SplitCommandLine_QuotedArgumentsWithSpaces_PreservesQuotesContent()
    {
        var result = CommandLineHelper.SplitCommandLine("\"C:\\Program Files\\Sol\\Sol.exe\" --unlock \"D:\\My Documents\\file test.docx\"");
        Assert.Equal(3, result.Length);
        Assert.Equal("C:\\Program Files\\Sol\\Sol.exe", result[0]);
        Assert.Equal("--unlock", result[1]);
        Assert.Equal("D:\\My Documents\\file test.docx", result[2]);
    }

    [Fact]
    public void TryGetUnlockPath_FromArray_ReturnsCorrectPath()
    {
        string[] args = ["Sol.exe", "--unlock", "C:\\data\\sample.log"];
        var path = CommandLineHelper.TryGetUnlockPath(args);
        Assert.Equal("C:\\data\\sample.log", path);
    }

    [Fact]
    public void TryGetUnlockPath_FromString_ReturnsCorrectPath()
    {
        string cmdLine = "--unlock \"C:\\Windows\\System32\\notepad.exe\"";
        var path = CommandLineHelper.TryGetUnlockPath(cmdLine);
        Assert.Equal("C:\\Windows\\System32\\notepad.exe", path);
    }

    [Fact]
    public void TryGetUnlockPath_CaseInsensitive_MatchesFlag()
    {
        string[] args = ["--UnLoCk", "C:\\temp\\test.bin"];
        var path = CommandLineHelper.TryGetUnlockPath(args);
        Assert.Equal("C:\\temp\\test.bin", path);
    }

    [Fact]
    public void TryGetUnlockPath_MissingValue_ReturnsNull()
    {
        string[] args = ["Sol.exe", "--unlock"];
        var path = CommandLineHelper.TryGetUnlockPath(args);
        Assert.Null(path);
    }

    [Fact]
    public void TryGetUnlockPath_FlagAbsent_ReturnsNull()
    {
        string[] args = ["Sol.exe", "--some-other-flag", "value"];
        var path = CommandLineHelper.TryGetUnlockPath(args);
        Assert.Null(path);
    }

    [Fact]
    public void TryGetUnlockPath_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CommandLineHelper.TryGetUnlockPath((string[]?)null));
        Assert.Null(CommandLineHelper.TryGetUnlockPath((string?)null));
        Assert.Null(CommandLineHelper.TryGetUnlockPath(""));
    }
}
