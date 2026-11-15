using FoldersCatalog;

namespace FoldersCatalog.Tests;

public sealed class ExampleFolderTests : IDisposable
{
    private static readonly char Sep = Path.DirectorySeparatorChar;

    private readonly string _root = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "cpf-example-" + Guid.NewGuid().ToString("N")));

    public ExampleFolderTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string P(params string[] parts) => Path.Combine([_root, .. parts]);

    [Fact]
    public void CreateScripts_MissingFolder_CreatesItAndBothScripts()
    {
        var folder = P("shortcuts");

        var written = ExampleFolder.CreateScripts(folder);

        Assert.Equal(
            [Path.Combine(folder, ExampleFolder.HelloCmd), Path.Combine(folder, ExampleFolder.HelloPs1)],
            written);
        Assert.Equal(
            "@echo off\r\necho Hello from Shortcuts. Arguments: %*\r\npause\r\n",
            File.ReadAllText(written[0]));
        Assert.Equal(
            "Write-Host \"Hello from Shortcuts. Arguments: $args\"\r\nRead-Host \"Press Enter to close\"\r\n",
            File.ReadAllText(written[1]));
        Assert.Equal(ExampleFolder.HelloCmdText, File.ReadAllText(written[0]));
        Assert.Equal(ExampleFolder.HelloPs1Text, File.ReadAllText(written[1]));
    }

    [Fact]
    public void CreateScripts_KeepsAnExistingScript()
    {
        var folder = P("shortcuts");
        Directory.CreateDirectory(folder);
        var cmd = Path.Combine(folder, ExampleFolder.HelloCmd);
        File.WriteAllText(cmd, "mine");

        var written = ExampleFolder.CreateScripts(folder);

        Assert.Equal("mine", File.ReadAllText(cmd));
        Assert.Equal([Path.Combine(folder, ExampleFolder.HelloPs1)], written);
        Assert.Equal(ExampleFolder.HelloPs1Text, File.ReadAllText(written[0]));
    }

    [Fact]
    public void CreateScripts_BothExist_ReturnsNothing()
    {
        var folder = P("shortcuts");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ExampleFolder.HelloCmd), "one");
        File.WriteAllText(Path.Combine(folder, ExampleFolder.HelloPs1), "two");

        var written = ExampleFolder.CreateScripts(folder);

        Assert.Empty(written);
        Assert.Equal("one", File.ReadAllText(Path.Combine(folder, ExampleFolder.HelloCmd)));
        Assert.Equal("two", File.ReadAllText(Path.Combine(folder, ExampleFolder.HelloPs1)));
    }

    [Fact]
    public void IsOffered_RecursivePattern_True()
    {
        var folder = P("shortcuts");

        Assert.True(ExampleFolder.IsOffered([folder + Sep + "**"], folder));
    }

    [Fact]
    public void IsOffered_FlatPattern_True()
    {
        var folder = P("shortcuts");

        Assert.True(ExampleFolder.IsOffered([folder], folder));
    }

    [Fact]
    public void IsOffered_OtherFolder_False()
    {
        var folder = P("shortcuts");

        Assert.False(ExampleFolder.IsOffered([P("other"), P("other") + Sep + "**", "", "  "], folder));
    }

    [Fact]
    public void IsOffered_TrailingSeparator_True()
    {
        var folder = P("shortcuts");

        Assert.True(ExampleFolder.IsOffered([folder + Sep], folder));
        Assert.True(ExampleFolder.IsOffered([P("other"), folder], folder + Sep));
    }

    [Fact]
    public void IsOffered_InvalidPattern_IsNoMatch()
    {
        var folder = P("shortcuts");

        Assert.False(ExampleFolder.IsOffered(["bad\0pattern", P("other")], folder));
    }

    [Fact]
    public void IsOffered_EnvironmentVariable_True()
    {
        var previous = Environment.GetEnvironmentVariable("CPS_TEST_ROOT");
        Environment.SetEnvironmentVariable("CPS_TEST_ROOT", _root);
        try
        {
            var pattern = "%CPS_TEST_ROOT%" + Sep + "shortcuts" + Sep + "**";

            Assert.True(ExampleFolder.IsOffered([pattern], P("shortcuts")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CPS_TEST_ROOT", previous);
        }
    }

    [Fact]
    public void DefaultFolder_IsShortcutsInTheProfile()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.Equal(Path.Combine(profile, "shortcuts"), ExampleFolder.DefaultFolder);
    }
}
