using System;
using System.Reflection;
using System.Collections.Generic;
using Sol.Helpers;
using Sol.Services;
using Sol.ViewModels;
using Sol.Views;
using Xunit;

namespace Sol.Tests;

public class ToolsNavigationTests
{
    [Fact]
    public void NavigationService_RegistersToolsPage()
    {
        var navService = new NavigationService();

        // Use reflection to inspect the private registry dictionary
        var field = typeof(NavigationService).GetField("_pageRegistry", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);

        var registry = field.GetValue(navService) as Dictionary<string, Type>;
        Assert.NotNull(registry);

        Assert.True(registry.ContainsKey("ToolsPage"), "NavigationService should contain registration for 'ToolsPage'.");
        Assert.Equal(typeof(ToolsPage), registry["ToolsPage"]);
    }

    [Fact]
    public void ToolsViewModel_InitializesWithDefaultStatuses()
    {
        var vm = new ToolsViewModel();

        Assert.False(vm.IsAwakeActive);
        Assert.Equal(Strings.S.ToolStatusInactive, vm.AwakeStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.FileLocksmithStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.ShortcutGuideStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.MmcStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.AdminCommandsStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.GrabFrameStatusBadge);
        Assert.Equal(Strings.S.ToolStatusReady, vm.EditTextStatusBadge);
        Assert.Equal("Win + Shift + ?", vm.ShortcutGuideHotkeyText);
        Assert.Equal("Alt + Space", vm.MmcHotkeyText);
        Assert.Equal("Win + Shift + C", vm.AdminCommandsHotkeyText);
        Assert.Equal("Win + Shift + G", vm.GrabFrameHotkeyText);
        Assert.Equal("Win + Shift + E", vm.EditTextHotkeyText);
        Assert.False(vm.KeepScreenOn);
        Assert.Equal(0, vm.AwakeModeIndex);
        Assert.NotNull(vm.LaunchMmcLookupCommand);
        Assert.NotNull(vm.LaunchAdminCommandsCommand);
        Assert.NotNull(vm.LaunchGrabFrameCommand);
        Assert.NotNull(vm.LaunchEditTextWindowCommand);
    }

    [Fact]
    public void ToolsStrings_AreDefinedAndNonEmpty()
    {
        var s = Strings.S;

        Assert.False(string.IsNullOrWhiteSpace(s.NavTools));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolsPageTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolsPageSubtitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolAwakeTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolAwakeDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolFileLocksmithTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolFileLocksmithDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolShortcutGuideTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolShortcutGuideDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolMmcTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolMmcDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolAdminCommandsTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolAdminCommandsDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolGrabFrameTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolGrabFrameDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolEditTextTitle));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolEditTextDesc));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolStatusActive));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolStatusInactive));
        Assert.False(string.IsNullOrWhiteSpace(s.ToolStatusReady));
    }
}
