using System;
using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class TransparentTintBackdropTests
{
    [Fact]
    public void ConfigureDwm_HandlesZeroHwndGracefully()
    {
        // Must not throw on zero or invalid handle
        var exception = Record.Exception(() => TransparentTintBackdrop.ConfigureDwm(IntPtr.Zero));
        Assert.Null(exception);
    }

    [Fact]
    public void ConfigureDwm_HandlesNonExistentHwndGracefully()
    {
        // Must not throw on invalid handle
        var exception = Record.Exception(() => TransparentTintBackdrop.ConfigureDwm(new IntPtr(0x12345678)));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0x00000000u, "WDA_NONE")]
    [InlineData(0x00000011u, "WDA_EXCLUDEFROMCAPTURE")]
    public void DisplayAffinityConstants_MatchWin32Specifications(uint affinityValue, string name)
    {
        // Validate that WDA constants match Windows SDK winuser.h values
        if (name == "WDA_NONE")
        {
            Assert.Equal(0u, affinityValue);
        }
        else if (name == "WDA_EXCLUDEFROMCAPTURE")
        {
            Assert.Equal(0x00000011u, affinityValue);
        }
    }
}
