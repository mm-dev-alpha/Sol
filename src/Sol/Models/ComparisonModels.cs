using System.Collections.Generic;

namespace Sol.Models;

public enum ComparisonMode
{
    Users,
    Computers
}

public enum DiffState
{
    Identical,
    Different,
    OnlyInA,
    OnlyInB
}

public enum InsightSeverity
{
    Info,
    Warning,
    Caution
}

/// <summary>
/// Represents a single property comparison across Target A and Target B.
/// </summary>
public record PropertyDiffItem(
    string Category,
    string PropertyName,
    string? ValueA,
    string? ValueB,
    bool IsDifferent
);

/// <summary>
/// Mathematical 3-way Venn partition of group memberships.
/// </summary>
public record GroupDiffSummary(
    List<string> OnlyInA,
    List<string> InBoth,
    List<string> OnlyInB
)
{
    public int UniqueToACount => OnlyInA.Count;
    public int SharedCount => InBoth.Count;
    public int UniqueToBCount => OnlyInB.Count;
    public int TotalGroupCount => OnlyInA.Count + InBoth.Count + OnlyInB.Count;
    public bool HasDifferences => UniqueToACount > 0 || UniqueToBCount > 0;
}

/// <summary>
/// Contextual intelligence highlighting potential causes of behavior or access divergence.
/// </summary>
public record SmartInsight(
    string Title,
    string Description,
    InsightSeverity Severity,
    string Glyph
);

/// <summary>
/// Complete evaluation payload for a User vs User comparison.
/// </summary>
public record UserComparisonResult(
    AdUser TargetA,
    AdUser TargetB,
    GroupDiffSummary Groups,
    List<PropertyDiffItem> Properties,
    List<SmartInsight> Insights,
    int DifferenceCount
);

/// <summary>
/// Complete evaluation payload for a Computer vs Computer comparison.
/// </summary>
public record ComputerComparisonResult(
    AdComputer TargetA,
    AdComputer TargetB,
    GroupDiffSummary Groups,
    List<PropertyDiffItem> Properties,
    List<SmartInsight> Insights,
    int DifferenceCount
);

public record InitiateComparisonMessage(
    ComparisonMode Mode,
    object TargetEntity
);

/// <summary>
/// Unified representation of a search suggestion for either User or Computer objects.
/// </summary>
public record ComparisonSuggestionItem(
    string Title,
    string Subtitle,
    string Glyph,
    object UnderlyingModel
);

