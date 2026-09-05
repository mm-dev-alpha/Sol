using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;
using Xunit;

namespace Sol.Tests;

public class CompareWorkspaceViewModelTests
{
    private readonly IActiveDirectoryService _adService = new FakeActiveDirectoryService();
    private readonly IEntityComparisonService _compService = new EntityComparisonService();
    private readonly INavigationService _navService = new FakeNavigationService();

    [Fact]
    public void SwapTargets_InvertsUserAAndUserB()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService);
        var u1 = new AdUser { SamAccountName = "user1", GivenName = "Alice", Groups = ["GroupA"] };
        var u2 = new AdUser { SamAccountName = "user2", GivenName = "Bob", Groups = ["GroupB"] };

        vm.UserA = u1;
        vm.UserB = u2;

        vm.SwapTargetsCommand.Execute(null);

        Assert.Equal(u2, vm.UserA);
        Assert.Equal(u1, vm.UserB);
        Assert.NotNull(vm.UserResult);
    }

    [Fact]
    public void SwapTargets_InvertsComputerAAndComputerB()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService)
        {
            SelectedMode = ComparisonMode.Computers
        };
        var c1 = new AdComputer { Name = "PC-01", Groups = ["G1"] };
        var c2 = new AdComputer { Name = "PC-02", Groups = ["G2"] };

        vm.ComputerA = c1;
        vm.ComputerB = c2;

        vm.SwapTargetsCommand.Execute(null);

        Assert.Equal(c2, vm.ComputerA);
        Assert.Equal(c1, vm.ComputerB);
        Assert.NotNull(vm.ComputerResult);
    }

    [Fact]
    public void ClearComparison_ResetsState()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService);
        vm.UserA = new AdUser { SamAccountName = "u1" };
        vm.UserB = new AdUser { SamAccountName = "u2" };

        vm.ClearComparisonCommand.Execute(null);

        Assert.Null(vm.UserA);
        Assert.Null(vm.UserB);
        Assert.Null(vm.UserResult);
        Assert.False(vm.HasActiveComparison);
    }

    [Fact]
    public void InitiateComparisonMessage_PrepopulatesSideA()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService);
        var u1 = new AdUser { SamAccountName = "target1", GivenName = "Target" };

        WeakReferenceMessenger.Default.Send(new InitiateComparisonMessage(ComparisonMode.Users, u1));

        Assert.Equal(ComparisonMode.Users, vm.SelectedMode);
        Assert.Equal(u1, vm.UserA);
        Assert.Null(vm.UserB);
    }

    [Fact]
    public void BuildReportText_IncludesAllDifferencesAndGroups()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService);
        var u1 = new AdUser { SamAccountName = "alice", Department = "Finance", Groups = ["Domain Users", "Finance-Team"] };
        var u2 = new AdUser { SamAccountName = "bob", Department = "Engineering", Groups = ["Domain Users", "DevOps"] };

        vm.UserA = u1;
        vm.UserB = u2;

        string report = vm.GenerateReportText();

        Assert.Contains("alice", report);
        Assert.Contains("bob", report);
        Assert.Contains("Finance", report);
        Assert.Contains("Engineering", report);
        Assert.Contains("Finance-Team", report);
        Assert.Contains("DevOps", report);
        Assert.Contains("Domain Users", report);
    }

    [Fact]
    public void ObjectToVisibilityConverter_ReturnsExpectedVisibility()
    {
        var converter = new Sol.Converters.ObjectToVisibilityConverter();
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, converter.Convert(new object(), typeof(Microsoft.UI.Xaml.Visibility), null!, "en-US"));
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, converter.Convert(null!, typeof(Microsoft.UI.Xaml.Visibility), null!, "en-US"));
    }

    [Fact]
    public void ComparisonSuggestionItem_HoldsPropertiesCorrectly()
    {
        var u = new AdUser { DisplayName = "Alice Doe", SamAccountName = "adoe" };
        var item = new ComparisonSuggestionItem(u.DisplayName, u.SamAccountName, "\uE77B", u);
        Assert.Equal("Alice Doe", item.Title);
        Assert.Equal("adoe", item.Subtitle);
        Assert.Equal("\uE77B", item.Glyph);
        Assert.Same(u, item.UnderlyingModel);
    }

    [Fact]
    public async Task SearchAsync_UserMode_PopulatesUnifiedSuggestions()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService)
        {
            SelectedMode = ComparisonMode.Users
        };

        await vm.SearchAsync("alice", isTargetA: true);

        Assert.NotEmpty(vm.SuggestionsA);
        var sugg = vm.SuggestionsA[0];
        Assert.Equal("Alice Doe", sugg.Title);
        Assert.Contains("adoe", sugg.Subtitle);
        Assert.Equal("\uE77B", sugg.Glyph);
        Assert.IsType<AdUser>(sugg.UnderlyingModel);
    }

    [Fact]
    public async Task SearchAsync_ComputerMode_PopulatesUnifiedSuggestions()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService)
        {
            SelectedMode = ComparisonMode.Computers
        };

        await vm.SearchAsync("pc", isTargetA: true);

        Assert.NotEmpty(vm.SuggestionsA);
        var sugg = vm.SuggestionsA[0];
        Assert.Equal("PC-FINANCE-01", sugg.Title);
        Assert.Contains("Windows 11", sugg.Subtitle);
        Assert.Equal("\uE7F8", sugg.Glyph);
        Assert.IsType<AdComputer>(sugg.UnderlyingModel);
    }

    [Fact]
    public void SelectSuggestion_SetsTargetAndRemoveTargetA_Clears()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService)
        {
            SelectedMode = ComparisonMode.Users
        };
        var user = new AdUser { DisplayName = "Charlie", SamAccountName = "charlie" };
        var item = new ComparisonSuggestionItem("Charlie", "charlie", "\uE77B", user);

        vm.SelectSuggestion(item, isTargetA: true);
        Assert.Equal(user, vm.UserA);
        Assert.True(vm.HasTargetA);
        Assert.False(vm.HasBothTargets);

        vm.RemoveTargetACommand.Execute(null);
        Assert.Null(vm.UserA);
        Assert.False(vm.HasTargetA);
    }

    [Fact]
    public void GuidancePrompt_AdaptsToSelectionState()
    {
        var vm = new CompareWorkspaceViewModel(_adService, _compService, _navService);
        Assert.Equal(Sol.Helpers.Strings.S.CompareGuidanceBothPrompt, vm.GuidancePrompt);

        vm.UserA = new AdUser { DisplayName = "Alice", SamAccountName = "alice" };
        Assert.Contains("Alice", vm.GuidancePrompt);
    }

    private class FakeActiveDirectoryService : IActiveDirectoryService
    {
        public Task<List<AdUser>> SearchUsersAsync(string query) => Task.FromResult(new List<AdUser>
        {
            new AdUser { DisplayName = "Alice Doe", SamAccountName = "adoe", Upn = "alice@domain.com" }
        });
        public Task<List<string>> SearchGroupsAsync(string query) => Task.FromResult(new List<string>());
        public Task<List<KeyValuePair<string, string>>> GetAllUserAttributesAsync(string samAccountName) => Task.FromResult(new List<KeyValuePair<string, string>>());
        public Task UnlockAccountAsync(string samAccountName) => Task.CompletedTask;
        public Task EnableAccountAsync(string samAccountName, bool enable) => Task.CompletedTask;
        public Task ResetPasswordAsync(string samAccountName, string newPassword, bool requireChangeAtNextLogon) => Task.CompletedTask;
        public Task ForcePasswordChangeAsync(string samAccountName) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string samAccountName, Dictionary<string, string> attributes, string? newManager) => Task.CompletedTask;
        public Task UpdateRawAttributeAsync(string samAccountName, string attributeName, string newValue) => Task.CompletedTask;
        public Task AddUserToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveUserFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task<List<AdComputer>> SearchComputersAsync(string query) => Task.FromResult(new List<AdComputer>
        {
            new AdComputer { Name = "PC-FINANCE-01", OperatingSystem = "Windows 11 Pro", DnsHostName = "pc-fin01.domain.com" }
        });
        public Task EnableComputerAccountAsync(string samAccountName, bool enable) => Task.CompletedTask;
        public Task AddComputerToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveComputerFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
    }

    private class FakeNavigationService : INavigationService
    {
        public event EventHandler<string>? Navigated;
        public string? CurrentPageKey => null;
        public bool CanGoBack => false;
        public bool GoBack() => false;
        public void Initialize(Microsoft.UI.Xaml.Controls.Frame frame) { }
        public bool NavigateTo(string pageKey, object? parameter = null)
        {
            Navigated?.Invoke(this, pageKey);
            return true;
        }
        public void RegisterPage(string pageKey, Type pageType) { }
    }
}
