using System.Text.Json;

namespace FoldersCatalog.Tests;

public sealed class GalleryEntryTests
{
    private static string EntryPath() =>
        Path.Combine(ToolRunner.RepoRoot(), "store", "gallery", "extension.json");

    private static JsonElement Entry()
    {
        var path = EntryPath();
        Assert.True(File.Exists(path), "store/gallery/extension.json is missing");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    [Fact]
    public void Entry_NamesTheExtension()
    {
        var entry = Entry();

        Assert.Equal("karnomag.shortcuts", entry.GetProperty("id").GetString());
        Assert.Equal("Shortcuts", entry.GetProperty("title").GetString());
    }

    [Fact]
    public void Entry_NamesTheAuthor()
    {
        var author = Entry().GetProperty("author");

        Assert.Equal("karnomag", author.GetProperty("name").GetString());
    }

    [Fact]
    public void Entry_InstallsFromTheStorePackage()
    {
        var sources = Entry().GetProperty("installSources").EnumerateArray().ToList();

        var store = Assert.Single(sources, s => s.GetProperty("type").GetString() == "msstore");
        Assert.Equal("9NN0F5R302LZ", store.GetProperty("id").GetString());
    }

    [Fact]
    public void Entry_IconExistsNextToIt()
    {
        var icon = Entry().GetProperty("icon").GetString();

        Assert.False(string.IsNullOrWhiteSpace(icon));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(EntryPath())!, icon!)),
            $"the icon {icon} named in extension.json is missing next to it");
    }
}
