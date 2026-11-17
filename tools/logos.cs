#:package SkiaSharp@4.156.0
#:package SkiaSharp.NativeAssets.Linux.NoDependencies@4.156.0

// NoDependencies: the gate's SDK container has no fontconfig. Windows natives come with SkiaSharp.

using SkiaSharp;

if (args.Length is < 2 or > 3 || args.Any(string.IsNullOrWhiteSpace))
{
    Console.Error.WriteLine("Usage: dotnet run tools/logos.cs -- <source.png> <assets-dir> [<gallery-icon.png>]");
    return 2;
}

var sourcePath = args[0];
var assetsDir = args[1];
var galleryPath = args.Length == 3 ? args[2] : null;

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source not found: {sourcePath}");
    return 1;
}

// The one logo list: file name, width, height.
(string Name, int Width, int Height)[] logos =
[
    ("StoreLogo.png", 50, 50),
    ("Square44x44Logo.scale-200.png", 88, 88),
    ("Square44x44Logo.targetsize-16.png", 16, 16),
    ("Square44x44Logo.targetsize-24.png", 24, 24),
    ("Square44x44Logo.targetsize-32.png", 32, 32),
    ("Square44x44Logo.targetsize-48.png", 48, 48),
    ("Square44x44Logo.targetsize-16_altform-unplated.png", 16, 16),
    ("Square44x44Logo.targetsize-24_altform-unplated.png", 24, 24),
    ("Square44x44Logo.targetsize-32_altform-unplated.png", 32, 32),
    ("Square44x44Logo.targetsize-48_altform-unplated.png", 48, 48),
    ("Square150x150Logo.scale-200.png", 300, 300),
    ("Wide310x150Logo.scale-200.png", 620, 300),
    ("SplashScreen.scale-200.png", 1240, 600),
    ("LockScreenLogo.scale-200.png", 48, 48),
    ("SmallTile.png", 71, 71),
    ("LargeTile.png", 310, 310),
    ("Square150x150Logo.png", 150, 150),
    ("Wide310x150Logo.png", 310, 150),
    ("SplashScreen.png", 620, 300),
    ("Square44x44Logo.png", 44, 44),
];

// The Gallery icon: written to the optional third argument.
const int GallerySize = 256;

// Skia has no Lanczos; linear filtering with mipmaps gives the high-quality downscale.
var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

using var source = SKImage.FromEncodedData(sourcePath);
if (source is null)
{
    Console.Error.WriteLine($"Cannot decode source: {sourcePath}");
    return 1;
}

try
{
    Directory.CreateDirectory(assetsDir);

    var targets = logos.Select(l => (Path.Combine(assetsDir, l.Name), l.Width, l.Height)).ToList();
    if (galleryPath is not null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(galleryPath))!);
        targets.Add((galleryPath, GallerySize, GallerySize));
    }

    foreach (var (path, width, height) in targets)
    {
        var scale = Math.Min((float)width / source.Width, (float)height / source.Height);
        var w = source.Width * scale;
        var h = source.Height * scale;
        var target = SKRect.Create((width - w) / 2, (height - h) / 2, w, h);

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(source, target, sampling);
        using var image = surface.Snapshot();
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        png.SaveTo(file);
        Console.WriteLine($"{path} {width}x{height}");
    }
}
catch (Exception e) when (e is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Failed: {e.Message}");
    return 1;
}

return 0;
