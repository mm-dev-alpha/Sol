using System;
using System.IO;
using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class AppLogTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testLogPath;

    public AppLogTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SolAppLogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _testLogPath = Path.Combine(_testDir, "test_diagnostic.log");
        AppLog.SetLogPathForTesting(_testLogPath);
    }

    public void Dispose()
    {
        AppLog.SetLogPathForTesting(null);
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public void Write_CreatesLogFileAndAppendsMessage()
    {
        // Act
        AppLog.Write("Sample diagnostic message");

        // Assert
        Assert.True(File.Exists(_testLogPath));
        string content = File.ReadAllText(_testLogPath);
        Assert.Contains("Sample diagnostic message", content);
    }

    [Fact]
    public void Write_WhenFileExceedsMaxLogSizeBytes_RotatesToOldLog()
    {
        // Arrange: create a file that exceeds 5MB
        byte[] dummyData = new byte[AppLog.MaxLogSizeBytes + 1024];
        File.WriteAllBytes(_testLogPath, dummyData);
        Assert.True(new FileInfo(_testLogPath).Length > AppLog.MaxLogSizeBytes);

        // Act: write new message, which should trigger rotation
        AppLog.Write("Message after rotation");

        // Assert
        string oldLogPath = Path.Combine(_testDir, "test_diagnostic.old.log");
        Assert.True(File.Exists(oldLogPath), "Old log file should have been created on rotation.");
        Assert.True(new FileInfo(oldLogPath).Length >= AppLog.MaxLogSizeBytes, "Old log should contain the rotated data.");

        Assert.True(File.Exists(_testLogPath), "Active log file should exist.");
        string activeContent = File.ReadAllText(_testLogPath);
        Assert.Contains("Message after rotation", activeContent);
        Assert.True(new FileInfo(_testLogPath).Length < AppLog.MaxLogSizeBytes, "Active log should now be small.");
    }
}
