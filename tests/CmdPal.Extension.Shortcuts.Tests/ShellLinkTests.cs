using System.Text;
using CmdPal.Extension.Shortcuts;

namespace CmdPal.Extension.Shortcuts.Tests;

public sealed class ShellLinkTests : IDisposable
{
    private readonly string _root = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "cpf-shelllink-" + Guid.NewGuid().ToString("N")));

    public ShellLinkTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    // A shell link stores the target as UTF-16 in its StringData and as ANSI in its LinkInfo;
    // either spelling of the path proves the link points there.
    private static bool ContainsPath(byte[] bytes, string path) =>
        IndexOf(bytes, Encoding.Unicode.GetBytes(path)) >= 0
        || IndexOf(bytes, Encoding.Default.GetBytes(path)) >= 0;

    private static void AssertIsShellLinkTo(string linkPath, string target)
    {
        Assert.True(File.Exists(linkPath));
        var bytes = File.ReadAllBytes(linkPath);
        Assert.True(bytes.Length > 4);
        Assert.Equal(new byte[] { 0x4C, 0x00, 0x00, 0x00 }, bytes[..4]);
        Assert.True(ContainsPath(bytes, target), "the link does not contain the target path");
    }

    [Fact]
    public void Create_WritesAShellLinkToTheFolder()
    {
        var folder = Path.Combine(_root, "target-folder");
        Directory.CreateDirectory(folder);
        var link = Path.Combine(_root, "Open shortcuts folder.lnk");

        ShellLink.Create(link, folder);

        AssertIsShellLinkTo(link, folder);
    }

    [Fact]
    public void Create_TargetFile_Works()
    {
        var file = Path.Combine(_root, "target-file.txt");
        File.WriteAllText(file, "x");
        var link = Path.Combine(_root, "Open file.lnk");

        ShellLink.Create(link, file);

        AssertIsShellLinkTo(link, file);
    }
}
