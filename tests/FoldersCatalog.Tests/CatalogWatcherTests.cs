using FoldersCatalog;

namespace FoldersCatalog.Tests;

public sealed class CatalogWatcherTests : IDisposable
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(500);

    private readonly string _root = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "cpf-watch-" + Guid.NewGuid().ToString("N")));

    private readonly SemaphoreSlim _called = new(0);
    private int _count;

    public CatalogWatcherTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        _called.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    private int Count => Volatile.Read(ref _count);

    private CatalogWatcher Watch(string pattern) =>
        new([pattern], Quiet, () =>
        {
            Interlocked.Increment(ref _count);
            _called.Release();
        });

    private string P(params string[] parts) => Path.Combine([_root, .. parts]);

    private async Task AssertCalledOnceAsync()
    {
        Assert.True(await _called.WaitAsync(Timeout), "changed was not called");
        await Task.Delay(Settle);
        Assert.Equal(1, Count);
    }

    private static async Task AssertNotCalledAsync(Func<int> count)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(0, count());
    }

    [Fact]
    public async Task FileCreated_CallsChangedOnce()
    {
        using var watcher = Watch(_root + "/**");

        File.WriteAllText(P("a.cmd"), "");

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task FileDeleted_CallsChanged()
    {
        File.WriteAllText(P("a.cmd"), "");
        using var watcher = Watch(_root + "/**");

        File.Delete(P("a.cmd"));

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task FileRenamed_CallsChanged()
    {
        File.WriteAllText(P("a.cmd"), "");
        using var watcher = Watch(_root + "/**");

        File.Move(P("a.cmd"), P("b.cmd"));

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task FileCreatedInSubfolder_RecursivePattern_CallsChanged()
    {
        Directory.CreateDirectory(P("sub"));
        using var watcher = Watch(_root + "/**");

        File.WriteAllText(P("sub", "a.cmd"), "");

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task FileCreatedInSubfolder_FlatPattern_DoesNotCall()
    {
        Directory.CreateDirectory(P("sub"));
        using var watcher = Watch(_root);

        File.WriteAllText(P("sub", "a.cmd"), "");

        await AssertNotCalledAsync(() => Count);
    }

    [Fact]
    public async Task BurstOf20Files_CallsChangedOnce()
    {
        using var watcher = Watch(_root + "/**");

        for (var i = 0; i < 20; i++)
        {
            File.WriteAllText(P($"f{i}.cmd"), "");
        }

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task MissingFolder_NoThrow_NoCall()
    {
        using var watcher = Watch(P("does-not-exist") + "/**");

        await AssertNotCalledAsync(() => Count);
    }

    [Fact]
    public async Task InvalidPattern_IsSkipped_OthersStillWatched()
    {
        using var watcher = new CatalogWatcher(["bad\0path", _root + "/**"], Quiet, () =>
        {
            Interlocked.Increment(ref _count);
            _called.Release();
        });

        File.WriteAllText(P("a.cmd"), "");

        await AssertCalledOnceAsync();
    }

    [Fact]
    public async Task Dispose_StopsCalls()
    {
        var watcher = Watch(_root + "/**");
        watcher.Dispose();

        File.WriteAllText(P("a.cmd"), "");

        await AssertNotCalledAsync(() => Count);
    }
}
