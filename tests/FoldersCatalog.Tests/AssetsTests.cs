using System.Xml.Linq;
using SkiaSharp;

namespace FoldersCatalog.Tests;

[Collection(LogosTool.CollectionName)]
public sealed class AssetsTests
{
    private const string SourceRelative = "src/CmdPal.Extension.Shortcuts/Assets/Source/Logo.png";
    private const string AssetsRelative = "src/CmdPal.Extension.Shortcuts/Assets";
    private const string GalleryRelative = "store/gallery/icon.png";
    private const string ManifestRelative = "src/CmdPal.Extension.Shortcuts/Package.appxmanifest";
    private const string Rerun =
        "dotnet run tools/logos.cs -- src/CmdPal.Extension.Shortcuts/Assets/Source/Logo.png src/CmdPal.Extension.Shortcuts/Assets store/gallery/icon.png";

    private static string[] PngNames(string dir) =>
        Directory.GetFiles(dir, "*.png").Select(f => Path.GetFileName(f)!).Order(StringComparer.Ordinal).ToArray();

    private const int Tolerance = 2;

    // Skia picks CPU-specific raster paths at run time, so bytes can differ between machines
    // without a real change; compare decoded pixels with a small per-channel tolerance instead.
    private static void AssertSimilar(string freshPath, string committedPath, string name)
    {
        var hint = $"{name} differs from the rendering of the source. Re-run: {Rerun}";
        using var fresh = SKBitmap.Decode(freshPath);
        using var committed = SKBitmap.Decode(committedPath);
        Assert.True(fresh is not null && committed is not null, $"{name} cannot be decoded. Re-run: {Rerun}");
        Assert.True(
            fresh.Width == committed.Width && fresh.Height == committed.Height,
            $"{name} has size {committed.Width}x{committed.Height}, the rendering {fresh.Width}x{fresh.Height}. Re-run: {Rerun}");

        var a = fresh.Pixels;
        var b = committed.Pixels;
        for (var i = 0; i < a.Length; i++)
        {
            var worst = Math.Max(
                Math.Max(Math.Abs(a[i].Red - b[i].Red), Math.Abs(a[i].Green - b[i].Green)),
                Math.Max(Math.Abs(a[i].Blue - b[i].Blue), Math.Abs(a[i].Alpha - b[i].Alpha)));
            Assert.True(
                worst <= Tolerance,
                $"{hint} (pixel {i % fresh.Width},{i / fresh.Width}: channel off by {worst}, allowed {Tolerance})");
        }
    }

    [Fact]
    public void Assets_AreTheRenderingOfTheSource()
    {
        var root = ToolRunner.RepoRoot();
        var source = Path.Combine(root, SourceRelative.Replace('/', Path.DirectorySeparatorChar));
        var assets = Path.Combine(root, AssetsRelative.Replace('/', Path.DirectorySeparatorChar));
        var rendered = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "cpf-assets-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(rendered);

        try
        {
            var renderedGallery = Path.Combine(rendered, "gallery-icon.png.out");
            var (code, _, stderr) = ToolRunner.Run("logos.cs", source, rendered, renderedGallery);

            Assert.True(code == 0, $"logos.cs exited {code} for {SourceRelative}: {stderr}");
            Assert.True(File.Exists(renderedGallery), $"logos.cs wrote no gallery icon to {renderedGallery}");
            Assert.Equal(PngNames(rendered), PngNames(assets));
            foreach (var name in PngNames(rendered))
            {
                AssertSimilar(Path.Combine(rendered, name), Path.Combine(assets, name), name);
            }

            AssertSimilar(renderedGallery, Path.Combine(root, GalleryRelative.Replace('/', Path.DirectorySeparatorChar)), GalleryRelative);
        }
        finally
        {
            Directory.Delete(rendered, recursive: true);
        }
    }

    [Fact]
    public void Manifest_LogosExist()
    {
        var root = ToolRunner.RepoRoot();
        var manifest = Path.Combine(root, ManifestRelative.Replace('/', Path.DirectorySeparatorChar));
        var assets = Path.Combine(root, AssetsRelative.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(manifest);

        var values = document.Descendants()
            .SelectMany(e => e.Attributes().Select(a => a.Value).Append(e.HasElements ? "" : e.Value))
            .Where(v => v.StartsWith(@"Assets\", StringComparison.Ordinal) && v.EndsWith(".png", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        Assert.Contains(@"Assets\SmallTile.png", values);
        Assert.Contains(@"Assets\LargeTile.png", values);
        var tiles = document.Descendants().Where(e => e.Name.LocalName == "DefaultTile").ToArray();
        Assert.Contains(tiles, t => (string?)t.Attribute("Square71x71Logo") == @"Assets\SmallTile.png");
        Assert.Contains(tiles, t => (string?)t.Attribute("Square310x310Logo") == @"Assets\LargeTile.png");

        foreach (var value in values)
        {
            var name = value[@"Assets\".Length..];
            var stem = Path.GetFileNameWithoutExtension(name);
            var exists = File.Exists(Path.Combine(assets, name))
                || Directory.GetFiles(assets, stem + ".*.png").Any(f =>
                {
                    var qualifier = Path.GetFileName(f)[(stem.Length + 1)..];
                    return qualifier.StartsWith("scale-", StringComparison.Ordinal)
                        || qualifier.StartsWith("targetsize-", StringComparison.Ordinal);
                });
            Assert.True(exists, $"The manifest names {value}, but {name} (or a scale/targetsize variant) is not in {AssetsRelative}");
        }
    }

    [Fact]
    public void GalleryIcon_Is256AndAtMost100KB()
    {
        var path = Path.Combine(ToolRunner.RepoRoot(), GalleryRelative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"{GalleryRelative} does not exist. Re-run: {Rerun}");

        using var bitmap = SKBitmap.Decode(path);
        Assert.True(bitmap is not null, $"{GalleryRelative} cannot be decoded");
        Assert.Equal(256, bitmap.Width);
        Assert.Equal(256, bitmap.Height);
        Assert.True(new FileInfo(path).Length <= 100 * 1024, $"{GalleryRelative} is {new FileInfo(path).Length} bytes, the Gallery allows 100 KB");
    }
}
