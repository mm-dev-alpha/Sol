using System;
using System.Threading;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class AwakeServiceTests
{
    [Fact]
    public void AwakeService_InitialState_IsPassive()
    {
        using var service = new AwakeService();

        Assert.Equal(AwakeMode.Passive, service.Mode);
        Assert.False(service.IsActive);
        Assert.False(service.KeepDisplayOn);
        Assert.Null(service.RemainingTime);
    }

    [Fact]
    public void SetIndefinite_TransitionsToActiveIndefinite()
    {
        using var service = new AwakeService();
        bool stateChangedFired = false;
        AwakeStateChangedEventArgs? eventArgs = null;

        service.StateChanged += (s, e) =>
        {
            stateChangedFired = true;
            eventArgs = e;
        };

        var result = service.SetIndefinite(keepDisplayOn: true);

        Assert.True(result);
        Assert.Equal(AwakeMode.Indefinite, service.Mode);
        Assert.True(service.IsActive);
        Assert.True(service.KeepDisplayOn);
        Assert.Null(service.RemainingTime);

        Assert.True(stateChangedFired);
        Assert.NotNull(eventArgs);
        Assert.Equal(AwakeMode.Indefinite, eventArgs.Mode);
        Assert.True(eventArgs.IsActive);
        Assert.True(eventArgs.KeepDisplayOn);
    }

    [Fact]
    public void SetTimed_TransitionsToTimed_WithValidRemainingTime()
    {
        using var service = new AwakeService();
        var duration = TimeSpan.FromMinutes(30);

        var result = service.SetTimed(duration, keepDisplayOn: false);

        Assert.True(result);
        Assert.Equal(AwakeMode.Timed, service.Mode);
        Assert.True(service.IsActive);
        Assert.False(service.KeepDisplayOn);
        Assert.NotNull(service.RemainingTime);
        Assert.True(service.RemainingTime.Value <= duration);
        Assert.True(service.RemainingTime.Value > TimeSpan.FromMinutes(29));
    }

    [Fact]
    public void SetPassive_RevertsToPassiveState()
    {
        using var service = new AwakeService();
        service.SetIndefinite(keepDisplayOn: true);

        var result = service.SetPassive();

        Assert.True(result);
        Assert.Equal(AwakeMode.Passive, service.Mode);
        Assert.False(service.IsActive);
        Assert.Null(service.RemainingTime);
    }

    [Fact]
    public async Task TimedSession_ExpiresAndRevertsToPassive()
    {
        using var service = new AwakeService();
        var expiredTcs = new TaskCompletionSource<bool>();

        service.TimedSessionExpired += (s, e) =>
        {
            expiredTcs.TrySetResult(true);
        };

        // Start 1-second session
        service.SetTimed(TimeSpan.FromSeconds(1), keepDisplayOn: true);

        // Wait up to 3 seconds for expiration
        var completed = await Task.WhenAny(expiredTcs.Task, Task.Delay(3000));

        Assert.Equal(expiredTcs.Task, completed);
        Assert.Equal(AwakeMode.Passive, service.Mode);
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Dispose_ResetsToPassive()
    {
        var service = new AwakeService();
        service.SetIndefinite(keepDisplayOn: true);

        service.Dispose();

        Assert.Equal(AwakeMode.Passive, service.Mode);
        Assert.False(service.IsActive);
    }
}
