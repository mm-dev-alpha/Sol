using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

namespace Sol;

public static class Program
{
    private const string AppKey = "Sol_SingleInstance_App";

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private const int ASFW_ANY = -1;

    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var mainInstance = AppInstance.FindOrRegisterForKey(AppKey);

        if (!mainInstance.IsCurrent)
        {
            try
            {
                AllowSetForegroundWindow(ASFW_ANY);

                var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                var redirectSemaphore = new Semaphore(0, 1);
                Task.Run(() =>
                {
                    try
                    {
                        mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().Wait();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SingleInstance] Redirection task failed: {ex.Message}");
                    }
                    finally
                    {
                        redirectSemaphore.Release();
                    }
                });

                redirectSemaphore.WaitOne(3000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SingleInstance] Redirection failed: {ex.Message}");
            }
            return;
        }

        mainInstance.Activated += OnAppActivated;

        Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void OnAppActivated(object? sender, AppActivationArguments args)
    {
        App.HandleActivation(args);
    }
}
