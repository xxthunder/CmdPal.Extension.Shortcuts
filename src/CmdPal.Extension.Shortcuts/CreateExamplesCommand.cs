using System;
using System.IO;
using FoldersCatalog;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal.Extension.Shortcuts;

/// <summary>Writes the example scripts and a link to the folder, then has the provider watch it.</summary>
public sealed partial class CreateExamplesCommand : InvokableCommand
{
    private readonly string _folder;
    private readonly Action _rewatch;

    public CreateExamplesCommand(string folder, Action rewatch)
    {
        _folder = folder;
        _rewatch = rewatch;
        Name = "Create example folder";
    }

    public override ICommandResult Invoke()
    {
        string? failure = null;
        try
        {
            ExampleFolder.CreateScripts(_folder);
            var link = Path.Combine(_folder, ExampleFolder.FolderShortcut);
            if (!File.Exists(link))
            {
                try
                {
                    ShellLink.Create(link, _folder);
                }
                catch (Exception ex)
                {
                    failure = "The scripts were created, but not the link to the folder: " + ex.Message;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return CommandResult.ShowToast("Could not create the example folder: " + ex.Message);
        }

        _rewatch();
        return failure is null ? CommandResult.KeepOpen() : CommandResult.ShowToast(failure);
    }
}
