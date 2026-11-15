namespace FoldersCatalog;

/// <summary>
/// Watches the catalogued pattern folders and calls an action once per burst of changes
/// (a file or folder created, deleted or renamed), through a <see cref="Debouncer"/>.
/// </summary>
public sealed class CatalogWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Debouncer _debouncer;

    /// <summary>
    /// Starts one watcher per pattern whose folder exists. Blank patterns, missing folders and
    /// folders that cannot be watched are skipped.
    /// </summary>
    public CatalogWatcher(IEnumerable<string> patterns, TimeSpan quiet, Action changed)
    {
        _debouncer = new Debouncer(quiet, TimeProvider.System, changed);

        foreach (var text in patterns)
        {
            if (string.IsNullOrWhiteSpace(text)) continue;

            FileSystemWatcher? watcher = null;
            try
            {
                var pattern = FolderPattern.Parse(text);
                if (!Directory.Exists(pattern.Folder)) continue;

                watcher = new FileSystemWatcher(pattern.Folder)
                {
                    IncludeSubdirectories = pattern.Recursive,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                };
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnChanged;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                           or ArgumentException or NotSupportedException)
            {
                // A pattern that cannot be parsed or watched is skipped; the others keep working.
                watcher?.Dispose();
            }
        }
    }

    /// <summary>Stops the watchers, then the debouncer; no call of the action happens after this returns.</summary>
    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _debouncer.Dispose();
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => _debouncer.Signal();

    // An overflowed buffer means events were lost: report a change so the catalog rereads.
    private void OnError(object sender, ErrorEventArgs e) => _debouncer.Signal();
}
