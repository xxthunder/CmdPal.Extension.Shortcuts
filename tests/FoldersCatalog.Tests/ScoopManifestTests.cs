using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FoldersCatalog.Tests;

public sealed class ScoopManifestTests
{
    private static readonly XNamespace Foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

    private static string ManifestPath() =>
        Path.Combine(ToolRunner.RepoRoot(), "bucket", "cmdpal-shortcuts.json");

    private static JsonElement Manifest()
    {
        var path = ManifestPath();
        Assert.True(File.Exists(path), "bucket/cmdpal-shortcuts.json is missing");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static string PackageName() =>
        (string)XDocument.Load(Path.Combine(ToolRunner.RepoRoot(), "src", "CmdPal.Extension.Shortcuts", "Package.appxmanifest"))
            .Root!.Element(Foundation + "Identity")!.Attribute("Name")!;

    private static string[] Script(string hook) =>
        Manifest().GetProperty(hook).GetProperty("script").EnumerateArray().Select(l => l.GetString()!).ToArray();

    [Fact]
    public void Manifest_DownloadsTheStoreSignedBundleOfItsVersion()
    {
        var manifest = Manifest();
        var version = manifest.GetProperty("version").GetString();

        Assert.Equal(
            $"https://github.com/xxthunder/CmdPal.Extension.Shortcuts/releases/download/v{version}/{PackageName()}_{version}.msixbundle",
            manifest.GetProperty("url").GetString());
        Assert.Matches(new Regex("^[0-9a-f]{64}$"), manifest.GetProperty("hash").GetString());
    }

    [Fact]
    public void Installer_AddsThePackage()
    {
        Assert.Contains(Script("installer"), line => line.Contains("Add-AppxPackage -Path \"$dir\\$fname\""));
    }

    [Fact]
    public void Uninstaller_KeepsThePackageOnUpdate()
    {
        // scoop update runs the old version's uninstaller; removing the package would delete its settings.
        Assert.Equal("if ($cmd -eq 'update') { return }", Script("uninstaller")[0]);
    }

    [Fact]
    public void Uninstaller_RemovesThisPackage()
    {
        Assert.Contains(Script("uninstaller"), line => line.Contains($"Get-AppxPackage {PackageName()} | Remove-AppxPackage"));
    }
}
