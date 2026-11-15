using FoldersCatalog;
using Microsoft.Extensions.Time.Testing;

namespace FoldersCatalog.Tests;

public sealed class DebouncerTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(500);

    private readonly FakeTimeProvider _time = new();
    private int _count;

    private Debouncer Create() => new(Quiet, _time, () => Interlocked.Increment(ref _count));

    private int Count => Volatile.Read(ref _count);

    [Fact]
    public void NoSignal_NeverRuns()
    {
        using var debouncer = Create();

        _time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0, Count);
    }

    [Fact]
    public void OneSignal_RunsOnceAfterQuiet()
    {
        using var debouncer = Create();

        debouncer.Signal();
        _time.Advance(TimeSpan.FromMilliseconds(499));
        Assert.Equal(0, Count);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, Count);

        _time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, Count);
    }

    [Fact]
    public void Burst_RunsOnceAfterTheLastSignal()
    {
        using var debouncer = Create();

        for (var i = 0; i < 20; i++)
        {
            debouncer.Signal();
            _time.Advance(TimeSpan.FromMilliseconds(100));
            Assert.Equal(0, Count);
        }

        // 100 ms have passed since the last signal; 400 ms more complete the quiet time.
        _time.Advance(TimeSpan.FromMilliseconds(399));
        Assert.Equal(0, Count);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, Count);
    }

    [Fact]
    public void SignalAfterRun_RunsAgain()
    {
        using var debouncer = Create();

        debouncer.Signal();
        _time.Advance(Quiet);
        debouncer.Signal();
        _time.Advance(Quiet);

        Assert.Equal(2, Count);
    }

    [Fact]
    public void Dispose_CancelsAPendingRun()
    {
        var debouncer = Create();

        debouncer.Signal();
        debouncer.Dispose();
        _time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(0, Count);

        debouncer.Signal();
        _time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(0, Count);
    }

    [Fact]
    public async Task Dispose_WaitsForARunningAction()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var debouncer = new Debouncer(Quiet, _time, () =>
        {
            started.Set();
            release.Wait(TimeSpan.FromSeconds(10));
        });

        debouncer.Signal();
        var advance = Task.Run(() => _time.Advance(Quiet));
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)), "the action did not start");

        var dispose = Task.Run(debouncer.Dispose);
        await Task.WhenAny(dispose, Task.Delay(TimeSpan.FromMilliseconds(200)));
        var returnedEarly = dispose.IsCompleted;

        release.Set();
        Assert.False(returnedEarly, "Dispose returned while the action was still running");
        await dispose.WaitAsync(TimeSpan.FromSeconds(5));
        await advance.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
