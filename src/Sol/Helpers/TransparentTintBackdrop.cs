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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
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

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, out IntPtr dispatcherQueueController);

    private static IntPtr _dispatcherQueueController = IntPtr.Zero;
    private static Windows.UI.Composition.Compositor? _compositor;
    private static readonly object _compositorLock = new();

    private static IntPtr _blackBrush = IntPtr.Zero;
    private static readonly object _brushLock = new();

    private static void EnsureDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() == null && _dispatcherQueueController == IntPtr.Zero)
        {
            try
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                int hr = CreateDispatcherQueueController(options, out _dispatcherQueueController);
                AppLog.Write($"TransparentTintBackdrop: CreateDispatcherQueueController hr=0x{hr:X8}, handle={_dispatcherQueueController}");
            }
            catch (Exception ex)
            {
                AppLog.Write($"TransparentTintBackdrop.EnsureDispatcherQueueController exception: {ex.Message}");
            }
        }
    }

    private static Windows.UI.Composition.Compositor Compositor
    {
        get
        {
            if (_compositor == null)
            {
                lock (_compositorLock)
                {
                    if (_compositor == null)
                    {
                        EnsureDispatcherQueueController();
                        _compositor = new Windows.UI.Composition.Compositor();
                    }
                }
            }
            return _compositor;
        }
    }

    private static bool ClearBackground(IntPtr hWnd, IntPtr hdc)
    {
        if (hWnd == IntPtr.Zero || hdc == IntPtr.Zero)
        {
            return false;
        }

        if (GetClientRect(hWnd, out var rect))
        {
            if (_blackBrush == IntPtr.Zero)
            {
                lock (_brushLock)
                {
                    if (_blackBrush == IntPtr.Zero)
                    {
                        _blackBrush = CreateSolidBrush(0); // COLORREF(0) = RGB(0,0,0)
                    }
                }
            }
            FillRect(hdc, ref rect, _blackBrush);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TransparentTintBackdrop"/> with default configuration.
    /// </summary>
    public TransparentTintBackdrop() : this(IntPtr.Zero)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TransparentTintBackdrop"/> for the specified window handle.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    public TransparentTintBackdrop(IntPtr hWnd)
    {
        _hwnd = hWnd;
        _subclassProc = WndProc;
    }

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

        try
        {
            IntPtr hWnd = _hwnd;
            if (hWnd == IntPtr.Zero && xamlRoot?.ContentIslandEnvironment != null)
            {
                hWnd = (IntPtr)xamlRoot.ContentIslandEnvironment.AppWindowId.Value;
            }

            if (hWnd != IntPtr.Zero && !_isSubclassed)
            {
                _isSubclassed = SetWindowSubclass(hWnd, _subclassProc, (UIntPtr)SUBCLASS_ID, UIntPtr.Zero);
            }

            if (hWnd != IntPtr.Zero)
            {
                ConfigureDwm(hWnd);
            }

            _brush = Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            connectedTarget.SystemBackdrop = _brush;
            AppLog.Write("TransparentTintBackdrop: successfully applied transparent brush to SystemBackdrop");

            if (hWnd != IntPtr.Zero)
            {
                IntPtr hdc = GetDC(hWnd);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        ClearBackground(hWnd, hdc);
                    }
                    finally
                    {
                        ReleaseDC(hWnd, hdc);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"TransparentTintBackdrop.OnTargetConnected failed: {ex}");
        }
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

        base.OnTargetDisconnected(disconnectedTarget);
    }

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == WM_ERASEBKGND)
        {
            if (ClearBackground(hWnd, wParam))
            {
                return (IntPtr)1;
            }
            return (IntPtr)1;
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
            int hrMargins = DwmExtendFrameIntoClientArea(hWnd, ref margins);

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
                int hrBlur = DwmEnableBlurBehindWindow(hWnd, ref bb);
                AppLog.Write($"TransparentTintBackdrop.ConfigureDwm: hWnd={hWnd}, DwmExtendFrame=0x{hrMargins:X8}, DwmEnableBlurBehind=0x{hrBlur:X8}");
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
