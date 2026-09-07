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
    private Windows.UI.Composition.Compositor? _compositor;
    private Windows.UI.Composition.CompositionColorBrush? _brush;
    private readonly SubclassProc _subclassProc;
    private bool _isSubclassed;

    private const int SUBCLASS_ID = 0x5442; // 'TB'
    private const uint WM_ERASEBKGND = 0x0014;
    private const uint WM_DWMCOMPOSITIONCHANGED = 0x031E;

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

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
        _subclassProc = WndProc;
    }

    /// <inheritdoc/>
    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        if (_hwnd != IntPtr.Zero && !_isSubclassed)
        {
            _isSubclassed = SetWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID, UIntPtr.Zero);
        }

        ConfigureDwm(_hwnd);

        _compositor = new Windows.UI.Composition.Compositor();
        _brush = _compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        connectedTarget.SystemBackdrop = _brush;
    }

    /// <inheritdoc/>
    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        if (_hwnd != IntPtr.Zero && _isSubclassed)
        {
            RemoveWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID);
            _isSubclassed = false;
        }

        disconnectedTarget.SystemBackdrop = null;
        _brush?.Dispose();
        _brush = null;
        _compositor?.Dispose();
        _compositor = null;

        base.OnTargetDisconnected(disconnectedTarget);
    }

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == WM_ERASEBKGND)
        {
            return (IntPtr)1; // Prevent GDI default erase with opaque window brush
        }

        if (uMsg == WM_DWMCOMPOSITIONCHANGED)
        {
            ConfigureDwm(hWnd);
            return IntPtr.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
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
            var margins = new MARGINS { cxLeftWidth = 0, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 };
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
