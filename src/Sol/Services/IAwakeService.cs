using System;

namespace Sol.Services;

public enum AwakeMode
{
    Passive,
    Indefinite,
    Timed
}

public class AwakeStateChangedEventArgs : EventArgs
{
    public AwakeMode Mode { get; }
    public bool IsActive { get; }
    public bool KeepDisplayOn { get; }
    public TimeSpan? RemainingTime { get; }

    public AwakeStateChangedEventArgs(AwakeMode mode, bool isActive, bool keepDisplayOn, TimeSpan? remainingTime)
    {
        Mode = mode;
        IsActive = isActive;
        KeepDisplayOn = keepDisplayOn;
        RemainingTime = remainingTime;
    }
}

public interface IAwakeService : IDisposable
{
    AwakeMode Mode { get; }
    bool IsActive { get; }
    bool KeepDisplayOn { get; set; }
    TimeSpan? RemainingTime { get; }

    event EventHandler<AwakeStateChangedEventArgs>? StateChanged;
    event EventHandler<TimeSpan>? TimeRemainingTick;
    event EventHandler? TimedSessionExpired;

    bool SetIndefinite(bool keepDisplayOn);
    bool SetTimed(TimeSpan duration, bool keepDisplayOn);
    bool SetPassive();
}
