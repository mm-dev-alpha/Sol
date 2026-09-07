using System;

namespace Sol.Models;

public enum AdminShellType
{
    All = 0,
    PowerShell = 1,
    Cmd = 2
}

public enum AdminCommandCategory
{
    All = 0,
    ActiveDirectory = 1,
    Networking = 2,
    GroupPolicy = 3,
    Diagnostics = 4,
    Security = 5,
    RemoteManagement = 6,
    Storage = 7
}

public sealed class AdminCommandItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public AdminShellType ShellType { get; set; } = AdminShellType.PowerShell;
    public AdminCommandCategory Category { get; set; } = AdminCommandCategory.ActiveDirectory;
    public string Description { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool RequiresElevation { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public string? RequiredFeature { get; set; }

    public string FavoriteGlyph => IsFavorite ? "\uE735" : "\uE734";
    public string ShellBadgeText => ShellType == AdminShellType.PowerShell ? "PS" : "CMD";

    public string CategoryDisplayName => Category switch
    {
        AdminCommandCategory.ActiveDirectory => "Active Directory",
        AdminCommandCategory.Networking => "Networking",
        AdminCommandCategory.GroupPolicy => "Group Policy",
        AdminCommandCategory.Diagnostics => "Diagnostics",
        AdminCommandCategory.Security => "Security",
        AdminCommandCategory.RemoteManagement => "Remote",
        AdminCommandCategory.Storage => "Storage",
        _ => "General"
    };

    public string Glyph => Category switch
    {
        AdminCommandCategory.ActiveDirectory => "\uE716",
        AdminCommandCategory.Networking => "\uE839",
        AdminCommandCategory.GroupPolicy => "\uE71D",
        AdminCommandCategory.Diagnostics => "\uE9D9",
        AdminCommandCategory.Security => "\uE72E",
        AdminCommandCategory.RemoteManagement => "\uE8AF",
        AdminCommandCategory.Storage => "\uEDA2",
        _ => "\uE756"
    };
}
