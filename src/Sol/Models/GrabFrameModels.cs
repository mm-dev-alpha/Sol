using System.Collections.Generic;
using Windows.Foundation;

namespace Sol.Models;

/// <summary>
/// Mode specifying how extracted text from the Grab Frame viewfinder should be structured.
/// </summary>
public enum GrabFrameMode
{
    /// <summary>
    /// Standard multiline text preserving lines and paragraphs.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Single continuous line with collapsed spaces and stripped line breaks.
    /// </summary>
    SingleLine = 1,

    /// <summary>
    /// Tab-delimited table structured by column dividers and detected lines.
    /// </summary>
    Table = 2
}

/// <summary>
/// Represents a vertical column divider placed on the Grab Frame viewfinder.
/// </summary>
public sealed class GrabFrameColumnDivider
{
    /// <summary>
    /// Gets or sets the X position of the divider relative to the viewfinder viewport.
    /// </summary>
    public double XPosition { get; set; }

    /// <summary>
    /// Gets or sets the 0-based column index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets whether the divider is currently being dragged.
    /// </summary>
    public bool IsDragging { get; set; }

    public GrabFrameColumnDivider(double xPosition, int index = 0)
    {
        XPosition = xPosition;
        Index = index;
    }
}

/// <summary>
/// Represents a recognized word item positioned within the viewfinder coordinate space.
/// </summary>
public sealed class GrabFrameWordItem
{
    /// <summary>
    /// Gets the recognized word text.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Gets the bounding rectangle of the word relative to the viewfinder canvas.
    /// </summary>
    public Rect BoundingBox { get; init; }

    /// <summary>
    /// Gets or sets whether this word matches the active search query.
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// Gets or sets whether this word is currently selected by the user.
    /// </summary>
    public bool IsSelected { get; set; }

    public GrabFrameWordItem(string text, Rect boundingBox)
    {
        Text = text;
        BoundingBox = boundingBox;
    }
}

/// <summary>
/// Represents structured table data extracted from the viewfinder using column dividers.
/// </summary>
public sealed record GrabFrameTableResult(
    int ColumnCount,
    int RowCount,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    string FormattedText
);
