using System;
using Microsoft.UI.Xaml.Controls;

namespace Sol.Services;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool NavigateTo(string pageKey, object? parameter = null);
    bool GoBack();
    bool CanGoBack { get; }
    string? CurrentPageKey { get; }
    event EventHandler<string>? Navigated;
}
