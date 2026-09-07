using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Sol.Helpers;
using Sol.Views;
using Windows.ApplicationModel.Activation;
using WinRT.Interop;

namespace Sol;

public partial class App : Application
{
    private static readonly object _crashLock = new();

    public static Window? MainWindow { get; private set; }
    
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        try
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        }
        catch { }

        this.InitializeComponent();

        // WinUI thread unhandled exceptions
        this.UnhandledException += (s, e) =>
        {
            e.Handled = true;
            LogCrash("WinUI", e.Exception, e.Message);
        };

        // AppDomain unhandled exceptions (background threads, threadpool, worker tasks)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogCrash("AppDomain", e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown AppDomain exception"));
        };

        // TaskScheduler unobserved task exceptions (async void, faulted Task background exceptions)
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            LogCrash("TaskScheduler", e.Exception);
        };
        
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logDir = Path.Combine(localAppData, "Sol", "Logs");
        Directory.CreateDirectory(logDir);
        string appLogPath = Path.Combine(logDir, "app.log");

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        UseSerilog((ctx, lc) =>
        {
            lc.MinimumLevel.Information()
              .Enrich.FromLogContext()
              .WriteTo.File(appLogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
        }).
        ConfigureServices((context, services) =>
        {
            // Caching
            services.AddMemoryCache();

            // Services
            services.AddSingleton<Sol.Services.ISettingsService, Sol.Services.SettingsService>();
            services.AddSingleton<Sol.Services.IActiveDirectoryService, Sol.Services.ActiveDirectoryService>();
            services.AddSingleton<Sol.Services.ISearchService, Sol.Services.SearchService>();
            services.AddSingleton<Sol.Services.IGreetingService, Sol.Services.GreetingService>();
            services.AddSingleton<Sol.Services.INavigationService, Sol.Services.NavigationService>();
            services.AddSingleton<Sol.Services.IExportService, Sol.Services.ExportService>();
            services.AddSingleton<Sol.Services.IHardwareDiagnosticService, Sol.Services.HardwareDiagnosticService>();
            services.AddSingleton<Sol.Services.IProcessManagementService, Sol.Services.ProcessManagementService>();
            services.AddSingleton<Sol.Services.IBitLockerManagementService, Sol.Services.BitLockerManagementService>();
            services.AddSingleton<Sol.Services.IComputerDiagnosticService, Sol.Services.ComputerDiagnosticService>();
            services.AddSingleton<Sol.Services.IJiraService, Sol.Services.JiraService>();
            services.AddSingleton<Sol.Services.IFileLocksmithService, Sol.Services.FileLocksmithService>();
            services.AddSingleton<Sol.Services.IShortcutGuideService, Sol.Services.ShortcutGuideService>();
            services.AddSingleton<Sol.Services.IOcrService, Sol.Services.OcrService>();
            services.AddSingleton<Sol.Services.ITextTransformService, Sol.Services.TextTransformService>();
            services.AddSingleton<Sol.Services.IScreenCaptureService, Sol.Services.ScreenCaptureService>();
            services.AddSingleton<Sol.Services.IGrabFrameService, Sol.Services.GrabFrameService>();
            services.AddSingleton<Sol.Services.IEditTextService, Sol.Services.EditTextService>();
            services.AddSingleton<Sol.Services.IMmcLookupService, Sol.Services.MmcLookupService>();
            services.AddSingleton<Sol.Services.IAdminCommandService, Sol.Services.AdminCommandService>();
            services.AddSingleton<Sol.Services.IEntityComparisonService, Sol.Services.EntityComparisonService>();
            services.AddSingleton<Sol.Services.IGlobalHotkeyService, Sol.Services.GlobalHotkeyService>();

            // ViewModels
            services.AddSingleton<Sol.ViewModels.GlobalSearchViewModel>();
            services.AddSingleton<Sol.ViewModels.ShellViewModel>();
            services.AddSingleton<Sol.ViewModels.HomeViewModel>();
            services.AddTransient<Sol.ViewModels.UserWorkspaceViewModel>();
            services.AddTransient<Sol.ViewModels.ComputerWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.CompareWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.JiraWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.SettingsViewModel>();
            services.AddSingleton<Sol.ViewModels.ToolsViewModel>();
            services.AddTransient<Sol.ViewModels.FileLocksmithViewModel>();
        }).
        Build();
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    public static MainWindow? MainWindowInstance => MainWindow as MainWindow;
    public static Microsoft.UI.Dispatching.DispatcherQueue? MainDispatcherQueue { get; private set; }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        }
        catch { }

        Strings.CurrentLanguage = "en";

        MainWindow = new MainWindow();
        MainDispatcherQueue = MainWindow.DispatcherQueue;
        MainWindow.Activate();

        try
        {
            var argsList = Environment.GetCommandLineArgs();
            var unlockPath = CommandLineHelper.TryGetUnlockPath(argsList);
            if (!string.IsNullOrWhiteSpace(unlockPath))
            {
                OpenOrActivateLocksmith(unlockPath);
            }
            else if (argsList.Any(a => string.Equals(a, "--grab-frame", StringComparison.OrdinalIgnoreCase)))
            {
                GetService<ViewModels.ToolsViewModel>().LaunchGrabFrame();
            }
        }
        catch { }
    }

    public static void OpenOrActivateLocksmith(string targetPath)
    {
        var existing = FileLocksmithWindow.CurrentActiveInstance;
        if (existing != null)
        {
            existing.InspectPath(targetPath);
            existing.BringToForeground();
        }
        else
        {
            var vm = GetService<Sol.ViewModels.FileLocksmithViewModel>();
            var win = new FileLocksmithWindow(vm, targetPath);
            win.Activate();
            win.BringToForeground();
        }
    }

    public static void RestoreMainWindow()
    {
        if (MainWindow != null)
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(MainWindow);
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
            }
            catch { }
        }
    }

    internal static void HandleActivation(Microsoft.Windows.AppLifecycle.AppActivationArguments args)
    {
        var dispatcher = MainDispatcherQueue ?? MainWindowInstance?.DispatcherQueue;
        if (dispatcher == null) return;

        dispatcher.TryEnqueue(() =>
        {
            try
            {
                string? unlockPath = null;

                if (args.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Launch)
                {
                    if (args.Data is ILaunchActivatedEventArgs launchArgs)
                    {
                        unlockPath = CommandLineHelper.TryGetUnlockPath(launchArgs.Arguments);
                    }
                }
                else if (args.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
                {
                    if (args.Data is IFileActivatedEventArgs fileArgs)
                    {
                        unlockPath = fileArgs.Files?.FirstOrDefault()?.Path;
                    }
                }

                if (!string.IsNullOrWhiteSpace(unlockPath))
                {
                    OpenOrActivateLocksmith(unlockPath);
                }
                else if (args.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Launch
                    && args.Data is ILaunchActivatedEventArgs launchArgs
                    && launchArgs.Arguments?.Contains("--grab-frame", StringComparison.OrdinalIgnoreCase) == true)
                {
                    GetService<ViewModels.ToolsViewModel>().LaunchGrabFrame();
                }
                else
                {
                    RestoreMainWindow();
                }
            }
            catch { }
        });
    }

    private static void LogCrash(string source, Exception? ex, string? extraMessage = null)
    {
        lock (_crashLock)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = Path.Combine(localAppData, "Sol");
                Directory.CreateDirectory(logDir);
                string crashLogPath = Path.Combine(logDir, "crash.log");

                // Bounded log rotation: if > 5MB, rotate to crash.log.1
                var fileInfo = new FileInfo(crashLogPath);
                if (fileInfo.Exists && fileInfo.Length > 5 * 1024 * 1024)
                {
                    string rotatedPath = Path.Combine(logDir, "crash.log.1");
                    File.Copy(crashLogPath, rotatedPath, overwrite: true);
                    File.Delete(crashLogPath);
                }

                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"[CRASH {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Source: {source}");
                if (!string.IsNullOrWhiteSpace(extraMessage))
                {
                    sb.AppendLine($"Message: {extraMessage}");
                }
                if (ex != null)
                {
                    sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
                    sb.AppendLine($"Exception Message: {ex.Message}");
                    sb.AppendLine($"Stack Trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine($"Inner Exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                        sb.AppendLine($"Inner Stack Trace:\n{ex.InnerException.StackTrace}");
                    }
                }
                sb.AppendLine();

                File.AppendAllText(crashLogPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
