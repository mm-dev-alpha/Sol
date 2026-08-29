using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Sol.Views;

namespace Sol.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly Dictionary<string, Type> _pageRegistry = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<string>? Navigated;
    public string? CurrentPageKey { get; private set; }

    public NavigationService()
    {
        // Explicit PageKey -> Type registry (no reflection / string parsing)
        RegisterPage("HomePage", typeof(HomePage));
        RegisterPage("UserWorkspacePage", typeof(UserWorkspacePage));
        RegisterPage("ComputerWorkspacePage", typeof(ComputerWorkspacePage));
        RegisterPage("SettingsPage", typeof(SettingsPage));
    }

    public void RegisterPage(string pageKey, Type pageType)
    {
        _pageRegistry[pageKey] = pageType;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public bool GoBack()
    {
        if (_frame != null && _frame.CanGoBack)
        {
            _frame.GoBack();
            return true;
        }
        return false;
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        if (_frame == null)
            throw new InvalidOperationException("NavigationService must be initialized with a Frame before navigating.");

        if (!_pageRegistry.TryGetValue(pageKey, out var pageType))
            throw new ArgumentException($"PageKey '{pageKey}' is not registered in NavigationService.", nameof(pageKey));

        // Don't re-navigate to the exact same page without parameters if already there
        if (CurrentPageKey == pageKey && parameter == null)
            return false;

        var transitionInfo = new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo();
        var navigated = _frame.Navigate(pageType, parameter, transitionInfo);
        if (navigated)
        {
            CurrentPageKey = pageKey;
            Navigated?.Invoke(this, pageKey);
        }

        return navigated;
    }
}
