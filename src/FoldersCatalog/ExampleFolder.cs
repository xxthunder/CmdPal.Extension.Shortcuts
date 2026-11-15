using System.Text;

namespace FoldersCatalog;

/// <summary>The example scripts, and whether "Shortcuts: create example folder" applies to the configured patterns.</summary>
public static class ExampleFolder
{
    public const string HelloCmd = "Hello.cmd";
    public const string HelloPs1 = "Hello.ps1";
    public const string FolderShortcut = "Open shortcuts folder.lnk";

    public static string HelloCmdText => "@echo off\r\necho Hello from Shortcuts. Arguments: %*\r\npause\r\n";

    public static string HelloPs1Text =>
        "Write-Host \"Hello from Shortcuts. Arguments: $args\"\r\nRead-Host \"Press Enter to close\"\r\n";

    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "shortcuts");

    /// <summary>Writes the example scripts into <paramref name="folder"/>, keeping any that exist; returns the paths written.</summary>
    public static IReadOnlyList<string> CreateScripts(string folder)
    {
        Directory.CreateDirectory(folder);
        var written = new List<string>();
        foreach (var (name, text) in new[] { (HelloCmd, HelloCmdText), (HelloPs1, HelloPs1Text) })
        {
            var path = Path.Combine(folder, name);
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                var bytes = new UTF8Encoding(false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (IOException) when (File.Exists(path))
            {
                continue; // never overwrite a script that is already there
            }

            written.Add(path);
        }

        return written;
    }

    /// <summary>True when a pattern names <paramref name="folder"/>, recursive or flat.</summary>
    public static bool IsOffered(IEnumerable<string> patterns, string folder)
    {
        var wanted = Normalize(folder);
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            try
            {
                if (Catalog.PathComparer.Equals(Normalize(FolderPattern.Parse(pattern).Folder), wanted))
                {
                    return true;
                }
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // A pattern that does not parse is no match.
            }
        }

        return false;
    }

    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
