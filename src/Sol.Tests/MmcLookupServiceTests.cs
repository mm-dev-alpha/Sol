using System;
using System.IO;
using System.Linq;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class MmcLookupServiceTests : IDisposable
{
    private readonly string _tempFavoritesPath;

    public MmcLookupServiceTests()
    {
        _tempFavoritesPath = Path.Combine(Path.GetTempPath(), $"mmc_fav_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFavoritesPath))
            {
                File.Delete(_tempFavoritesPath);
            }
        }
        catch { }
    }

    [Fact]
    public void GetAllTools_ReturnsCuratedCatalog_WithAtLeast40Tools()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var tools = service.GetAllTools();

        Assert.NotNull(tools);
        Assert.True(tools.Count >= 40, $"Expected >= 40 tools, but got {tools.Count}");
        Assert.Contains(tools, t => t.Command.Equals("dsa.msc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tools, t => t.Command.Equals("services.msc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tools, t => t.Command.Equals("compmgmt.msc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tools, t => t.Command.Equals("eventvwr.msc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tools, t => t.Command.Equals("ncpa.cpl", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("dsa", "Active Directory Users and Computers")]
    [InlineData("services", "Services Console")]
    [InlineData("eventvwr", "Event Viewer")]
    [InlineData("firewall", "Windows Defender Firewall")]
    [InlineData("disk", "Disk Management")]
    public void SearchTools_WithQuery_FindsMatchingTools(string query, string expectedName)
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var results = service.SearchTools(query);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Name.Contains(expectedName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchTools_WithCategory_FiltersCorrectly()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var adTools = service.SearchTools(null, MmcCategory.ActiveDirectory);

        Assert.NotEmpty(adTools);
        Assert.All(adTools, t => Assert.Equal(MmcCategory.ActiveDirectory, t.Category));
        Assert.Contains(adTools, t => t.Command.Equals("dsa.msc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToggleFavorite_TogglesAndPersistsStatus()
    {
        var service1 = new MmcLookupService(_tempFavoritesPath);
        Assert.False(service1.IsFavorite("dsa"));

        bool eventRaised = false;
        service1.FavoritesChanged += (s, e) => eventRaised = true;

        service1.ToggleFavorite("dsa");

        Assert.True(eventRaised);
        Assert.True(service1.IsFavorite("dsa"));

        // Verify favorite tool is pinned to top of GetAllTools
        var tools = service1.GetAllTools();
        Assert.Equal("dsa", tools[0].Id);
        Assert.True(tools[0].IsFavorite);

        // Verify persistence in new instance with same file path
        var service2 = new MmcLookupService(_tempFavoritesPath);
        Assert.True(service2.IsFavorite("dsa"));

        // Toggle back off
        service2.ToggleFavorite("dsa");
        Assert.False(service2.IsFavorite("dsa"));
    }

    [Fact]
    public void CopyRunCommand_ReturnsCorrectCommand()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var tool = new MmcToolItem
        {
            Id = "test",
            Name = "Test Tool",
            Command = "mmc.exe",
            Arguments = "test.msc"
        };

        string runCmd = service.CopyRunCommand(tool);
        Assert.Equal("mmc.exe test.msc", runCmd);
    }

    [Fact]
    public void SearchTools_EmptyQueryWithCategory_ReturnsAllToolsInCategory()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var secTools = service.SearchTools("   ", MmcCategory.Security);

        Assert.NotEmpty(secTools);
        Assert.All(secTools, t => Assert.Equal(MmcCategory.Security, t.Category));
    }

    [Fact]
    public void SearchTools_CategoryAll_ReturnsAllTools()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var allTools = service.GetAllTools();
        var searchAll = service.SearchTools(null, MmcCategory.All);

        Assert.Equal(allTools.Count, searchAll.Count);
    }

    [Fact]
    public void SearchTools_ByDescription_MatchesProperly()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        // "replication" is in description of Active Directory Sites and Services (dssite)
        var results = service.SearchTools("replication");

        Assert.NotEmpty(results);
        Assert.Contains(results, t => t.Id == "dssite");
    }

    [Fact]
    public void MultipleFavorites_ArePinnedInAlphabeticalOrderAtTop()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        service.ToggleFavorite("ncpa");      // Network Connections
        service.ToggleFavorite("cleanmgr");  // Disk Cleanup
        service.ToggleFavorite("dsa");       // Active Directory Users and Computers

        var tools = service.GetAllTools();
        var favoriteTools = tools.Where(t => t.IsFavorite).ToList();

        Assert.Equal(3, favoriteTools.Count);
        Assert.Equal("Active Directory Users and Computers", favoriteTools[0].Name);
        Assert.Equal("Disk Cleanup", favoriteTools[1].Name);
        Assert.Equal("Network Connections", favoriteTools[2].Name);
    }

    [Fact]
    public void CorruptedFavoritesJson_RecoversGracefullyWithoutCrashing()
    {
        File.WriteAllText(_tempFavoritesPath, "{ this is invalid json !!! corrupt");

        var service = new MmcLookupService(_tempFavoritesPath);
        var tools = service.GetAllTools();

        Assert.NotEmpty(tools);
        Assert.All(tools, t => Assert.False(t.IsFavorite));
    }

    [Fact]
    public void LaunchTool_InvalidOrEmptyCommand_ReturnsFalseWithError()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        var invalidTool = new MmcToolItem { Id = "empty", Name = "Empty", Command = "" };

        bool launched = service.LaunchTool(invalidTool, out string? error);
        Assert.False(launched);
        Assert.NotNull(error);
    }

    [Fact]
    public void RequestOpen_FiresOpenRequestedEvent()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        bool fired = false;
        service.OpenRequested += (s, e) => fired = true;

        service.RequestOpen();
        Assert.True(fired);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        var service = new MmcLookupService(_tempFavoritesPath);
        service.Dispose();
        service.Dispose(); // idempotent
        Assert.True(true);
    }
}
