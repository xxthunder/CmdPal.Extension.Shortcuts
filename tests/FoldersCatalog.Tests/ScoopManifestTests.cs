using System.Text.Json;
using System.Xml.Linq;

namespace FoldersCatalog.Tests;

public sealed class ScoopManifestTests
{
    private static readonly XNamespace Foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

    // The template of the manifest that publish-store-bundle.ps1 fills and writes into the
    // bucket repository xxthunder/xxthunder-scoop-apps.
    private static string ManifestPath() =>
        Path.Combine(ToolRunner.RepoRoot(), "scoop", "cmdpal-shortcuts.json");

    private static JsonElement Manifest()
    {
        var path = ManifestPath();
        Assert.True(File.Exists(path), "scoop/cmdpal-shortcuts.json is missing");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static string PackageName() =>
        (string)XDocument.Load(Path.Combine(ToolRunner.RepoRoot(), "src", "CmdPal.Extension.Shortcuts", "Package.appxmanifest"))
            .Root!.Element(Foundation + "Identity")!.Attribute("Name")!;

    private static string[] Script(string hook) =>
        Manifest().GetProperty(hook).GetProperty("script").EnumerateArray().Select(l => l.GetString()!).ToArray();

    [Theory]
    [InlineData("version")]
    [InlineData("url")]
    [InlineData("hash")]
    public void Template_LeavesTheReleaseFieldsToTheScript(string field)
    {
        Assert.Equal("", Manifest().GetProperty(field).GetString());
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
