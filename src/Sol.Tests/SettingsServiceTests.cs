using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SolSettingsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "appsettings.json");
    }

    public void Dispose()
    {
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
    public void CorruptedJson_CreatesBackupAndKeepsDefaults()
    {
        File.WriteAllText(_settingsPath, "{ unparseable json [[[");

        var service = new SettingsService(_settingsPath);

        Assert.Equal("en", service.AppLanguage);
        Assert.True(service.IsMmcLookupEnabled);
        Assert.False(service.IsDemoMode);

        var backupFiles = Directory.GetFiles(_testDir, "appsettings.json.corrupt.*.bak");
        Assert.Single(backupFiles);
    }

    [Fact]
    public void PartialTypeMismatch_PreservesValidProperties()
    {
        var json = """
        {
            "AdDomain": "corp.contoso.com",
            "IsMmcLookupEnabled": "NotABoolean",
            "IsDemoMode": true,
            "AppLanguage": "de"
        }
        """;
        File.WriteAllText(_settingsPath, json);

        var service = new SettingsService(_settingsPath);

        Assert.Equal("corp.contoso.com", service.AdDomain);
        Assert.Equal("de", service.AppLanguage);
        Assert.True(service.IsDemoMode);
        // "IsMmcLookupEnabled" had invalid type, so it remains the default value (true)
        Assert.True(service.IsMmcLookupEnabled);
    }

    [Fact]
    public void SaveAndReload_PersistsAllPropertiesAndSchemaVersion()
    {
        var service = new SettingsService(_settingsPath)
        {
            AdDomain = "myad.local",
            IsDemoMode = true,
            IsMmcLookupEnabled = false,
            AppLanguage = "fr"
        };
        service.Save();

        var reloaded = new SettingsService(_settingsPath);

        Assert.Equal("myad.local", reloaded.AdDomain);
        Assert.True(reloaded.IsDemoMode);
        Assert.False(reloaded.IsMmcLookupEnabled);
        Assert.Equal("fr", reloaded.AppLanguage);
        Assert.Equal(1, reloaded.SchemaVersion);
    }

    [Fact]
    public void ConcurrentAccess_IsThreadSafe()
    {
        var service = new SettingsService(_settingsPath);

        Parallel.For(0, 100, i =>
        {
            if (i % 2 == 0)
            {
                service.AdDomain = $"domain_{i}.local";
                service.IsDemoMode = (i % 4 == 0);
                service.Save();
            }
            else
            {
                _ = service.AdDomain;
                _ = service.IsDemoMode;
                _ = service.IsMmcLookupEnabled;
            }
        });

        Assert.NotNull(service.AdDomain);
    }
}
