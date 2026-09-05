using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Windows.Storage.Pickers;

namespace Sol.ViewModels;

public partial class FileLocksmithViewModel : ObservableObject
{
    private readonly IFileLocksmithService _locksmithService;

    public Strings S => Strings.S;

    [ObservableProperty]
    public partial string TargetPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    public partial bool HasResults { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoResultsVisibility))]
    public partial bool HasNoResults { get; set; }

    [ObservableProperty]
    public partial string ResultCountBadge { get; set; } = string.Empty;

    public Microsoft.UI.Xaml.Visibility ResultsVisibility => HasResults ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility NoResultsVisibility => HasNoResults ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility LoadingVisibility => IsLoading ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public bool IsNotLoading => !IsLoading;

    public ObservableCollection<LockingProcessInfo> LockingProcesses { get; } = [];

    public FileLocksmithViewModel(IFileLocksmithService locksmithService)
    {
        _locksmithService = locksmithService;
    }

    [RelayCommand]
    public async Task InspectLocksAsync(string? path = null)
    {
        var target = path ?? TargetPath;
        if (string.IsNullOrWhiteSpace(target))
        {
            StatusMessage = S.FileLocksmithInvalidPath;
            return;
        }

        TargetPath = target.Trim('\"');
        IsLoading = true;
        StatusMessage = string.Empty;
        HasResults = false;
        HasNoResults = false;
        LockingProcesses.Clear();

        try
        {
            var processes = await _locksmithService.FindLockingProcessesAsync(TargetPath);
            foreach (var proc in processes)
            {
                LockingProcesses.Add(proc);
            }

            HasResults = LockingProcesses.Count > 0;
            HasNoResults = LockingProcesses.Count == 0;
            ResultCountBadge = Strings.FileLocksmithLocksFound(LockingProcesses.Count);
            if (HasNoResults)
            {
                StatusMessage = S.FileLocksmithNoLocksFound;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void EndTask(LockingProcessInfo? process)
    {
        if (process == null) return;

        if (_locksmithService.KillProcess(process.ProcessId, out var error))
        {
            LockingProcesses.Remove(process);
            HasResults = LockingProcesses.Count > 0;
            HasNoResults = LockingProcesses.Count == 0;
            ResultCountBadge = Strings.FileLocksmithLocksFound(LockingProcesses.Count);
            if (HasNoResults)
            {
                StatusMessage = S.FileLocksmithNoLocksFound;
            }

            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(
                S.FileLocksmithTerminatedSuccess,
                InfoBarSeverity.Success));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(
                Strings.FileLocksmithTerminateFailed(error ?? "Unknown"),
                InfoBarSeverity.Error));
        }
    }

    [RelayCommand]
    public void EndAllTasks()
    {
        var pids = LockingProcesses.Select(p => p.ProcessId).ToList();
        if (pids.Count == 0) return;

        if (_locksmithService.KillAllProcesses(pids, out var errors))
        {
            LockingProcesses.Clear();
            HasResults = false;
            HasNoResults = true;
            StatusMessage = S.FileLocksmithNoLocksFound;
            ResultCountBadge = Strings.FileLocksmithLocksFound(0);

            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(
                S.FileLocksmithTerminatedSuccess,
                InfoBarSeverity.Success));
        }
        else
        {
            // Re-inspect to see remaining processes
            _ = InspectLocksAsync();
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(
                string.Join("\n", errors),
                InfoBarSeverity.Error));
        }
    }
}
