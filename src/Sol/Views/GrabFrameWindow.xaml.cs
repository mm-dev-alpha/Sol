using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using WinRT.Interop;

namespace Sol.Views;

/// <summary>
/// Floating, resizable viewfinder window for desktop OCR and table extraction.
/// </summary>
public sealed partial class GrabFrameWindow : Window
{
    public Strings S => Strings.S;

    public static GrabFrameWindow? CurrentInstance { get; private set; }

    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IOcrService _ocrService;
    private readonly IGrabFrameService _grabFrameService;
    private readonly IEditTextService _editTextService;
    private readonly ISettingsService _settingsService;

    private readonly IntPtr _hwnd;
    private readonly AppWindow? _appWindow;
    private OverlappedPresenter? _presenter;

    private readonly DispatcherTimer _autoOcrTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _statusPillTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    private CancellationTokenSource? _ocrCts;
    private OcrResult? _lastOcrResult;
    private OcrLanguageInfo? _selectedLanguage;

    private GrabFrameMode _currentMode = GrabFrameMode.Standard;
    private bool _isFrozen;
    private bool _isAutoOcrEnabled;
    private bool _isAlwaysOnTop = true;

    private readonly List<double> _columnDividers = new();
    private int _draggedDividerIndex = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private const uint WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x2;
    private const uint WDA_NONE = 0x00000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    public GrabFrameWindow(
        IScreenCaptureService screenCaptureService,
        IOcrService ocrService,
        IGrabFrameService grabFrameService,
        IEditTextService editTextService,
        ISettingsService settingsService)
    {
        CurrentInstance = this;
        _screenCaptureService = screenCaptureService;
        _ocrService = ocrService;
        _grabFrameService = grabFrameService;
        _editTextService = editTextService;
        _settingsService = settingsService;

        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Exclude viewfinder window from screen capture so it never captures its own UI
        SetWindowDisplayAffinity(_hwnd, WDA_EXCLUDEFROMCAPTURE);

        ExtendsContentIntoTitleBar = true;
        Title = S.ToolGrabFrameTitle;

        // Apply custom transparent backdrop
        try
        {
            this.SystemBackdrop = new TransparentTintBackdrop(this);
        }
        catch (Exception ex)
        {
            AppLog.Write($"GrabFrameWindow: TransparentTintBackdrop failed: {ex}");
        }

        ConfigureWindowFrame();
        LoadSettings();
        InitializeLanguages();

        UpdateResponsiveToolbar(650);

        _autoOcrTimer.Tick += AutoOcrTimer_Tick;
        _statusPillTimer.Tick += (s, e) =>
        {
            _statusPillTimer.Stop();
            StatusPill.Visibility = Visibility.Collapsed;
        };

        TopBarGrid.PointerPressed += TopBarGrid_PointerPressed;
        RootGrid.SizeChanged += RootGrid_SizeChanged;

        Closed += GrabFrameWindow_Closed;

        if (Content is FrameworkElement root)
        {
            root.KeyDown += Root_KeyDown;
        }

        // Trigger initial OCR capture after window loads
        DispatcherQueue.TryEnqueue(async () =>
        {
            await Task.Delay(300);
            await RunOcrAsync();
        });
    }

    private void UpdateResponsiveToolbar(double width)
    {
        if (width <= 0) return;

        var state = _grabFrameService.CalculateToolbarState(width);

        LanguageComboBox.Visibility = state.IsLanguageSelectorVisible ? Visibility.Visible : Visibility.Collapsed;
        SingleLineToggleButton.Visibility = state.AreModeButtonsVisible ? Visibility.Visible : Visibility.Collapsed;
        TableToggleButton.Visibility = state.AreModeButtonsVisible ? Visibility.Visible : Visibility.Collapsed;
        FreezeToggleButton.Visibility = state.AreSecondaryButtonsVisible ? Visibility.Visible : Visibility.Collapsed;
        RefreshButton.Visibility = state.AreSecondaryButtonsVisible ? Visibility.Visible : Visibility.Collapsed;
        MatchCountTextBlock.Visibility = state.IsMatchCountVisible ? Visibility.Visible : Visibility.Collapsed;

        GrabButtonTextBlock.Visibility = state.IsGrabTextVisible ? Visibility.Visible : Visibility.Collapsed;
        GrabActionButton.Padding = state.IsGrabTextVisible ? new Thickness(12, 0, 12, 0) : new Thickness(8, 0, 8, 0);

        WindowTitleTextBlock.Visibility = state.IsTitleTextVisible ? Visibility.Visible : Visibility.Collapsed;

        if (width < 260)
        {
            SearchTextBox.Width = double.NaN;
            SearchTextBox.MinWidth = 50;
        }
        else
        {
            SearchTextBox.Width = 140;
            SearchTextBox.MinWidth = 80;
        }
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveToolbar(e.NewSize.Width);
        OnWindowMovedOrResized();
    }

    private void ConfigureWindowFrame()
    {
        if (_appWindow != null)
        {
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                _presenter = presenter;
                presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
                presenter.IsAlwaysOnTop = _isAlwaysOnTop;
                presenter.IsResizable = true;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            try { _appWindow.IsShownInSwitchers = true; } catch { }

            _appWindow.Resize(new Windows.Graphics.SizeInt32(650, 420));
        }
    }

    private void LoadSettings()
    {
        if (_settingsService != null)
        {
            _isAutoOcrEnabled = _settingsService.GrabFrameAutoOcr;
            AutoOcrToggleButton.IsChecked = _isAutoOcrEnabled;

            _isAlwaysOnTop = _settingsService.GrabFrameAlwaysOnTop;
            AlwaysOnTopToggleButton.IsChecked = _isAlwaysOnTop;

            if (_settingsService.GrabFrameTableMode)
            {
                _currentMode = GrabFrameMode.Table;
                TableToggleButton.IsChecked = true;
                TableDividersCanvas.Visibility = Visibility.Visible;
                TableModeHintBanner.Visibility = Visibility.Visible;
            }
            else if (_settingsService.GrabFrameSingleLine)
            {
                _currentMode = GrabFrameMode.SingleLine;
                SingleLineToggleButton.IsChecked = true;
            }
        }
    }

    private void InitializeLanguages()
    {
        var languages = _ocrService.GetAvailableLanguages();
        LanguageComboBox.ItemsSource = languages;

        if (languages.Count > 0)
        {
            string prefLang = _settingsService?.GrabFrameDefaultLanguage ?? "en-US";
            var selected = languages.FirstOrDefault(l => l.LanguageTag == prefLang)
                ?? languages.FirstOrDefault(l => l.LanguageTag == _ocrService.GetDefaultLanguage().LanguageTag)
                ?? languages[0];

            LanguageComboBox.SelectedItem = selected;
            _selectedLanguage = selected;
        }
    }

    private void TopBarGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(TopBarGrid).Properties.IsLeftButtonPressed)
        {
            ReleaseCapture();
            SendMessage(_hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            OnWindowMovedOrResized();
        }
    }

    private void OnWindowMovedOrResized()
    {
        if (_isAutoOcrEnabled && !_isFrozen)
        {
            _autoOcrTimer.Stop();
            _autoOcrTimer.Start();
        }
    }

    private async void AutoOcrTimer_Tick(object? sender, object e)
    {
        _autoOcrTimer.Stop();
        if (!_isFrozen)
        {
            await RunOcrAsync();
        }
    }

    private ScreenBounds GetViewfinderScreenBounds()
    {
        var pt = new POINT { X = 0, Y = 0 };
        ClientToScreen(_hwnd, ref pt);

        uint dpi = GetDpiForWindow(_hwnd);
        double dpiScale = dpi > 0 ? dpi / 96.0 : 1.0;

        // Viewfinder is located at Row 1 of RootGrid (under the 36px TopBar)
        double localX = 0;
        double localY = TopBarGrid.ActualHeight > 0 ? TopBarGrid.ActualHeight : 36;
        double localWidth = ViewfinderBorder.ActualWidth > 0 ? ViewfinderBorder.ActualWidth : 650;
        double localHeight = ViewfinderBorder.ActualHeight > 0 ? ViewfinderBorder.ActualHeight : 336;

        var virtualScreen = _screenCaptureService.GetVirtualScreenBounds();
        return _grabFrameService.CalculateViewportScreenRect(
            pt.X, pt.Y, localX, localY, localWidth, localHeight, dpiScale, virtualScreen);
    }

    private async Task RunOcrAsync()
    {
        if (_isFrozen) return;

        _ocrCts?.Cancel();
        _ocrCts?.Dispose();
        _ocrCts = new CancellationTokenSource();
        var token = _ocrCts.Token;

        ShowStatus(S.GrabFrameStatusRecognizing, isTemporary: false);

        try
        {
            var bounds = GetViewfinderScreenBounds();
            if (bounds.Width <= 1 || bounds.Height <= 1)
            {
                return;
            }

            byte[] bmpBytes = _screenCaptureService.CaptureRegion(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            if (bmpBytes == null || bmpBytes.Length == 0)
            {
                return;
            }

            var ocrResult = await _ocrService.RecognizeAsync(bmpBytes, _selectedLanguage, token);
            if (token.IsCancellationRequested) return;

            _lastOcrResult = ocrResult;
            RenderWordBorders(ocrResult);
            ApplySearchFilter();

            HideStatus();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            AppLog.Write($"GrabFrameWindow.RunOcrAsync error: {ex}");
            HideStatus();
        }
    }

    private void RenderWordBorders(OcrResult ocrResult)
    {
        WordBordersCanvas.Children.Clear();
        if (ocrResult?.Words == null) return;

        uint dpi = GetDpiForWindow(_hwnd);
        double dpiScale = dpi > 0 ? dpi / 96.0 : 1.0;

        foreach (var word in ocrResult.Words)
        {
            // Convert physical coordinates to local XAML coordinates
            double left = word.BoundingBox.Left / dpiScale;
            double top = word.BoundingBox.Top / dpiScale;
            double width = word.BoundingBox.Width / dpiScale;
            double height = word.BoundingBox.Height / dpiScale;

            var border = new Border
            {
                Width = Math.Max(width, 4),
                Height = Math.Max(height, 4),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 120, 215)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(15, 0, 120, 215)),
                CornerRadius = new CornerRadius(2),
                Tag = word
            };

            ToolTipService.SetToolTip(border, word.Text);

            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                {
                    CopyTextToClipboard(word.Text);
                    e.Handled = true;
                }
            };

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);
            WordBordersCanvas.Children.Add(border);
        }
    }

    private void ApplySearchFilter()
    {
        string query = SearchTextBox.Text?.Trim() ?? string.Empty;
        var words = _lastOcrResult?.Words ?? Array.Empty<OcrWord>();
        var filtered = _grabFrameService.FilterWords(words, query, exactMatch: false);

        int matchCount = filtered.Count(w => w.IsMatched);

        if (string.IsNullOrEmpty(query))
        {
            MatchCountTextBlock.Text = S.GrabFrameNoMatches;
            foreach (var child in WordBordersCanvas.Children.OfType<Border>())
            {
                child.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 120, 215));
                child.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(15, 0, 120, 215));
            }
        }
        else
        {
            MatchCountTextBlock.Text = string.Format(S.GrabFrameMatchesFormat, matchCount);

            for (int i = 0; i < Math.Min(filtered.Count, WordBordersCanvas.Children.Count); i++)
            {
                if (WordBordersCanvas.Children[i] is Border border)
                {
                    bool isMatched = filtered[i].IsMatched;
                    border.BorderBrush = isMatched
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 185, 0)) // Amber glow
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128));
                    border.Background = isMatched
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 185, 0))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(5, 128, 128, 128));
                }
            }
        }
    }

    private async void GrabActionButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteGrabAsync();
    }

    private async Task ExecuteGrabAsync()
    {
        string outputText = string.Empty;

        if (_currentMode == GrabFrameMode.Table)
        {
            if (_lastOcrResult?.Words != null)
            {
                double viewportWidth = ViewfinderBorder.ActualWidth;
                var table = _grabFrameService.ParseTable(_lastOcrResult.Words, _columnDividers, viewportWidth);
                outputText = table.FormattedText;
            }
        }
        else
        {
            string query = SearchTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(query) && _lastOcrResult?.Words != null)
            {
                var matched = _grabFrameService.FilterWords(_lastOcrResult.Words, query)
                    .Where(w => w.IsMatched)
                    .Select(w => w.Text);
                outputText = string.Join(" ", matched);
            }
            else
            {
                outputText = _lastOcrResult?.Text ?? string.Empty;
            }

            outputText = _grabFrameService.FormatExtractedText(outputText, _currentMode);
        }

        if (string.IsNullOrWhiteSpace(outputText))
        {
            ShowStatus(S.GrabFrameStatusNoText, isTemporary: true);
            return;
        }

        CopyTextToClipboard(outputText);

        if (_settingsService?.GrabFrameAutoPaste == true)
        {
            await Task.Delay(100);
            await _editTextService.TryInsertTextAsync(outputText);
        }
    }

    private void CopyTextToClipboard(string text)
    {
        if (SafeClipboard.TrySetText(text))
        {
            ShowStatus(S.GrabFrameStatusCopied, isTemporary: true);
        }
        else
        {
            ShowStatus(S.ClipboardBusy, isTemporary: true);
        }
    }

    private void ShowStatus(string message, bool isTemporary)
    {
        StatusPillText.Text = message;
        StatusPill.Visibility = Visibility.Visible;

        if (isTemporary)
        {
            _statusPillTimer.Stop();
            _statusPillTimer.Start();
        }
    }

    private void HideStatus()
    {
        StatusPill.Visibility = Visibility.Collapsed;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunOcrAsync();
    }

    private void FreezeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isFrozen = FreezeToggleButton.IsChecked == true;

        if (_isFrozen)
        {
            var bounds = GetViewfinderScreenBounds();
            byte[] bmpBytes = _screenCaptureService.CaptureRegion(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            if (bmpBytes != null && bmpBytes.Length > 0)
            {
                var bitmapImage = new BitmapImage();
                using var stream = new MemoryStream(bmpBytes);
                bitmapImage.SetSource(stream.AsRandomAccessStream());
                FrozenImageView.Source = bitmapImage;
                FrozenImageView.Visibility = Visibility.Visible;
            }
        }
        else
        {
            FrozenImageView.Visibility = Visibility.Collapsed;
            FrozenImageView.Source = null;
            _ = RunOcrAsync();
        }
    }

    private void TableToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (TableToggleButton.IsChecked == true)
        {
            _currentMode = GrabFrameMode.Table;
            SingleLineToggleButton.IsChecked = false;
            TableDividersCanvas.Visibility = Visibility.Visible;
            TableModeHintBanner.Visibility = Visibility.Visible;
            EnsureDefaultDividers();
            RenderDividers();
        }
        else
        {
            _currentMode = GrabFrameMode.Standard;
            TableDividersCanvas.Visibility = Visibility.Collapsed;
            TableModeHintBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void SingleLineToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (SingleLineToggleButton.IsChecked == true)
        {
            _currentMode = GrabFrameMode.SingleLine;
            TableToggleButton.IsChecked = false;
            TableDividersCanvas.Visibility = Visibility.Collapsed;
            TableModeHintBanner.Visibility = Visibility.Collapsed;
        }
        else
        {
            _currentMode = GrabFrameMode.Standard;
        }
    }

    private void EnsureDefaultDividers()
    {
        if (_columnDividers.Count == 0 && ViewfinderBorder.ActualWidth > 200)
        {
            double half = ViewfinderBorder.ActualWidth / 2.0;
            _columnDividers.Add(half);
        }
    }

    private void RenderDividers()
    {
        TableDividersCanvas.Children.Clear();
        double height = ViewfinderBorder.ActualHeight;

        for (int i = 0; i < _columnDividers.Count; i++)
        {
            int index = i;
            double x = _columnDividers[i];

            var lineBorder = new Border
            {
                Width = 6,
                Height = height,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 120, 215)),
                CornerRadius = new CornerRadius(3),
                Tag = index
            };

            Canvas.SetLeft(lineBorder, x - 3);
            Canvas.SetTop(lineBorder, 0);
            TableDividersCanvas.Children.Add(lineBorder);
        }
    }

    private void TableDividersCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(TableDividersCanvas);
        if (point.Properties.IsLeftButtonPressed)
        {
            // Check if clicking close to an existing divider to drag
            int hitIndex = _columnDividers.FindIndex(d => Math.Abs(d - point.Position.X) <= 12);
            if (hitIndex >= 0)
            {
                _draggedDividerIndex = hitIndex;
                TableDividersCanvas.CapturePointer(e.Pointer);
                e.Handled = true;
            }
            else
            {
                // Add a new divider
                _columnDividers.Add(point.Position.X);
                _columnDividers.Sort();
                RenderDividers();
                e.Handled = true;
            }
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            // Right-click removes divider
            int hitIndex = _columnDividers.FindIndex(d => Math.Abs(d - point.Position.X) <= 16);
            if (hitIndex >= 0)
            {
                _columnDividers.RemoveAt(hitIndex);
                RenderDividers();
                e.Handled = true;
            }
        }
    }

    private void TableDividersCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedDividerIndex >= 0 && _draggedDividerIndex < _columnDividers.Count)
        {
            var point = e.GetCurrentPoint(TableDividersCanvas);
            double newX = Math.Clamp(point.Position.X, 10, ViewfinderBorder.ActualWidth - 10);
            _columnDividers[_draggedDividerIndex] = newX;
            RenderDividers();
            e.Handled = true;
        }
    }

    private void TableDividersCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedDividerIndex >= 0)
        {
            _draggedDividerIndex = -1;
            TableDividersCanvas.ReleasePointerCapture(e.Pointer);
            _columnDividers.Sort();
            RenderDividers();
            e.Handled = true;
        }
    }

    private void TableDividersCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        _draggedDividerIndex = -1;
        TableDividersCanvas.ReleasePointerCapture(e.Pointer);
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedLanguage = LanguageComboBox.SelectedItem as OcrLanguageInfo;
        if (!_isFrozen)
        {
            _ = RunOcrAsync();
        }
    }

    private void AutoOcrToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isAutoOcrEnabled = AutoOcrToggleButton.IsChecked == true;
        if (_settingsService != null)
        {
            _settingsService.GrabFrameAutoOcr = _isAutoOcrEnabled;
            _settingsService.Save();
        }
    }

    private void AlwaysOnTopToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isAlwaysOnTop = AlwaysOnTopToggleButton.IsChecked == true;
        if (_presenter != null)
        {
            _presenter.IsAlwaysOnTop = _isAlwaysOnTop;
        }
        if (_settingsService != null)
        {
            _settingsService.GrabFrameAlwaysOnTop = _isAlwaysOnTop;
            _settingsService.Save();
        }
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                Close();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Enter:
                _ = ExecuteGrabAsync();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F:
                FreezeToggleButton.IsChecked = !(FreezeToggleButton.IsChecked == true);
                FreezeToggleButton_Click(FreezeToggleButton, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.T:
                TableToggleButton.IsChecked = !(TableToggleButton.IsChecked == true);
                TableToggleButton_Click(TableToggleButton, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.S:
                SingleLineToggleButton.IsChecked = !(SingleLineToggleButton.IsChecked == true);
                SingleLineToggleButton_Click(SingleLineToggleButton, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void GrabFrameWindow_Closed(object sender, WindowEventArgs args)
    {
        AppLog.Write("GrabFrameWindow Closed event fired");
        SetWindowDisplayAffinity(_hwnd, WDA_NONE);
        _autoOcrTimer.Stop();
        _statusPillTimer.Stop();
        _ocrCts?.Cancel();
        _ocrCts?.Dispose();

        if (CurrentInstance == this)
        {
            CurrentInstance = null;
        }
    }
}
