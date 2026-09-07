using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class SafeClipboardTests
{
    [Fact]
    public void TrySetText_NullOrEmpty_DoesNotThrow()
    {
        // SafeClipboard should handle null or empty strings gracefully without throwing
        var ex = Record.Exception(() => SafeClipboard.TrySetText(null));
        Assert.Null(ex);

        ex = Record.Exception(() => SafeClipboard.TrySetText(string.Empty));
        Assert.Null(ex);
    }

    [Fact]
    public void TrySetText_NormalString_DoesNotThrowUncaughtException()
    {
        // In unit test runners where Clipboard COM handle may be unavailable,
        // SafeClipboard should swallow the COMException and return boolean instead of crashing.
        var ex = Record.Exception(() => SafeClipboard.TrySetText("Test clipboard content"));
        Assert.Null(ex);
    }

    [Fact]
    public void TrySetText_SensitiveString_DoesNotThrowUncaughtException()
    {
        var ex = Record.Exception(() => SafeClipboard.TrySetText("Sensitive secret content", isSensitive: true));
        Assert.Null(ex);
    }

    [Fact]
    public async System.Threading.Tasks.Task TrySetTextAsync_DoesNotThrowUncaughtException()
    {
        var ex = await Record.ExceptionAsync(async () => await SafeClipboard.TrySetTextAsync("Async content", isSensitive: true));
        Assert.Null(ex);
    }
}
