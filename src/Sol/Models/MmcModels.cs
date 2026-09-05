using System;

namespace Sol.Models;

public enum MmcCategory
{
    All = 0,
    ActiveDirectory = 1,
    System = 2,
    Management = 3,
    Networking = 4,
    Diagnostics = 5,
    Security = 6,
    Storage = 7
}

public sealed class MmcToolItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string Description { get; set; } = string.Empty;
    public MmcCategory Category { get; set; } = MmcCategory.Management;
    public bool IsFavorite { get; set; }
    public bool RequiresElevation { get; set; } = true;

    public string FavoriteGlyph => IsFavorite ? "\uE735" : "\uE734";
    public string FullRunCommand => string.IsNullOrWhiteSpace(Arguments) ? Command : $"{Command} {Arguments}";

    public string CategoryDisplayName => Category switch
    {
        MmcCategory.ActiveDirectory => "Active Directory",
        MmcCategory.System => "System",
        MmcCategory.Management => "Management",
        MmcCategory.Networking => "Networking",
        MmcCategory.Diagnostics => "Diagnostics",
        MmcCategory.Security => "Security",
        MmcCategory.Storage => "Storage",
        _ => "General"
    };

    public string Glyph => Category switch
    {
        MmcCategory.ActiveDirectory => "\uE716",
        MmcCategory.System => "\uE770",
        MmcCategory.Management => "\uE713",
        MmcCategory.Networking => "\uE839",
        MmcCategory.Diagnostics => "\uE9D9",
        MmcCategory.Security => "\uE72E",
        MmcCategory.Storage => "\uEDA2",
        _ => "\uE756"
    };
}
