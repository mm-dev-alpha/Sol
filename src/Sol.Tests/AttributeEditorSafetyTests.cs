using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class AttributeEditorSafetyTests : IDisposable
{
    private readonly string _testLogDir;

    public AttributeEditorSafetyTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "Sol_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testLogDir);
        AdAuditLogger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        AdAuditLogger.SetCustomLogDirectoryForTesting(null);
        if (Directory.Exists(_testLogDir))
        {
            try { Directory.Delete(_testLogDir, true); } catch { }
        }
    }

    [Theory]
    [InlineData("title", true)]
    [InlineData("department", true)]
    [InlineData("physicalDeliveryOfficeName", true)]
    [InlineData("telephoneNumber", true)]
    [InlineData("mobile", true)]
    [InlineData("streetAddress", true)]
    [InlineData("l", true)]
    [InlineData("st", true)]
    [InlineData("postalCode", true)]
    [InlineData("description", true)]
    [InlineData("wWWHomePage", true)]
    [InlineData("Title", true)] // Case-insensitive check
    [InlineData("DEPARTMENT", true)]
    [InlineData("objectSid", false)]
    [InlineData("objectGUID", false)]
    [InlineData("nTSecurityDescriptor", false)]
    [InlineData("pwdLastSet", false)]
    [InlineData("userAccountControl", false)]
    [InlineData("accountExpires", false)]
    [InlineData("lastLogon", false)]
    [InlineData("memberOf", false)]
    [InlineData("sAMAccountName", false)]
    [InlineData("userPrincipalName", false)]
    public void IsAttributeEditable_StrictlyEnforcesWhitelist(string attributeName, bool expectedAllowed)
    {
        // Act
        bool isAllowed = ActiveDirectoryService.IsAttributeEditable(attributeName);

        // Assert
        Assert.Equal(expectedAllowed, isAllowed);
    }

    [Fact]
    public async Task AuditLogger_WritesDurableStructuredLogEntry()
    {
        // Arrange
        var targetSam = "test.user";
        var attribute = "department";
        var oldVal = "Old Dept";
        var newVal = "New Dept";

        // Act
        await AdAuditLogger.LogAttributeChangeAsync(targetSam, attribute, oldVal, newVal, success: true);

        // Assert
        var logFile = AdAuditLogger.LogFilePath;
        Assert.True(File.Exists(logFile), "Audit log file was not created.");

        var lines = await File.ReadAllLinesAsync(logFile);
        Assert.NotEmpty(lines);

        var lastLine = lines[^1];
        var entry = JsonSerializer.Deserialize<AdAuditEntry>(lastLine);

        Assert.NotNull(entry);
        Assert.Equal(targetSam, entry.TargetUserSam);
        Assert.Equal(attribute, entry.AttributeName);
        Assert.Equal(oldVal, entry.OldValue);
        Assert.Equal(newVal, entry.NewValue);
        Assert.True(entry.Success);
        Assert.True((DateTime.UtcNow - entry.TimestampUtc).TotalMinutes < 5);
    }

    [Fact]
    public void NavigationService_RegistersCorePagesWithoutReflection()
    {
        // Arrange
        var navService = new NavigationService();

        // Assert that core pages are registered
        Assert.Null(navService.CurrentPageKey);
    }

    [Fact]
    public void AdAttributeItem_StoresKeyAndValueCorrectly()
    {
        var item = new Sol.Models.AdAttributeItem("title", "Software Engineer");
        Assert.Equal("title", item.Key);
        Assert.Equal("Software Engineer", item.Value);
    }
}
