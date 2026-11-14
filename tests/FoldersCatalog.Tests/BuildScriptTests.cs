namespace FoldersCatalog.Tests;

public sealed class BuildScriptTests
{
    private const string UsageLine = "Usage: dotnet run build.cs -- <check|extension> [--dry-run]";

    private const string ExtensionProject = "src/CmdPal.Extension.Shortcuts/CmdPal.Extension.Shortcuts.csproj";

    private static string[] Lines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();

    private static string[] ExpectedCheckLines()
    {
        string[] common =
        [
            "delete TestResults",
            "dotnet build tools/catalog.cs",
            "dotnet test tests/FoldersCatalog.Tests/FoldersCatalog.Tests.csproj --collect:\"XPlat Code Coverage\" --results-directory TestResults/library",
        ];

        return OperatingSystem.IsWindows()
            ?
            [
                .. common,
                "dotnet test tests/CmdPal.Extension.Shortcuts.Tests/CmdPal.Extension.Shortcuts.Tests.csproj -p:Platform=x64 --collect:\"XPlat Code Coverage\" --results-directory TestResults/extension",
                $"dotnet build {ExtensionProject} -c Release -p:Platform=x64",
            ]
            :
            [
                .. common,
                "skipped: the extension's tests and Release build need Windows",
            ];
    }

    [Fact]
    public void NoTarget_PrintsUsageAndExits2()
    {
        var (code, _, stderr) = ToolRunner.RunFile("build.cs");

        Assert.Equal(2, code);
        Assert.StartsWith(UsageLine, stderr);
    }

    [Fact]
    public void UnknownTarget_PrintsUsageAndExits2()
    {
        var (code, _, stderr) = ToolRunner.RunFile("build.cs", "nope");

        Assert.Equal(2, code);
        Assert.StartsWith(UsageLine, stderr);
    }

    [Fact]
    public void Check_DryRun_ListsTheStepsOfThisSystem()
    {
        var (code, stdout, stderr) = ToolRunner.RunFile("build.cs", "check", "--dry-run");

        Assert.True(code == 0, stderr);
        Assert.Equal(ExpectedCheckLines(), Lines(stdout));
    }

    [Fact]
    public void Check_DryRun_CleansTestResultsFirst()
    {
        var (code, stdout, stderr) = ToolRunner.RunFile("build.cs", "check", "--dry-run");

        Assert.True(code == 0, stderr);
        Assert.Equal("delete TestResults", Lines(stdout).First());
    }

    [Fact]
    public void Check_DryRun_CollectsCoverageInEveryTestStep()
    {
        var (code, stdout, stderr) = ToolRunner.RunFile("build.cs", "check", "--dry-run");

        Assert.True(code == 0, stderr);
        var testLines = Lines(stdout).Where(l => l.StartsWith("dotnet test", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(testLines);
        Assert.All(testLines, l =>
        {
            Assert.Contains("--collect:\"XPlat Code Coverage\"", l);
            Assert.Contains("--results-directory TestResults/", l);
        });
    }

    [Fact]
    public void Check_DryRun_FromAnotherDirectory_UsesRepoRelativePaths()
    {
        var workingDirectory = Path.Combine(ToolRunner.RepoRoot(), "tests");

        var (code, stdout, stderr) = ToolRunner.RunFile(workingDirectory, "../build.cs", ["check", "--dry-run"]);

        Assert.True(code == 0, stderr);
        Assert.Equal(ExpectedCheckLines(), Lines(stdout));
    }

    [Fact]
    public void Extension_DryRun_BuildsDebugOnWindows()
    {
        var (code, stdout, stderr) = ToolRunner.RunFile("build.cs", "extension", "--dry-run");

        if (OperatingSystem.IsWindows())
        {
            Assert.True(code == 0, stderr);
            Assert.Equal([$"dotnet build {ExtensionProject} -p:Platform=x64"], Lines(stdout));
        }
        else
        {
            Assert.Equal(1, code);
            Assert.Equal(["extension: needs Windows"], Lines(stderr));
        }
    }
}
