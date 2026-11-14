using System.Diagnostics;

const string Usage = "Usage: dotnet run build.cs -- <check|extension> [--dry-run]";
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
