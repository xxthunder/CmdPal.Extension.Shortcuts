using System.Diagnostics;

const string Usage = "Usage: dotnet run build.cs -- <check|extension|bundle> [--dry-run]";
const string ExtensionProject = "src/CmdPal.Extension.Shortcuts/CmdPal.Extension.Shortcuts.csproj";
const string Coverage = "--collect:\"XPlat Code Coverage\"";

var target = args.FirstOrDefault(a => a != "--dry-run");
var dryRun = args.Contains("--dry-run");
var root = (string)AppContext.GetData("EntryPointFileDirectoryPath")!;
var windows = OperatingSystem.IsWindows();

// Each step is a line to print; a step starting with "dotnet " is run, the others are built in.
string[] steps;
switch (target)
{
    case "check":
        steps =
        [
            "delete TestResults",
            "dotnet build tools/catalog.cs",
            $"dotnet test tests/FoldersCatalog.Tests/FoldersCatalog.Tests.csproj {Coverage} --results-directory TestResults/library",
            .. windows
                ?
                [
                    $"dotnet test tests/CmdPal.Extension.Shortcuts.Tests/CmdPal.Extension.Shortcuts.Tests.csproj -p:Platform=x64 {Coverage} --results-directory TestResults/extension",
                    $"dotnet build {ExtensionProject} -c Release -p:Platform=x64",
                ]
                : new[] { "skipped: the extension's tests and Release build need Windows" },
        ];
        break;
    case "extension":
        if (!windows)
        {
            Console.Error.WriteLine("extension: needs Windows");
            return 1;
        }

        steps = [$"dotnet build {ExtensionProject} -p:Platform=x64"];
        break;
    case "bundle":
        if (!windows)
        {
            Console.Error.WriteLine("bundle: needs Windows");
            return 1;
        }

        var build = $"dotnet build {ExtensionProject} -c Release";
        const string Packaging = "-p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false";
        steps =
        [
            "delete AppPackages",
            $"{build} -p:Platform=x64 {Packaging} -p:AppxPackageDir=\"{root}\\AppPackages\\x64\\\\\"",
            $"{build} -p:Platform=ARM64 {Packaging} -p:AppxPackageDir=\"{root}\\AppPackages\\ARM64\\\\\"",
            $"bundle AppPackages\\ShortcutsforCommandPalette_{BundleVersion()}.msixbundle",
        ];
        break;
    default:
        Console.Error.WriteLine(Usage);
        return 2;
}

foreach (var step in steps)
{
    if (dryRun || !step.StartsWith("dotnet ", StringComparison.Ordinal))
    {
        Console.WriteLine(step);
        if (dryRun || step.StartsWith("skipped:", StringComparison.Ordinal))
        {
            continue;
        }

        if (step.StartsWith("bundle ", StringComparison.Ordinal))
        {
            var bundled = Bundle(step["bundle ".Length..]);
            if (bundled != 0)
            {
                return bundled;
            }

            continue;
        }

        if (!step.StartsWith("delete ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unknown step: {step}");
        }

        var path = Path.Combine(root, step["delete ".Length..]);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        continue;
    }

    Console.WriteLine($"> {step}");
    var start = new ProcessStartInfo("dotnet", step["dotnet ".Length..]) { WorkingDirectory = root };
    using var process = Process.Start(start)!;
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        return process.ExitCode;
    }
}

return 0;

string BundleVersion() => File.ReadAllText(Path.Combine(root, "version.txt")).Trim() + ".0";

// Bundles the x64 and ARM64 app packages found under AppPackages into the given .msixbundle.
int Bundle(string output)
{
    var version = BundleVersion();
    var packages = Path.Combine(root, "AppPackages");
    var staging = Path.Combine(packages, "bundle");
    Directory.CreateDirectory(staging);

    foreach (var platform in new[] { "x64", "ARM64" })
    {
        var found = Directory.Exists(Path.Combine(packages, platform))
            ? Directory.EnumerateFiles(Path.Combine(packages, platform), "*.msix", SearchOption.AllDirectories)
                .Where(f => !f.Split(Path.DirectorySeparatorChar).Contains("Dependencies", StringComparer.OrdinalIgnoreCase))
                .ToArray()
            : [];
        if (found.Length != 1 || !Path.GetFileName(found[0]).Contains(version, StringComparison.Ordinal))
        {
            var what = found.Length == 0 ? "none" : string.Join(", ", found.Select(f => Path.GetRelativePath(root, f)));
            Console.Error.WriteLine($"bundle: expected exactly one {platform} .msix named for version {version} under AppPackages\\{platform}, found: {what}");
            return 1;
        }

        File.Copy(found[0], Path.Combine(staging, Path.GetFileName(found[0])), overwrite: true);
    }

    var bin = @"C:\Program Files (x86)\Windows Kits\10\bin";
    var makeappx = Directory.Exists(bin)
        ? Directory.GetDirectories(bin)
            .Select(d => (Dir: d, Version: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
            .Where(d => d.Version is not null && File.Exists(Path.Combine(d.Dir, "x64", "makeappx.exe")))
            .OrderByDescending(d => d.Version)
            .Select(d => Path.Combine(d.Dir, "x64", "makeappx.exe"))
            .FirstOrDefault()
        : null;
    if (makeappx is null)
    {
        Console.Error.WriteLine($"bundle: makeappx.exe not found under {bin}\\<version>\\x64 (install the Windows SDK)");
        return 1;
    }

    var start = new ProcessStartInfo(makeappx) { WorkingDirectory = root };
    foreach (var argument in new[] { "bundle", "/o", "/bv", version, "/d", Path.Combine("AppPackages", "bundle"), "/p", output })
    {
        start.ArgumentList.Add(argument);
    }

    Console.WriteLine($"> makeappx {string.Join(' ', start.ArgumentList)}");
    using var process = Process.Start(start)!;
    process.WaitForExit();
    return process.ExitCode;
}
