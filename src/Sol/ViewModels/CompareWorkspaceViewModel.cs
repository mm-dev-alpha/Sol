using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class CompareWorkspaceViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly IEntityComparisonService _comparisonService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUserMode))]
    [NotifyPropertyChangedFor(nameof(IsComputerMode))]
    [NotifyPropertyChangedFor(nameof(TargetAPlaceholder))]
    [NotifyPropertyChangedFor(nameof(TargetBPlaceholder))]
    [NotifyPropertyChangedFor(nameof(CurrentResultCount))]
    [NotifyPropertyChangedFor(nameof(FilteredGroupItems))]
    [NotifyPropertyChangedFor(nameof(FilteredPropertyItems))]
    [NotifyPropertyChangedFor(nameof(HasUserA))]
    [NotifyPropertyChangedFor(nameof(HasComputerA))]
    [NotifyPropertyChangedFor(nameof(HasUserB))]
    [NotifyPropertyChangedFor(nameof(HasComputerB))]
    [NotifyPropertyChangedFor(nameof(TargetAName))]
    [NotifyPropertyChangedFor(nameof(TargetBName))]
    [NotifyPropertyChangedFor(nameof(TargetASubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetBSubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetAOu))]
    [NotifyPropertyChangedFor(nameof(TargetBOu))]
    [NotifyPropertyChangedFor(nameof(TargetAStatus))]
    [NotifyPropertyChangedFor(nameof(TargetBStatus))]
    [NotifyPropertyChangedFor(nameof(TargetAIconGlyph))]
    [NotifyPropertyChangedFor(nameof(TargetBIconGlyph))]
    [NotifyPropertyChangedFor(nameof(GuidancePrompt))]
    public partial ComparisonMode SelectedMode { get; set; } = ComparisonMode.Users;

    public bool IsUserMode => SelectedMode == ComparisonMode.Users;
    public bool IsComputerMode => SelectedMode == ComparisonMode.Computers;

    public string TargetAPlaceholder => IsUserMode ? Strings.S.CompareTargetPlaceholderUserA : Strings.S.CompareTargetPlaceholderComputerA;
    public string TargetBPlaceholder => IsUserMode ? Strings.S.CompareTargetPlaceholderUserB : Strings.S.CompareTargetPlaceholderComputerB;

    // Side A & B - Users
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetA))]
    [NotifyPropertyChangedFor(nameof(HasUserA))]
    [NotifyPropertyChangedFor(nameof(HasBothTargets))]
    [NotifyPropertyChangedFor(nameof(HasNoTargets))]
    [NotifyPropertyChangedFor(nameof(HasSingleTarget))]
    [NotifyPropertyChangedFor(nameof(TargetAName))]
    [NotifyPropertyChangedFor(nameof(TargetASubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetAOu))]
    [NotifyPropertyChangedFor(nameof(TargetAStatus))]
    [NotifyPropertyChangedFor(nameof(TargetAIconGlyph))]
    [NotifyPropertyChangedFor(nameof(GuidancePrompt))]
    public partial AdUser? UserA { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetB))]
    [NotifyPropertyChangedFor(nameof(HasUserB))]
    [NotifyPropertyChangedFor(nameof(HasBothTargets))]
    [NotifyPropertyChangedFor(nameof(HasNoTargets))]
    [NotifyPropertyChangedFor(nameof(HasSingleTarget))]
    [NotifyPropertyChangedFor(nameof(TargetBName))]
    [NotifyPropertyChangedFor(nameof(TargetBSubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetBOu))]
    [NotifyPropertyChangedFor(nameof(TargetBStatus))]
    [NotifyPropertyChangedFor(nameof(TargetBIconGlyph))]
    [NotifyPropertyChangedFor(nameof(GuidancePrompt))]
    public partial AdUser? UserB { get; set; }

    // Side A & B - Computers
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetA))]
    [NotifyPropertyChangedFor(nameof(HasComputerA))]
    [NotifyPropertyChangedFor(nameof(HasBothTargets))]
    [NotifyPropertyChangedFor(nameof(HasNoTargets))]
    [NotifyPropertyChangedFor(nameof(HasSingleTarget))]
    [NotifyPropertyChangedFor(nameof(TargetAName))]
    [NotifyPropertyChangedFor(nameof(TargetASubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetAOu))]
    [NotifyPropertyChangedFor(nameof(TargetAStatus))]
    [NotifyPropertyChangedFor(nameof(TargetAIconGlyph))]
    [NotifyPropertyChangedFor(nameof(GuidancePrompt))]
    public partial AdComputer? ComputerA { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetB))]
    [NotifyPropertyChangedFor(nameof(HasComputerB))]
    [NotifyPropertyChangedFor(nameof(HasBothTargets))]
    [NotifyPropertyChangedFor(nameof(HasNoTargets))]
    [NotifyPropertyChangedFor(nameof(HasSingleTarget))]
    [NotifyPropertyChangedFor(nameof(TargetBName))]
    [NotifyPropertyChangedFor(nameof(TargetBSubtitle))]
    [NotifyPropertyChangedFor(nameof(TargetBOu))]
    [NotifyPropertyChangedFor(nameof(TargetBStatus))]
    [NotifyPropertyChangedFor(nameof(TargetBIconGlyph))]
    [NotifyPropertyChangedFor(nameof(GuidancePrompt))]
    public partial AdComputer? ComputerB { get; set; }

    public bool HasTargetA => IsUserMode ? UserA != null : ComputerA != null;
    public bool HasTargetB => IsUserMode ? UserB != null : ComputerB != null;
    public bool HasBothTargets => HasTargetA && HasTargetB;
    public bool HasNoTargets => !HasTargetA && !HasTargetB;
    public bool HasSingleTarget => HasTargetA ^ HasTargetB;

    public bool HasUserA => IsUserMode && UserA != null;
    public bool HasComputerA => IsComputerMode && ComputerA != null;
    public bool HasUserB => IsUserMode && UserB != null;
    public bool HasComputerB => IsComputerMode && ComputerB != null;

    public string TargetAName => IsUserMode
        ? (!string.IsNullOrWhiteSpace(UserA?.DisplayName) ? UserA.DisplayName : UserA?.SamAccountName ?? Strings.S.CompareTargetALabel)
        : (ComputerA?.Name ?? Strings.S.CompareTargetALabel);

    public string TargetBName => IsUserMode
        ? (!string.IsNullOrWhiteSpace(UserB?.DisplayName) ? UserB.DisplayName : UserB?.SamAccountName ?? Strings.S.CompareTargetBLabel)
        : (ComputerB?.Name ?? Strings.S.CompareTargetBLabel);

    public string TargetASubtitle => IsUserMode
        ? (UserA?.SamAccountName ?? string.Empty)
        : (!string.IsNullOrWhiteSpace(ComputerA?.OperatingSystem) ? ComputerA.OperatingSystem : ComputerA?.DnsHostName ?? string.Empty);

    public string TargetBSubtitle => IsUserMode
        ? (UserB?.SamAccountName ?? string.Empty)
        : (!string.IsNullOrWhiteSpace(ComputerB?.OperatingSystem) ? ComputerB.OperatingSystem : ComputerB?.DnsHostName ?? string.Empty);

    public string TargetAOu => IsUserMode ? (UserA?.OuPath ?? string.Empty) : (ComputerA?.OuPath ?? string.Empty);
    public string TargetBOu => IsUserMode ? (UserB?.OuPath ?? string.Empty) : (ComputerB?.OuPath ?? string.Empty);

    public string TargetAStatus => IsUserMode ? (UserA?.AccountStatus ?? string.Empty) : (ComputerA?.AccountStatus ?? string.Empty);
    public string TargetBStatus => IsUserMode ? (UserB?.AccountStatus ?? string.Empty) : (ComputerB?.AccountStatus ?? string.Empty);

    public string TargetAIconGlyph => IsUserMode ? "\uE77B" : "\uE7F8";
    public string TargetBIconGlyph => IsUserMode ? "\uE77B" : "\uE7F8";

    public string GuidancePrompt => HasNoTargets
        ? Strings.S.CompareGuidanceBothPrompt
        : string.Format(Strings.S.CompareGuidanceSecondPrompt, HasTargetA ? TargetAName : TargetBName);

    // Active Results
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveComparison))]
    [NotifyPropertyChangedFor(nameof(CurrentResultCount))]
    [NotifyPropertyChangedFor(nameof(DiffCountBadgeText))]
    [NotifyPropertyChangedFor(nameof(HasInsights))]
    [NotifyPropertyChangedFor(nameof(CurrentInsights))]
    [NotifyPropertyChangedFor(nameof(FilteredGroupItems))]
    [NotifyPropertyChangedFor(nameof(FilteredPropertyItems))]
    [NotifyPropertyChangedFor(nameof(GroupTotalCount))]
    [NotifyPropertyChangedFor(nameof(GroupDiffsCount))]
    [NotifyPropertyChangedFor(nameof(GroupOnlyACount))]
    [NotifyPropertyChangedFor(nameof(GroupSharedCount))]
    [NotifyPropertyChangedFor(nameof(GroupOnlyBCount))]
    [NotifyPropertyChangedFor(nameof(GroupFilterAllLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterDiffsLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterOnlyALabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterSharedLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterOnlyBLabel))]
    public partial UserComparisonResult? UserResult { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveComparison))]
    [NotifyPropertyChangedFor(nameof(CurrentResultCount))]
    [NotifyPropertyChangedFor(nameof(DiffCountBadgeText))]
    [NotifyPropertyChangedFor(nameof(HasInsights))]
    [NotifyPropertyChangedFor(nameof(CurrentInsights))]
    [NotifyPropertyChangedFor(nameof(FilteredGroupItems))]
    [NotifyPropertyChangedFor(nameof(FilteredPropertyItems))]
    [NotifyPropertyChangedFor(nameof(GroupTotalCount))]
    [NotifyPropertyChangedFor(nameof(GroupDiffsCount))]
    [NotifyPropertyChangedFor(nameof(GroupOnlyACount))]
    [NotifyPropertyChangedFor(nameof(GroupSharedCount))]
    [NotifyPropertyChangedFor(nameof(GroupOnlyBCount))]
    [NotifyPropertyChangedFor(nameof(GroupFilterAllLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterDiffsLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterOnlyALabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterSharedLabel))]
    [NotifyPropertyChangedFor(nameof(GroupFilterOnlyBLabel))]
    public partial ComputerComparisonResult? ComputerResult { get; set; }

    public bool HasActiveComparison => IsUserMode ? UserResult != null : ComputerResult != null;
    public int CurrentResultCount => IsUserMode ? (UserResult?.DifferenceCount ?? 0) : (ComputerResult?.DifferenceCount ?? 0);

    public string DiffCountBadgeText => CurrentResultCount > 0
        ? string.Format(Strings.S.CompareDiffCountBadge, CurrentResultCount)
        : Strings.S.CompareIdenticalBadge;

    public bool HasInsights => CurrentInsights.Count > 0;

    public List<SmartInsight> CurrentInsights => IsUserMode
        ? (UserResult?.Insights ?? [])
        : (ComputerResult?.Insights ?? []);

    public int GroupTotalCount => (IsUserMode ? UserResult?.Groups?.TotalGroupCount : ComputerResult?.Groups?.TotalGroupCount) ?? 0;
    public int GroupDiffsCount => ((IsUserMode ? UserResult?.Groups?.UniqueToACount : ComputerResult?.Groups?.UniqueToACount) ?? 0)
                                + ((IsUserMode ? UserResult?.Groups?.UniqueToBCount : ComputerResult?.Groups?.UniqueToBCount) ?? 0);
    public int GroupOnlyACount => (IsUserMode ? UserResult?.Groups?.UniqueToACount : ComputerResult?.Groups?.UniqueToACount) ?? 0;
    public int GroupSharedCount => (IsUserMode ? UserResult?.Groups?.SharedCount : ComputerResult?.Groups?.SharedCount) ?? 0;
    public int GroupOnlyBCount => (IsUserMode ? UserResult?.Groups?.UniqueToBCount : ComputerResult?.Groups?.UniqueToBCount) ?? 0;

    public string GroupFilterAllLabel => string.Format(Strings.S.CompareGroupFilterAll, GroupTotalCount);
    public string GroupFilterDiffsLabel => string.Format(Strings.S.CompareGroupFilterDiffs, GroupDiffsCount);
    public string GroupFilterOnlyALabel => string.Format(Strings.S.CompareGroupOnlyA, TargetAName, GroupOnlyACount);
    public string GroupFilterSharedLabel => string.Format(Strings.S.CompareGroupShared, GroupSharedCount);
    public string GroupFilterOnlyBLabel => string.Format(Strings.S.CompareGroupOnlyB, TargetBName, GroupOnlyBCount);

    // Filter and View Toggles
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPropertyItems))]
    public partial bool ShowDifferencesOnly { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPropertyItems))]
    public partial string PropertySearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroupItems))]
    public partial string GroupFilter { get; set; } = "All"; // "All", "DifferencesOnly", "OnlyInA", "Shared", "OnlyInB"

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroupItems))]
    public partial string GroupSearchQuery { get; set; } = string.Empty;

    // AutoSuggest Suggestions
    public ObservableCollection<ComparisonSuggestionItem> SuggestionsA { get; } = [];
    public ObservableCollection<ComparisonSuggestionItem> SuggestionsB { get; } = [];
    public ObservableCollection<AdUser> UserSuggestionsA { get; } = [];
    public ObservableCollection<AdUser> UserSuggestionsB { get; } = [];
    public ObservableCollection<AdComputer> ComputerSuggestionsA { get; } = [];
    public ObservableCollection<AdComputer> ComputerSuggestionsB { get; } = [];

    public CompareWorkspaceViewModel(
        IActiveDirectoryService adService,
        IEntityComparisonService comparisonService,
        INavigationService navigationService)
    {
        _adService = adService ?? throw new ArgumentNullException(nameof(adService));
        _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        WeakReferenceMessenger.Default.Register<CompareWorkspaceViewModel, InitiateComparisonMessage>(this, static (r, m) =>
        {
            r.HandleInitiateComparison(m);
        });
    }

    private void HandleInitiateComparison(InitiateComparisonMessage message)
    {
        SelectedMode = message.Mode;
        if (message.Mode == ComparisonMode.Users && message.TargetEntity is AdUser user)
        {
            UserA = user;
            UserB = null;
            UserResult = null;
        }
        else if (message.Mode == ComparisonMode.Computers && message.TargetEntity is AdComputer computer)
        {
            ComputerA = computer;
            ComputerB = null;
            ComputerResult = null;
        }
    }

    partial void OnUserAChanged(AdUser? value) => RecalculateDiff();
    partial void OnUserBChanged(AdUser? value) => RecalculateDiff();
    partial void OnComputerAChanged(AdComputer? value) => RecalculateDiff();
    partial void OnComputerBChanged(AdComputer? value) => RecalculateDiff();

    partial void OnSelectedModeChanged(ComparisonMode value)
    {
        ClearComparison();
    }

    public void RecalculateDiff()
    {
        if (IsUserMode)
        {
            if (UserA != null && UserB != null)
            {
                UserResult = _comparisonService.CompareUsers(UserA, UserB);
            }
            else
            {
                UserResult = null;
            }
        }
        else
        {
            if (ComputerA != null && ComputerB != null)
            {
                ComputerResult = _comparisonService.CompareComputers(ComputerA, ComputerB);
            }
            else
            {
                ComputerResult = null;
            }
        }
    }

    [RelayCommand]
    public void SwapTargets()
    {
        if (IsUserMode)
        {
            (UserA, UserB) = (UserB, UserA);
        }
        else
        {
            (ComputerA, ComputerB) = (ComputerB, ComputerA);
        }
    }

    [RelayCommand]
    public void RemoveTargetA()
    {
        if (IsUserMode)
        {
            UserA = null;
        }
        else
        {
            ComputerA = null;
        }
    }

    [RelayCommand]
    public void RemoveTargetB()
    {
        if (IsUserMode)
        {
            UserB = null;
        }
        else
        {
            ComputerB = null;
        }
    }

    public void SelectSuggestion(ComparisonSuggestionItem item, bool isTargetA)
    {
        if (item.UnderlyingModel is AdUser user)
        {
            if (isTargetA) UserA = user; else UserB = user;
        }
        else if (item.UnderlyingModel is AdComputer comp)
        {
            if (isTargetA) ComputerA = comp; else ComputerB = comp;
        }
    }

    [RelayCommand]
    public void ClearComparison()
    {
        UserA = null;
        UserB = null;
        ComputerA = null;
        ComputerB = null;
        UserResult = null;
        ComputerResult = null;
        SuggestionsA.Clear();
        SuggestionsB.Clear();
        UserSuggestionsA.Clear();
        UserSuggestionsB.Clear();
        ComputerSuggestionsA.Clear();
        ComputerSuggestionsB.Clear();
        GroupSearchQuery = string.Empty;
        PropertySearchQuery = string.Empty;
    }

    [RelayCommand]
    public void CopyComparisonReport()
    {
        string report = GenerateReportText();

        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(report);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Fallback for headless / non-UI testing environments
        }

        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.CompareReportCopied));
    }

    public string GenerateReportText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {Strings.S.CompareTitle}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (IsUserMode && UserResult != null)
        {
            sb.AppendLine($"## Targets");
            sb.AppendLine($"- Target A: {UserResult.TargetA.DisplayName} ({UserResult.TargetA.SamAccountName})");
            sb.AppendLine($"- Target B: {UserResult.TargetB.DisplayName} ({UserResult.TargetB.SamAccountName})");
            sb.AppendLine($"- Total Discrepancies: {UserResult.DifferenceCount}");
            sb.AppendLine();

            if (UserResult.Insights.Count > 0)
            {
                sb.AppendLine($"### Smart Discrepancy Insights");
                foreach (var insight in UserResult.Insights)
                {
                    sb.AppendLine($"- [{insight.Severity}] **{insight.Title}**: {insight.Description}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"### Group Membership Breakdown");
            sb.AppendLine($"- Shared in Both ({UserResult.Groups.SharedCount}): {string.Join(", ", UserResult.Groups.InBoth)}");
            sb.AppendLine($"- Only in Target A ({UserResult.Groups.UniqueToACount}): {string.Join(", ", UserResult.Groups.OnlyInA)}");
            sb.AppendLine($"- Only in Target B ({UserResult.Groups.UniqueToBCount}): {string.Join(", ", UserResult.Groups.OnlyInB)}");
            sb.AppendLine();

            sb.AppendLine($"### Attribute Comparison Matrix");
            foreach (var prop in UserResult.Properties)
            {
                string status = prop.IsDifferent ? "[DIFFERENT]" : "[IDENTICAL]";
                sb.AppendLine($"- {prop.Category} | {prop.PropertyName}: '{prop.ValueA}' vs '{prop.ValueB}' {status}");
            }
        }
        else if (IsComputerMode && ComputerResult != null)
        {
            sb.AppendLine($"## Targets");
            sb.AppendLine($"- Target A: {ComputerResult.TargetA.Name} ({ComputerResult.TargetA.SamAccountName})");
            sb.AppendLine($"- Target B: {ComputerResult.TargetB.Name} ({ComputerResult.TargetB.SamAccountName})");
            sb.AppendLine($"- Total Discrepancies: {ComputerResult.DifferenceCount}");
            sb.AppendLine();

            if (ComputerResult.Insights.Count > 0)
            {
                sb.AppendLine($"### Smart Discrepancy Insights");
                foreach (var insight in ComputerResult.Insights)
                {
                    sb.AppendLine($"- [{insight.Severity}] **{insight.Title}**: {insight.Description}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"### Group Membership Breakdown");
            sb.AppendLine($"- Shared in Both ({ComputerResult.Groups.SharedCount}): {string.Join(", ", ComputerResult.Groups.InBoth)}");
            sb.AppendLine($"- Only in Target A ({ComputerResult.Groups.UniqueToACount}): {string.Join(", ", ComputerResult.Groups.OnlyInA)}");
            sb.AppendLine($"- Only in Target B ({ComputerResult.Groups.UniqueToBCount}): {string.Join(", ", ComputerResult.Groups.OnlyInB)}");
            sb.AppendLine();

            sb.AppendLine($"### Attribute Comparison Matrix");
            foreach (var prop in ComputerResult.Properties)
            {
                string status = prop.IsDifferent ? "[DIFFERENT]" : "[IDENTICAL]";
                sb.AppendLine($"- {prop.Category} | {prop.PropertyName}: '{prop.ValueA}' vs '{prop.ValueB}' {status}");
            }
        }
        else
        {
            sb.AppendLine("No active comparison.");
        }

        return sb.ToString();
    }

    public List<PropertyDiffItem> FilteredPropertyItems
    {
        get
        {
            var source = IsUserMode ? UserResult?.Properties : ComputerResult?.Properties;
            if (source == null) return [];

            var query = source.AsEnumerable();

            if (ShowDifferencesOnly)
            {
                query = query.Where(p => p.IsDifferent);
            }

            if (!string.IsNullOrWhiteSpace(PropertySearchQuery))
            {
                string q = PropertySearchQuery.Trim();
                query = query.Where(p =>
                    p.PropertyName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.ValueA != null && p.ValueA.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.ValueB != null && p.ValueB.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            return query.ToList();
        }
    }

    public List<GroupDiffRow> FilteredGroupItems
    {
        get
        {
            var groups = IsUserMode ? UserResult?.Groups : ComputerResult?.Groups;
            if (groups == null) return [];

            var rows = new List<GroupDiffRow>();

            foreach (var g in groups.OnlyInA)
            {
                rows.Add(new GroupDiffRow(g, DiffState.OnlyInA, TargetAName, string.Format(Strings.S.CompareGroupOnlyABadge, TargetAName)));
            }

            foreach (var g in groups.InBoth)
            {
                rows.Add(new GroupDiffRow(g, DiffState.Identical, Strings.S.CompareGroupFilterSharedItem, Strings.S.CompareGroupInBothBadge));
            }

            foreach (var g in groups.OnlyInB)
            {
                rows.Add(new GroupDiffRow(g, DiffState.OnlyInB, TargetBName, string.Format(Strings.S.CompareGroupOnlyBBadge, TargetBName)));
            }

            var query = rows.AsEnumerable();

            switch (GroupFilter)
            {
                case "DifferencesOnly":
                    query = query.Where(r => r.State != DiffState.Identical);
                    break;
                case "OnlyInA":
                    query = query.Where(r => r.State == DiffState.OnlyInA);
                    break;
                case "Shared":
                    query = query.Where(r => r.State == DiffState.Identical);
                    break;
                case "OnlyInB":
                    query = query.Where(r => r.State == DiffState.OnlyInB);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(GroupSearchQuery))
            {
                string q = GroupSearchQuery.Trim();
                query = query.Where(r => r.GroupName.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(r => r.GroupName, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public async Task SearchAsync(string query, bool isTargetA)
    {
        var targetSuggestions = isTargetA ? SuggestionsA : SuggestionsB;
        var targetUsers = isTargetA ? UserSuggestionsA : UserSuggestionsB;
        var targetComputers = isTargetA ? ComputerSuggestionsA : ComputerSuggestionsB;

        if (string.IsNullOrWhiteSpace(query))
        {
            targetSuggestions.Clear();
            targetUsers.Clear();
            targetComputers.Clear();
            return;
        }

        targetSuggestions.Clear();
        targetUsers.Clear();
        targetComputers.Clear();

        if (IsUserMode)
        {
            var results = await _adService.SearchUsersAsync(query);
            foreach (var user in results)
            {
                targetUsers.Add(user);
                string title = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.SamAccountName;
                string subtitle = user.SamAccountName + (!string.IsNullOrWhiteSpace(user.Upn) ? $" ({user.Upn})" : "");
                targetSuggestions.Add(new ComparisonSuggestionItem(title, subtitle, "\uE77B", user));
            }
        }
        else
        {
            var results = await _adService.SearchComputersAsync(query);
            foreach (var comp in results)
            {
                targetComputers.Add(comp);
                string title = comp.Name;
                string subtitle = !string.IsNullOrWhiteSpace(comp.OperatingSystem)
                    ? $"{comp.OperatingSystem} ({comp.DnsHostName})"
                    : (!string.IsNullOrWhiteSpace(comp.DnsHostName) ? comp.DnsHostName : comp.SamAccountName);
                targetSuggestions.Add(new ComparisonSuggestionItem(title, subtitle, "\uE7F8", comp));
            }
        }
    }
}

public record GroupDiffRow(
    string GroupName,
    DiffState State,
    string OwnerLabel,
    string BadgeText
);

