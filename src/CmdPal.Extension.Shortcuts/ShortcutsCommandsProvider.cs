// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FoldersCatalog;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal.Extension.Shortcuts;

public sealed partial class ShortcutsCommandsProvider : CommandProvider
{
    // The extension's own commands start with its name: Command Palette's empty search does not surface them, typing "Shortcuts" does.
    public const string HintTitle = "Shortcuts: no files found, choose your folders";

    public const string OfferTitle = "Shortcuts: create example folder";

    public const string SettingsTitle = "Shortcuts: change your settings";

    private readonly SettingsManager _settings;

    private readonly Func<LaunchSpec, bool> _startScript;

    private readonly ArgumentHistory _history;

    private readonly TimeSpan _rebuildQuiet;

    private readonly string _exampleFolder;

    // Guards only the swap of the watcher; Reload never takes it, so a SettingsChanged thread that
    // disposes the old watcher (and waits for a running Reload) cannot deadlock with it.
    private readonly object _watcherLock = new();

    private CatalogWatcher _watcher;

    private volatile ICommandItem[] _commands;

    public ShortcutsCommandsProvider()
        : this(new SettingsManager())
    {
    }

    public ShortcutsCommandsProvider(SettingsManager settings, Func<LaunchSpec, bool>? startScript = null, ArgumentHistory? history = null, TimeSpan? rebuildQuiet = null, string? exampleFolder = null)
    {
        _settings = settings;
        _startScript = startScript ?? StartProcess;
        _history = history ?? new ArgumentHistory(SettingsManager.HistoryJsonPath());
        DisplayName = "Shortcuts";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Settings = settings.Settings;
        _rebuildQuiet = rebuildQuiet ?? TimeSpan.FromMilliseconds(500);
        _exampleFolder = exampleFolder ?? ExampleFolder.DefaultFolder;
        _commands = BuildCommands();
        _watcher = NewWatcher();
        settings.Settings.SettingsChanged += (_, _) => OnSettingsChanged();
    }

    private CatalogWatcher NewWatcher() =>
        new(_settings.Patterns, _rebuildQuiet, Reload);

    private void OnSettingsChanged() => Rewatch();

    // Dispose the watcher, rebuild, start a new watcher: after a settings change and after the example folder was created.
    private void Rewatch()
    {
        lock (_watcherLock)
        {
            _watcher.Dispose();
            try
            {
                Reload();
            }
            finally
            {
                _watcher = NewWatcher();
            }
        }
    }

    // Runs on the debouncer's thread and on the settings thread; a folder that is briefly
    // unreadable keeps the previous list.
    private void Reload()
    {
        ICommandItem[] commands;
        try
        {
            commands = BuildCommands();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        _commands = commands;
        RaiseItemsChanged(commands.Length);
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    // Not ShellHelpers.OpenInShell: it catches the start failure and only returns false, so RunScriptCommand could not show why the start failed.
    private static bool StartProcess(LaunchSpec spec)
    {
        using var process = Process.Start(new ProcessStartInfo(spec.FileName, spec.Arguments)
        {
            WorkingDirectory = spec.WorkingDirectory,
            UseShellExecute = true,
        });
        return process != null;
    }

    private ICommandItem[] BuildCommands()
    {
        var items = Catalog.Build(_settings.Patterns, _settings.Extensions)
            .Select(ToItem)
            .ToArray();
        return [.. items.Length > 0 ? items : EmptyCatalogItems(), SettingsItem()];
    }

    private CommandItem SettingsItem() =>
        new(_settings.Settings.SettingsPage)
        {
            Title = SettingsTitle,
            Subtitle = "The folders and file extensions to catalogue",
        };

    private ICommandItem[] EmptyCatalogItems()
    {
        var items = new List<ICommandItem>
        {
            new CommandItem(_settings.Settings.SettingsPage)
            {
                Title = HintTitle,
                Subtitle = "Open the settings to add the folders to catalogue",
            },
        };
        if (ExampleFolder.IsOffered(_settings.Patterns, _exampleFolder))
        {
            items.Add(new CommandItem(new CreateExamplesCommand(_exampleFolder, Rewatch))
            {
                Title = OfferTitle,
                Subtitle = _exampleFolder,
            });
        }

        return items.ToArray();
    }

    private ICommandItem ToItem(CatalogEntry entry)
    {
        var icon = new IconInfo(entry.FullPath);
        if (!ScriptLaunch.IsScript(entry.FullPath))
        {
            return new CommandItem(new StartFileCommand(entry.FullPath, (p, dir) => ShellHelpers.OpenInShell(p, workingDir: dir)))
            {
                Title = entry.Name,
                Subtitle = entry.Folder,
                Icon = icon,
            };
        }

        return new CommandItem(new RunScriptCommand(entry.FullPath, () => string.Empty, _startScript))
        {
            Title = entry.Name,
            Subtitle = entry.Folder,
            Icon = icon,
            MoreCommands =
            [
                new CommandContextItem(new ScriptArgumentsPage(entry.FullPath, _history, _startScript)),
            ],
        };
    }
}
