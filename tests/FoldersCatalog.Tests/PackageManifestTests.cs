using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FoldersCatalog.Tests;

public sealed class PackageManifestTests
{
    private static readonly XNamespace Foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly XNamespace Uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
    private static readonly XNamespace Uap3 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/3";

    private const string ManifestRelative = "src/CmdPal.Extension.Shortcuts/Package.appxmanifest";

    private static string ManifestPath() => Path.Combine(ToolRunner.RepoRoot(), ManifestRelative);

    private static XElement Package() => XDocument.Load(ManifestPath()).Root!;

    [Fact]
    public void Identity_IsTheReservedOne()
    {
        var identity = Package().Element(Foundation + "Identity")!;

        Assert.Equal("karnomag.ShortcutsforCommandPalette", (string?)identity.Attribute("Name"));
        Assert.Equal("CN=EB467DD7-FED1-4C41-A876-46255255EB4A", (string?)identity.Attribute("Publisher"));
    }

    [Fact]
    public void Properties_NameTheStoreListing()
    {
        var properties = Package().Element(Foundation + "Properties")!;

        Assert.Equal("Shortcuts for Command Palette", (string?)properties.Element(Foundation + "DisplayName"));
        Assert.Equal("karnomag", (string?)properties.Element(Foundation + "PublisherDisplayName"));
    }

    [Fact]
    public void InsideCommandPalette_ItIsStillShortcuts()
    {
        var package = Package();
        var visual = package.Descendants(Uap + "VisualElements").Single();
        var extension = package.Descendants(Uap3 + "AppExtension").Single();

        Assert.Equal("Shortcuts", (string?)visual.Attribute("DisplayName"));
        Assert.Equal("Shortcuts", (string?)extension.Attribute("DisplayName"));
    }

    [Fact]
    public void Version_IsVersionTxtWithAFourthZero()
    {
        var versionFile = Path.Combine(ToolRunner.RepoRoot(), "version.txt");
        Assert.True(File.Exists(versionFile), "version.txt is missing in the repository root");

        var version = File.ReadAllText(versionFile).Trim();
        var identity = Package().Element(Foundation + "Identity")!;

        Assert.Equal(version + ".0", (string?)identity.Attribute("Version"));
    }

    [Fact]
    public void Version_LineCarriesTheReleasePleaseAnnotation()
    {
        var line = File.ReadAllLines(ManifestPath()).Single(l => Regex.IsMatch(l, "(?<![A-Za-z])Version="));

        Assert.Contains("x-release-please-version", line, StringComparison.Ordinal);
    }
}
