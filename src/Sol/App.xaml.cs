using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;

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
        this.InitializeComponent();

        this.UnhandledException += (s, e) =>
        {
            e.Handled = true;
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log"), e.Exception.ToString() + "\nMessage: " + e.Message);
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

            // ViewModels
            services.AddSingleton<Sol.ViewModels.GlobalSearchViewModel>();
            services.AddSingleton<Sol.ViewModels.ShellViewModel>();
            services.AddSingleton<Sol.ViewModels.HomeViewModel>();
            services.AddSingleton<Sol.ViewModels.UserWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.ComputerWorkspaceViewModel>();
            services.AddSingleton<Sol.ViewModels.SettingsViewModel>();
        }).
        Build();
    }

    public static MainWindow? MainWindowInstance => MainWindow as MainWindow;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var settings = GetService<Sol.Services.ISettingsService>();
        Sol.Helpers.Strings.CurrentLanguage = settings.AppLanguage;

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
