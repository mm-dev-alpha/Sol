using System;
using System.IO;
using System.Linq;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class AdminCommandServiceTests : IDisposable
{
    private readonly string _tempFavoritesPath;

    public AdminCommandServiceTests()
    {
        _tempFavoritesPath = Path.Combine(Path.GetTempPath(), $"admin_cmd_fav_{Guid.NewGuid():N}.json");
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
    public void GetAllCommands_ReturnsCuratedCatalog_WithAtLeast50Commands()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var commands = service.GetAllCommands();

        Assert.NotNull(commands);
        Assert.True(commands.Count >= 50, $"Expected >= 50 commands, but got {commands.Count}");
        Assert.Contains(commands, c => c.Command.Contains("repadmin", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Command.Contains("ipconfig /flushdns", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Command.Contains("gpupdate /force", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Command.Contains("sfc /scannow", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Command.Contains("whoami /all", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Command.Contains("manage-bde", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("flushdns", "ipconfig /flushdns")]
    [InlineData("sfc", "sfc /scannow")]
    [InlineData("gpupdate", "gpupdate /force")]
    [InlineData("Get-ADUser", "Get-ADUser")]
    [InlineData("BitLocker", "manage-bde")]
    public void SearchCommands_WithQuery_FindsMatchingCommands(string query, string expectedSubstring)
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var results = service.SearchCommands(query);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Command.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase) ||
                                      r.Title.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchCommands_WithCategoryAndShell_FiltersCorrectly()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var psNetCommands = service.SearchCommands(null, AdminCommandCategory.Networking, AdminShellType.PowerShell);

        Assert.NotEmpty(psNetCommands);
        Assert.All(psNetCommands, c =>
        {
            Assert.Equal(AdminCommandCategory.Networking, c.Category);
            Assert.Equal(AdminShellType.PowerShell, c.ShellType);
        });
    }

    [Fact]
    public void ToggleFavorite_TogglesAndPersistsStatus()
    {
        var service1 = new AdminCommandService(_tempFavoritesPath);
        Assert.False(service1.IsFavorite("net-flush-dns"));

        bool eventRaised = false;
        service1.FavoritesChanged += (s, e) => eventRaised = true;

        service1.ToggleFavorite("net-flush-dns");

        Assert.True(eventRaised);
        Assert.True(service1.IsFavorite("net-flush-dns"));

        // Verify favorite command is pinned to top of GetAllCommands
        var commands = service1.GetAllCommands();
        Assert.Equal("net-flush-dns", commands[0].Id);
        Assert.True(commands[0].IsFavorite);

        // Verify persistence in new instance with same file path
        var service2 = new AdminCommandService(_tempFavoritesPath);
        Assert.True(service2.IsFavorite("net-flush-dns"));

        // Toggle back off
        service2.ToggleFavorite("net-flush-dns");
        Assert.False(service2.IsFavorite("net-flush-dns"));
    }

    [Fact]
    public void CopyCommand_ReturnsExactCommandString()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var item = new AdminCommandItem
        {
            Id = "test",
            Title = "Test Command",
            Command = "Get-Service | Where-Object Status -eq 'Stopped'"
        };

        string copied = service.CopyCommand(item);
        Assert.Equal("Get-Service | Where-Object Status -eq 'Stopped'", copied);
    }

    [Fact]
    public void CopyCommand_NullItem_ReturnsEmptyString()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        string copied = service.CopyCommand(null!);
        Assert.Equal(string.Empty, copied);
    }

    [Fact]
    public void SearchCommands_EmptyQueryWithFilters_ReturnsFilteredCommands()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var results = service.SearchCommands("   ", AdminCommandCategory.Security, AdminShellType.Cmd);

        Assert.NotEmpty(results);
        Assert.All(results, c =>
        {
            Assert.Equal(AdminCommandCategory.Security, c.Category);
            Assert.Equal(AdminShellType.Cmd, c.ShellType);
        });
    }

    [Fact]
    public void SearchCommands_AllFilters_ReturnsAllCommands()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        var all = service.GetAllCommands();
        var searchAll = service.SearchCommands(null, AdminCommandCategory.All, AdminShellType.All);

        Assert.Equal(all.Count, searchAll.Count);
    }

    [Fact]
    public void SearchCommands_ByDescription_MatchesProperly()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        // "Kerberos" is in description of sec-klist-purge
        var results = service.SearchCommands("Kerberos");

        Assert.NotEmpty(results);
        Assert.Contains(results, c => c.Id == "sec-klist-purge");
    }

    [Fact]
    public void MultipleFavorites_ArePinnedInAlphabeticalOrderAtTop()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        service.ToggleFavorite("net-flush-dns"); // Flush DNS Resolver Cache
        service.ToggleFavorite("sys-sfc-scan");  // System File Checker Integrity Scan
        service.ToggleFavorite("gp-force-update"); // Force Group Policy Update

        var commands = service.GetAllCommands();
        var favorites = commands.Where(c => c.IsFavorite).ToList();

        Assert.Equal(3, favorites.Count);
        Assert.Equal("Flush DNS Resolver Cache", favorites[0].Title);
        Assert.Equal("Force Group Policy Update", favorites[1].Title);
        Assert.Equal("System File Checker Integrity Scan", favorites[2].Title);
    }

    [Fact]
    public void CorruptedFavoritesJson_RecoversGracefullyWithoutCrashing()
    {
        File.WriteAllText(_tempFavoritesPath, "{ this is invalid json !!! corrupt");

        var service = new AdminCommandService(_tempFavoritesPath);
        var commands = service.GetAllCommands();

        Assert.NotEmpty(commands);
        Assert.All(commands, c => Assert.False(c.IsFavorite));
    }

    [Fact]
    public void RequestOpen_FiresOpenRequestedEvent()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        bool fired = false;
        service.OpenRequested += (s, e) => fired = true;

        service.RequestOpen();
        Assert.True(fired);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        var service = new AdminCommandService(_tempFavoritesPath);
        service.Dispose();
        service.Dispose(); // idempotent
        Assert.True(true);
    }
}
