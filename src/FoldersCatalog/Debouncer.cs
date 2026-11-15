namespace FoldersCatalog;

/// <summary>
/// Turns a burst of signals into one call of an action: the action runs once, on a timer
/// thread, after no signal has come for the quiet time.
/// </summary>
public sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _quiet;
    private readonly Action _action;
    private readonly ITimer _timer;
    private readonly object _gate = new();
    private bool _disposed;

    public Debouncer(TimeSpan quiet, TimeProvider time, Action action)
    {
        _quiet = quiet;
        _action = action;
        _timer = time.CreateTimer(OnElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>(Re)starts the quiet period. Does nothing after <see cref="Dispose"/>.</summary>
    public void Signal()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _timer.Change(_quiet, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Cancels a pending run and waits for a running action to finish; no action call happens
    /// after this returns. The action must not wait on another thread that calls into the
    /// debouncer. The action may call <see cref="Signal"/> or <see cref="Dispose"/> on its own thread.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
        }
    }

    private void OnElapsed(object? state)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _action();
        }
    }
}
