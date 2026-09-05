using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class EditTextServiceTests
{
    [Fact]
    public void RequestToggle_RaisesToggleRequestedEvent()
    {
        using var service = new EditTextService();
        bool eventFired = false;
        service.ToggleRequested += (s, e) => eventFired = true;

        service.RequestToggle();

        Assert.True(eventFired);
    }

    [Fact]
    public void RequestOpen_RaisesOpenRequestedEventWithArguments()
    {
        using var service = new EditTextService();
        string? passedText = null;
        EditTextTableDocument? passedTable = null;

        service.OpenRequested += (s, e) =>
        {
            passedText = e.InitialText;
            passedTable = e.InitialTable;
        };

        var table = EditTextTableDocument.CreateFromText("A\tB\r\n1\t2");
        service.RequestOpen("Sample Text", table);

        Assert.Equal("Sample Text", passedText);
        Assert.NotNull(passedTable);
        Assert.Equal(2, passedTable!.RowCount);
    }
}
