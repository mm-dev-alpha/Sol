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

    [Fact]
    public void TryGetUnlockPath_FollowedByAnotherFlag_ReturnsNull()
    {
        string[] args1 = ["Sol.exe", "--unlock", "--silent"];
        Assert.Null(CommandLineHelper.TryGetUnlockPath(args1));

        string[] args2 = ["Sol.exe", "--unlock", "-f"];
        Assert.Null(CommandLineHelper.TryGetUnlockPath(args2));
    }

    [Fact]
    public void TryGetUnlockPath_FileStartingWithHyphen_IsAccepted()
    {
        string[] args = ["Sol.exe", "--unlock", "-private-data.txt"];
        var path = CommandLineHelper.TryGetUnlockPath(args);
        Assert.Equal("-private-data.txt", path);
    }

    [Theory]
    [InlineData("PC-01")]
    [InlineData("PC-01.corp.contoso.com")]
    [InlineData("192.168.1.1")]
    [InlineData("web-server")]
    [InlineData("localhost")]
    [InlineData("DC1")]
    public void IsValidHostNameOrAddress_ValidTargets_ReturnsTrue(string host)
    {
        Assert.True(CommandLineHelper.IsValidHostNameOrAddress(host));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PC-01 & calc.exe")]
    [InlineData("PC-01; dir")]
    [InlineData("PC-01 | powershell")]
    [InlineData("-s")]
    [InlineData("--silent")]
    [InlineData("192.168.1.1/24")]
    [InlineData("host name with spaces")]
    [InlineData("host`whoami`")]
    [InlineData("host$(whoami)")]
    [InlineData("host'")]
    [InlineData("host\"")]
    public void IsValidHostNameOrAddress_InjectionOrInvalidTargets_ReturnsFalse(string? host)
    {
        Assert.False(CommandLineHelper.IsValidHostNameOrAddress(host));
    }
}
