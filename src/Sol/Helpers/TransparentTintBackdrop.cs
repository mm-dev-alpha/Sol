using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace Sol.Helpers;

/// <summary>
/// A custom <see cref="SystemBackdrop"/> that enables transparent desktop rendering for floating overlay
/// and viewfinder windows in WinUI 3 using DirectComposition and DWM blur-behind.
/// </summary>
public sealed class TransparentTintBackdrop : SystemBackdrop
{
    private readonly IntPtr _hwnd;

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DWM_BLURBEHIND
    {
        public uint dwFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fEnable;
        public IntPtr hRgnBlur;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fTransitionOnMaximized;
    }

    private const uint DWM_BB_ENABLE = 0x00000001;
    private const uint DWM_BB_BLURREGION = 0x00000002;

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll")]
    private static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// Initializes a new instance of <see cref="TransparentTintBackdrop"/> for the specified window.
    /// </summary>
    /// <param name="window">Target WinUI 3 Window.</param>
    public TransparentTintBackdrop(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _hwnd = WindowNative.GetWindowHandle(window);
    }

    /// <inheritdoc/>
    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        ConfigureDwm(_hwnd);
        connectedTarget.SystemBackdrop = null;
    }

    /// <inheritdoc/>
    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        disconnectedTarget.SystemBackdrop = null;
        base.OnTargetDisconnected(disconnectedTarget);
    }

    /// <summary>
    /// Configures DWM blur-behind and client area frame extension for transparency.
    /// </summary>
    public static void ConfigureDwm(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            IntPtr hrgn = CreateRectRgn(-2, -2, -1, -1);
            try
            {
                var bb = new DWM_BLURBEHIND
                {
                    dwFlags = DWM_BB_ENABLE | DWM_BB_BLURREGION,
                    fEnable = true,
                    hRgnBlur = hrgn,
                    fTransitionOnMaximized = false
                };
                DwmEnableBlurBehindWindow(hWnd, ref bb);
            }
            finally
            {
                if (hrgn != IntPtr.Zero)
                {
                    DeleteObject(hrgn);
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"TransparentTintBackdrop.ConfigureDwm exception: {ex}");
        }
    }
}
