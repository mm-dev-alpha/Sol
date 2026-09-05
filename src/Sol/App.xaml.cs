using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Sol.Helpers;
using Sol.Views;
using Windows.ApplicationModel.Activation;
using WinRT.Interop;

namespace Sol;

public partial class App : Application
{
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

        this.UnhandledException += (s, e) =>
        {
            e.Handled = true;
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = System.IO.Path.Combine(localAppData, "Sol");
                System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "crash.log"), e.Exception?.ToString() + "\nMessage: " + e.Message);
            }
            catch { }
        };
        
        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
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
            services.AddSingleton<Sol.Services.IComputerDiagnosticService, Sol.Services.ComputerDiagnosticService>();
            services.AddSingleton<Sol.Services.IJiraService, Sol.Services.JiraService>();
            services.AddSingleton<Sol.Services.IAwakeService, Sol.Services.AwakeService>();
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

            // ViewModels
            services.AddSingleton<Sol.ViewModels.GlobalSearchViewModel>();
            services.AddSingleton<Sol.ViewModels.ShellViewModel>();
            services.AddSingleton<Sol.ViewModels.HomeViewModel>();
            services.AddSingleton<Sol.ViewModels.UserWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.ComputerWorkspaceViewModel>();
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
            var unlockPath = CommandLineHelper.TryGetUnlockPath(Environment.GetCommandLineArgs());
            if (!string.IsNullOrWhiteSpace(unlockPath))
            {
                OpenOrActivateLocksmith(unlockPath);
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
                else
                {
                    RestoreMainWindow();
                }
            }
            catch { }
        });
    }
}
