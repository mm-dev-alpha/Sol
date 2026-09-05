using System;
using System.Linq;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class ShortcutGuideServiceTests
{
    [Fact]
    public void GetCategories_ReturnsPredefinedCategories_WithShortcuts()
    {
        using var service = new ShortcutGuideService();
        var categories = service.GetCategories();

        Assert.NotEmpty(categories);
        Assert.True(categories.Count >= 7, "Should have at least 7 shortcut categories.");

        foreach (var category in categories)
        {
            Assert.False(string.IsNullOrWhiteSpace(category.Name));
            Assert.NotEmpty(category.Shortcuts);

            foreach (var shortcut in category.Shortcuts)
            {
                Assert.False(string.IsNullOrWhiteSpace(shortcut.PrimaryKeyCombination));
                Assert.NotEmpty(shortcut.Keys);
                Assert.False(string.IsNullOrWhiteSpace(shortcut.Description));
                Assert.Equal(category.Name, shortcut.Category);
            }
        }
    }

    [Fact]
    public void GetAllShortcuts_ReturnsFlattenedListWithoutEmptyEntries()
    {
        using var service = new ShortcutGuideService();
        var all = service.GetAllShortcuts();

        Assert.NotEmpty(all);
        Assert.True(all.Count >= 40, "Should have 40 or more shortcuts cataloged.");
        Assert.All(all, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.PrimaryKeyCombination));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
        });
    }

    [Fact]
    public void SearchShortcuts_FiltersByQueryCaseInsensitively()
    {
        using var service = new ShortcutGuideService();

        // Search for desktop shortcuts
        var desktopMatches = service.SearchShortcuts("desktop");
        Assert.NotEmpty(desktopMatches);
        Assert.Contains(desktopMatches, s => s.PrimaryKeyCombination.Contains("D", StringComparison.OrdinalIgnoreCase));

        // Search for clipboard
        var clipboardMatches = service.SearchShortcuts("clipboard");
        Assert.NotEmpty(clipboardMatches);
        Assert.Contains(clipboardMatches, s => s.PrimaryKeyCombination == "Win + V");

        // Search with empty query returns all
        var allMatches = service.SearchShortcuts(string.Empty);
        Assert.Equal(service.GetAllShortcuts().Count, allMatches.Count);
    }

    [Fact]
    public void ToggleOverlay_FiresOverlayToggleRequestedEvent()
    {
        using var service = new ShortcutGuideService();
        bool fired = false;

        service.OverlayToggleRequested += (s, e) => fired = true;
        service.ToggleOverlay();

        Assert.True(fired);
    }

    [Fact]
    public void NonWindowsShortcuts_AreCatalogedAndSearchable()
    {
        using var service = new ShortcutGuideService();
        var all = service.GetAllShortcuts();

        // Verify editing shortcuts
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Ctrl + C" && s.Category == Sol.Helpers.Strings.S.ShortcutCatGeneralEditing);
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Ctrl + Shift + Esc");
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Alt + Tab");

        // Verify explorer navigation shortcuts
        Assert.Contains(all, s => s.PrimaryKeyCombination == "F2" && s.Category == Sol.Helpers.Strings.S.ShortcutCatFileExplorerNav);
        Assert.Contains(all, s => s.PrimaryKeyCombination == "F5");
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Shift + Delete");

        // Verify browser navigation shortcuts
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Ctrl + T" && s.Category == Sol.Helpers.Strings.S.ShortcutCatAppNavigation);
        Assert.Contains(all, s => s.PrimaryKeyCombination == "Ctrl + Shift + T");

        // Verify search by non-Win key
        var ctrlMatches = service.SearchShortcuts("Ctrl +");
        Assert.True(ctrlMatches.Count >= 10);

        var taskManagerMatches = service.SearchShortcuts("Task Manager");
        Assert.NotEmpty(taskManagerMatches);
        Assert.Contains(taskManagerMatches, s => s.PrimaryKeyCombination == "Ctrl + Shift + Esc");
    }
}
