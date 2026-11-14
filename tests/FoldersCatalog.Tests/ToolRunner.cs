using System.Diagnostics;

namespace FoldersCatalog.Tests;

/// <summary>Runs a file-based app with dotnet run and captures its output.</summary>
internal static class ToolRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    internal static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "scripts", "gate")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Repository root (scripts/gate) not found above " + AppContext.BaseDirectory);
    }

    internal static (int ExitCode, string Stdout, string Stderr) Run(string script, params string[] args) =>
        RunFile("tools/" + script, args);

    /// <summary>Runs a file-based app given by its repository-relative path, from the repository root.</summary>
    internal static (int ExitCode, string Stdout, string Stderr) RunFile(string repoRelativePath, params string[] args) =>
        RunFile(RepoRoot(), repoRelativePath, args);

    /// <summary>Runs a file-based app; the path is relative to the working directory. The arguments are not
    /// <c>params</c>, so that a call with only strings binds to the root-based overload.</summary>
    internal static (int ExitCode, string Stdout, string Stderr) RunFile(
        string workingDirectory, string repoRelativePath, string[] args)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        info.ArgumentList.Add("run");
        info.ArgumentList.Add(repoRelativePath);
        info.ArgumentList.Add("--");
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            Assert.Fail($"dotnet run did not exit within {Timeout.TotalSeconds} s. stderr:\n{stderr.GetAwaiter().GetResult()}");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }
}
