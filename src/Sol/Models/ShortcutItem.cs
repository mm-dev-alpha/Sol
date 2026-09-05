using System.Collections.Generic;

namespace Sol.Models;

public record ShortcutItem(
    string PrimaryKeyCombination,
    IReadOnlyList<string> Keys,
    string Description,
    string Category);

public record ShortcutCategory(
    string Name,
    IReadOnlyList<ShortcutItem> Shortcuts);
