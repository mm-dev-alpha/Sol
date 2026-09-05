using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace Sol.Views;

/// <summary>
/// Dedicated utility window for text editing, table formatting, calculation, and OCR pasting.
/// </summary>
public sealed partial class EditTextWindow : Window
{
    public Strings S => Strings.S;

    public static EditTextWindow? CurrentInstance { get; private set; }

    private readonly IEditTextService _editTextService;
    private readonly ITextTransformService _textTransformService;
    private readonly IOcrService _ocrService;
    private readonly ISettingsService _settingsService;

    private readonly IntPtr _hwnd;
    private readonly AppWindow? _appWindow;
    private OverlappedPresenter? _presenter;

    private readonly DispatcherTimer _statusPillTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };

    private bool _isAlwaysOnTop;
    private bool _isWordWrap;
    private bool _isCalcPaneVisible;
    private bool _isTableMode;

    private string? _currentFilePath;
    private EditTextTableDocument? _currentTable;

    public EditTextWindow(
        IEditTextService editTextService,
        ITextTransformService textTransformService,
        IOcrService ocrService,
        ISettingsService settingsService,
        string? initialText = null,
        EditTextTableDocument? initialTable = null)
    {
        CurrentInstance = this;
        _editTextService = editTextService;
        _textTransformService = textTransformService;
        _ocrService = ocrService;
        _settingsService = settingsService;

        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            _presenter = _appWindow.Presenter as OverlappedPresenter;
            if (_presenter != null)
            {
                _presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
                _presenter.IsResizable = true;
                _presenter.IsMinimizable = false;
                _presenter.IsMaximizable = false;
            }
            _appWindow.Resize(new Windows.Graphics.SizeInt32(880, 580));
        }

        _statusPillTimer.Tick += (s, e) =>
        {
            _statusPillTimer.Stop();
            StatusPill.Visibility = Visibility.Collapsed;
        };

        // Initialize user preferences from ISettingsService
        _isWordWrap = _settingsService.EditTextWindowWordWrap;
        WordWrapToggle.IsChecked = _isWordWrap;
        TextEditor.TextWrapping = _isWordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

        _isAlwaysOnTop = _settingsService.EditTextWindowAlwaysOnTop;
        AlwaysOnTopToggle.IsChecked = _isAlwaysOnTop;
        ApplyAlwaysOnTop(_isAlwaysOnTop);

        this.Closed += EditTextWindow_Closed;

        LoadContent(initialText, initialTable);
    }

    public void LoadContent(string? text = null, EditTextTableDocument? table = null)
    {
        if (table != null)
        {
            _currentTable = table;
            _isTableMode = true;
            TableModeToggle.IsChecked = true;
            SwitchToTableMode(true);
        }
        else if (!string.IsNullOrEmpty(text))
        {
            TextEditor.Text = text;
            _currentTable = EditTextTableDocument.CreateFromText(text);
            SwitchToTableMode(false);
        }
        else
        {
            TextEditor.Text = string.Empty;
            _currentTable = EditTextTableDocument.CreateFromText(string.Empty);
            SwitchToTableMode(false);
        }

        UpdateTextStats();
    }

    private void ApplyAlwaysOnTop(bool alwaysOnTop)
    {
        if (_presenter != null)
        {
            _presenter.IsAlwaysOnTop = alwaysOnTop;
        }
    }

    private void ShowStatusPill(string message)
    {
        StatusPillText.Text = message;
        StatusPill.Visibility = Visibility.Visible;
        _statusPillTimer.Stop();
        _statusPillTimer.Start();
    }

    private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTextStats();

        if (_isCalcPaneVisible)
        {
            UpdateCalculationResults();
        }
    }

    private void UpdateTextStats()
    {
        string text = TextEditor.Text ?? string.Empty;
        int charCount = text.Length;
        int lineCount = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
        int wordCount = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        TextStatsTextBlock.Text = string.Format(S.EditTextStatusStatsFormat, lineCount, wordCount, charCount);
    }

    private void UpdateCalculationResults()
    {
        var calcResult = _editTextService.EvaluateExpressions(TextEditor.Text);
        CalcResultsEditor.Text = string.Join(Environment.NewLine, calcResult.LineOutputs);

        CalcStatsTextBlock.Text = string.Format(
            S.EditTextStatusCalcSummaryFormat,
            calcResult.Sum.ToString("G6"),
            calcResult.Average.ToString("G6"),
            calcResult.EvaluatedCount);
        CalcStatsTextBlock.Visibility = Visibility.Visible;
    }

    #region Window Controls

    private void WordWrapToggle_Click(object sender, RoutedEventArgs e)
    {
        _isWordWrap = WordWrapToggle.IsChecked ?? false;
        TextEditor.TextWrapping = _isWordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private void CalcPaneToggle_Click(object sender, RoutedEventArgs e)
    {
        _isCalcPaneVisible = CalcPaneToggle.IsChecked ?? false;
        CalcResultsPane.Visibility = _isCalcPaneVisible ? Visibility.Visible : Visibility.Collapsed;
        CalcStatsTextBlock.Visibility = _isCalcPaneVisible ? Visibility.Visible : Visibility.Collapsed;

        if (_isCalcPaneVisible)
        {
            UpdateCalculationResults();
        }
    }

    private void TableModeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isTableMode = TableModeToggle.IsChecked ?? false;
        SwitchToTableMode(_isTableMode);
    }

    private void SwitchToTableMode(bool enableTableMode)
    {
        _isTableMode = enableTableMode;
        TableModeToggle.IsChecked = enableTableMode;

        if (enableTableMode)
        {
            _currentTable ??= EditTextTableDocument.CreateFromText(TextEditor.Text);
            RenderTableGrid();
            TextModeContainer.Visibility = Visibility.Collapsed;
            TableModeContainer.Visibility = Visibility.Visible;
        }
        else
        {
            if (_currentTable != null)
            {
                TextEditor.Text = _currentTable.SerializeToText();
            }
            TableModeContainer.Visibility = Visibility.Collapsed;
            TextModeContainer.Visibility = Visibility.Visible;
        }
    }

    private void AlwaysOnTopToggle_Click(object sender, RoutedEventArgs e)
    {
        _isAlwaysOnTop = AlwaysOnTopToggle.IsChecked ?? false;
        ApplyAlwaysOnTop(_isAlwaysOnTop);
    }

    #endregion

    #region File Menu

    private void MenuFileNew_Click(object sender, RoutedEventArgs e)
    {
        TextEditor.Text = string.Empty;
        _currentFilePath = null;
        _currentTable = EditTextTableDocument.CreateFromText(string.Empty);
        if (_isTableMode)
        {
            RenderTableGrid();
        }
        ShowStatusPill(S.EditTextMenuNew);
    }

    private async void MenuFileOpen_Click(object sender, RoutedEventArgs e)
    {
        await OpenFileAsync();
    }

    private async void MenuFileSave_Click(object sender, RoutedEventArgs e)
    {
        await SaveFileAsync(false);
    }

    private async void MenuFileSaveAs_Click(object sender, RoutedEventArgs e)
    {
        await SaveFileAsync(true);
    }

    private async Task OpenFileAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".tsv");
            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, _hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string content = await Windows.Storage.FileIO.ReadTextAsync(file);
                _currentFilePath = file.Path;
                LoadContent(content);
                ShowStatusPill(S.EditTextStatusOpened);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"EditTextWindow.OpenFileAsync error: {ex}");
        }
    }

    private async Task SaveFileAsync(bool saveAs)
    {
        try
        {
            string contentToSave = _isTableMode
                ? (_currentTable?.SerializeToText() ?? TextEditor.Text)
                : TextEditor.Text;

            if (string.IsNullOrEmpty(_currentFilePath) || saveAs)
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.FileTypeChoices.Add("Text Document", new List<string> { ".txt" });
                picker.FileTypeChoices.Add("CSV Spreadsheet", new List<string> { ".csv" });
                picker.FileTypeChoices.Add("TSV Document", new List<string> { ".tsv" });
                picker.FileTypeChoices.Add("Markdown Document", new List<string> { ".md" });
                picker.SuggestedFileName = "Document";
                InitializeWithWindow.Initialize(picker, _hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file == null) return;
                _currentFilePath = file.Path;
            }

            await File.WriteAllTextAsync(_currentFilePath, contentToSave, Encoding.UTF8);
            ShowStatusPill(S.EditTextStatusSaved);
        }
        catch (Exception ex)
        {
            AppLog.Write($"EditTextWindow.SaveFileAsync error: {ex}");
        }
    }

    private void MenuFileCopyClose_Click(object sender, RoutedEventArgs e)
    {
        CopyActiveContentToClipboard();
        this.Close();
    }

    private async void MenuFileCloseInsert_Click(object sender, RoutedEventArgs e)
    {
        string text = GetActiveContent();
        this.Close();
        await _editTextService.TryInsertTextAsync(text);
    }

    private void MenuFileClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    #endregion

    #region Edit & Transform Menu

    private void MenuEditUndo_Click(object sender, RoutedEventArgs e)
    {
        // TextBox native undo via automation or key stroke
    }

    private void MenuEditRedo_Click(object sender, RoutedEventArgs e)
    {
        // TextBox native redo
    }

    private void MenuEditCut_Click(object sender, RoutedEventArgs e)
    {
        if (TextEditor.SelectionLength > 0)
        {
            var package = new DataPackage();
            package.SetText(TextEditor.SelectedText);
            Clipboard.SetContent(package);
            InsertTextAtCursor(string.Empty);
        }
    }

    private void MenuEditCopy_Click(object sender, RoutedEventArgs e)
    {
        CopyActiveContentToClipboard();
        ShowStatusPill(S.EditTextStatusCopied);
    }

    private async void MenuEditPaste_Click(object sender, RoutedEventArgs e)
    {
        var data = Clipboard.GetContent();
        if (data.Contains(StandardDataFormats.Text))
        {
            string text = await data.GetTextAsync();
            InsertTextAtCursor(text);
        }
    }

    private async void MenuEditOcrPaste_Click(object sender, RoutedEventArgs e)
    {
        await DoOcrPasteAsync();
    }

    private async Task DoOcrPasteAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                var streamRef = await dataPackageView.GetBitmapAsync();
                using var stream = await streamRef.OpenReadAsync();
                using var memStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memStream);
                byte[] imageBytes = memStream.ToArray();

                ShowStatusPill(S.OcrProcessing);
                var result = await _ocrService.RecognizeAsync(imageBytes);
                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                {
                    InsertTextAtCursor(result.Text);
                    ShowStatusPill(S.EditTextStatusOcrSuccess);
                }
                else
                {
                    ShowStatusPill(S.EditTextStatusOcrEmpty);
                }
            }
            else
            {
                ShowStatusPill(S.EditTextStatusOcrEmpty);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"EditTextWindow.DoOcrPasteAsync exception: {ex}");
            ShowStatusPill(S.EditTextStatusOcrError);
        }
    }

    private void MenuEditSelectAll_Click(object sender, RoutedEventArgs e)
    {
        TextEditor.SelectAll();
    }

    private void MenuEditClear_Click(object sender, RoutedEventArgs e)
    {
        TextEditor.Text = string.Empty;
    }

    private void InsertTextAtCursor(string textToInsert)
    {
        int selStart = TextEditor.SelectionStart;
        int selLen = TextEditor.SelectionLength;
        string current = TextEditor.Text ?? string.Empty;

        if (selLen > 0)
        {
            TextEditor.Text = current.Remove(selStart, selLen).Insert(selStart, textToInsert);
        }
        else
        {
            TextEditor.Text = current.Insert(selStart, textToInsert);
        }
        TextEditor.SelectionStart = selStart + textToInsert.Length;
        TextEditor.SelectionLength = 0;
    }

    private void ApplyTransformation(Func<string, string> transformFunc)
    {
        if (string.IsNullOrEmpty(TextEditor.Text)) return;

        int selStart = TextEditor.SelectionStart;
        int selLen = TextEditor.SelectionLength;

        if (selLen > 0)
        {
            string selected = TextEditor.SelectedText;
            string transformed = transformFunc(selected);
            TextEditor.Text = TextEditor.Text.Remove(selStart, selLen).Insert(selStart, transformed);
            TextEditor.SelectionStart = selStart;
            TextEditor.SelectionLength = transformed.Length;
        }
        else
        {
            string full = TextEditor.Text;
            string transformed = transformFunc(full);
            TextEditor.Text = transformed;
            TextEditor.SelectionStart = Math.Min(selStart, transformed.Length);
            TextEditor.SelectionLength = 0;
        }
    }

    private void TransformSingleLine_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.MakeSingleLine(t));

    private void TransformTrim_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.TrimEachLine(t));

    private void TransformMakeNumbers_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.TryFixToNumbers(t));

    private void TransformMakeLetters_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.TryFixToLetters(t));

    private void TransformFixGuid_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.CorrectCommonGuidErrors(t));

    private void TransformToggleCase_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ToggleCase(t));

    private void TransformRemoveDuplicates_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.RemoveDuplicateLines(t));

    private void TransformShuffle_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ShuffleLines(t));

    private void TransformReplaceReserved_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ReplaceReservedCharacters(t));

    private void TransformUnstack_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.UnstackToColumns(t, 2));

    private void TransformSortAsc_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.SortLines(t, false));

    private void TransformSortDesc_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.SortLines(t, true));

    private void TransformExtractEmails_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ExtractEmails(t));

    private void TransformExtractUrls_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ExtractUrls(t));

    private void TransformExtractNumbers_Click(object sender, RoutedEventArgs e) =>
        ApplyTransformation(t => _textTransformService.ExtractNumbers(t));

    #endregion

    #region Table Operations

    private void TableTranspose_Click(object sender, RoutedEventArgs e)
    {
        _currentTable?.Transpose();
        if (_isTableMode)
        {
            RenderTableGrid();
        }
        else if (_currentTable != null)
        {
            TextEditor.Text = _currentTable.SerializeToText();
        }
    }

    private void TableAddRow_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null)
        {
            _currentTable.InsertRow(_currentTable.RowCount);
            RenderTableGrid();
        }
    }

    private void TableAddColumn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null)
        {
            _currentTable.InsertColumn(_currentTable.ColumnCount);
            RenderTableGrid();
        }
    }

    private void TableDeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null && _currentTable.RowCount > 1)
        {
            _currentTable.DeleteRow(_currentTable.RowCount - 1);
            RenderTableGrid();
        }
    }

    private void TableDeleteColumn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null && _currentTable.ColumnCount > 1)
        {
            _currentTable.DeleteColumn(_currentTable.ColumnCount - 1);
            RenderTableGrid();
        }
    }

    private void TableCopyTsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null)
        {
            var pkg = new DataPackage();
            pkg.SetText(_currentTable.SerializeToText());
            Clipboard.SetContent(pkg);
            ShowStatusPill(S.EditTextStatusCopied);
        }
    }

    private void TableCopyCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null)
        {
            var oldFormat = _currentTable.Format;
            _currentTable.Format = EtwStructuredTextFormat.Csv;
            string csv = _currentTable.SerializeToText();
            _currentTable.Format = oldFormat;

            var pkg = new DataPackage();
            pkg.SetText(csv);
            Clipboard.SetContent(pkg);
            ShowStatusPill(S.EditTextStatusCopied);
        }
    }

    private void TableCopyMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTable != null)
        {
            string md = _currentTable.SerializeToMarkdown();
            var pkg = new DataPackage();
            pkg.SetText(md);
            Clipboard.SetContent(pkg);
            ShowStatusPill(S.EditTextStatusCopied);
        }
    }

    private void RenderTableGrid()
    {
        TableContainerPanel.Children.Clear();
        if (_currentTable == null) return;

        // Render Column Headers Row
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        for (int c = 0; c < _currentTable.ColumnCount; c++)
        {
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            var headerBox = new Border
            {
                Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(2)
            };
            string colName = c < _currentTable.ColumnNames.Count ? _currentTable.ColumnNames[c] : EditTextTableDocument.GetSpreadsheetColumnLabel(c);
            headerBox.Child = new TextBlock
            {
                Text = colName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(headerBox, c);
            headerGrid.Children.Add(headerBox);
        }
        TableContainerPanel.Children.Add(headerGrid);

        // Render Data Rows
        for (int r = 0; r < _currentTable.RowCount; r++)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            for (int c = 0; c < _currentTable.ColumnCount; c++)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                int rowIdx = r;
                int colIdx = c;
                string cellValue = (rowIdx < _currentTable.Rows.Count && colIdx < _currentTable.Rows[rowIdx].Count)
                    ? _currentTable.Rows[rowIdx][colIdx] ?? string.Empty
                    : string.Empty;

                var cellBox = new TextBox
                {
                    Text = cellValue,
                    FontSize = 13,
                    FontFamily = new FontFamily("Consolas, Cascadia Code, Segoe UI"),
                    Margin = new Thickness(2),
                    Padding = new Thickness(4, 2, 4, 2)
                };
                cellBox.TextChanged += (s, e) =>
                {
                    if (rowIdx < _currentTable.Rows.Count && colIdx < _currentTable.Rows[rowIdx].Count)
                    {
                        _currentTable.Rows[rowIdx][colIdx] = cellBox.Text;
                    }
                };
                Grid.SetColumn(cellBox, c);
                rowGrid.Children.Add(cellBox);
            }
            TableContainerPanel.Children.Add(rowGrid);
        }
    }

    #endregion

    private string GetActiveContent()
    {
        if (_isTableMode && _currentTable != null)
        {
            return _currentTable.SerializeToText();
        }
        return TextEditor.Text ?? string.Empty;
    }

    private void CopyActiveContentToClipboard()
    {
        string text = GetActiveContent();
        var pkg = new DataPackage();
        pkg.SetText(text);
        Clipboard.SetContent(pkg);
    }

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private void AppTitleBar_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(AppTitleBar).Properties.IsLeftButtonPressed)
        {
            ReleaseCapture();
            SendMessage(_hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void EditTextWindow_Closed(object sender, WindowEventArgs args)
    {
        _statusPillTimer.Stop();
        if (CurrentInstance == this)
        {
            CurrentInstance = null;
        }
    }
}
