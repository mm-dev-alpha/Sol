using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sol.Services;

public sealed class AwakeService : IAwakeService
{
    [Flags]
    public enum ExecutionState : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    private readonly object _lock = new();
    private Timer? _timer;
    private DateTime? _expireAt;
    private bool _disposed;

    public AwakeMode Mode { get; private set; } = AwakeMode.Passive;
    public bool IsActive => Mode != AwakeMode.Passive;
    public bool KeepDisplayOn { get; set; } = false;

    public TimeSpan? RemainingTime
    {
        get
        {
            if (Mode != AwakeMode.Timed || !_expireAt.HasValue)
                return null;

            var remaining = _expireAt.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public event EventHandler<AwakeStateChangedEventArgs>? StateChanged;
    public event EventHandler<TimeSpan>? TimeRemainingTick;
    public event EventHandler? TimedSessionExpired;

    public bool SetIndefinite(bool keepDisplayOn)
    {
        lock (_lock)
        {
            StopTimerInternal();

            Mode = AwakeMode.Indefinite;
            KeepDisplayOn = keepDisplayOn;
            _expireAt = null;

            var flags = ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED;
            if (keepDisplayOn)
            {
                flags |= ExecutionState.ES_DISPLAY_REQUIRED;
            }

            var result = ApplyExecutionState(flags);
            RaiseStateChanged();
            return result;
        }
    }

    public bool SetTimed(TimeSpan duration, bool keepDisplayOn)
    {
        if (duration <= TimeSpan.Zero)
            return SetPassive();

        lock (_lock)
        {
            StopTimerInternal();

            Mode = AwakeMode.Timed;
            KeepDisplayOn = keepDisplayOn;
            _expireAt = DateTime.UtcNow + duration;

            var flags = ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED;
            if (keepDisplayOn)
            {
                flags |= ExecutionState.ES_DISPLAY_REQUIRED;
            }

            var result = ApplyExecutionState(flags);

            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RaiseStateChanged();
            TimeRemainingTick?.Invoke(this, duration);
            return result;
        }
    }

    public bool SetPassive()
    {
        lock (_lock)
        {
            StopTimerInternal();

            Mode = AwakeMode.Passive;
            _expireAt = null;

            var result = ApplyExecutionState(ExecutionState.ES_CONTINUOUS);
            RaiseStateChanged();
            return result;
        }
    }

    private void OnTimerTick(object? state)
    {
        TimeSpan? remaining;
        bool expired = false;

        lock (_lock)
        {
            if (Mode != AwakeMode.Timed || !_expireAt.HasValue)
                return;

            remaining = RemainingTime;
            if (!remaining.HasValue || remaining.Value <= TimeSpan.Zero)
            {
                expired = true;
            }
        }

        if (expired)
        {
            SetPassive();
            TimedSessionExpired?.Invoke(this, EventArgs.Empty);
        }
        else if (remaining.HasValue)
        {
            TimeRemainingTick?.Invoke(this, remaining.Value);
        }
    }

    private void StopTimerInternal()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private static bool ApplyExecutionState(ExecutionState state)
    {
        try
        {
            var result = SetThreadExecutionState(state);
            return result != 0;
        }
        catch
        {
            return false;
        }
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new AwakeStateChangedEventArgs(Mode, IsActive, KeepDisplayOn, RemainingTime));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        SetPassive();
    }
}
