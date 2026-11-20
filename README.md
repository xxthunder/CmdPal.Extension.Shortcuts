# CmdPal.Extension.Shortcuts

[![CI](https://github.com/xxthunder/CmdPal.Extension.Shortcuts/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/xxthunder/CmdPal.Extension.Shortcuts/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/xxthunder/CmdPal.Extension.Shortcuts/branch/develop/graph/badge.svg)](https://codecov.io/gh/xxthunder/CmdPal.Extension.Shortcuts)

A [PowerToys Command Palette](https://learn.microsoft.com/windows/powertoys/command-palette/overview)
extension that catalogues folders of your choice into the main search — the files in them,
filtered by extension, as top-level commands that Command Palette ranks by how often and how
recently you start them. It fills the gap Keypirinha's `filescatalog` leaves when moving to
Command Palette: the built-in *Installed apps* extension searches only the Start menu, the
desktop and `PATH`.

*Shortcuts* here are the small things you start by name: scripts (`.ps1`, `.cmd`, `.bat`), links
(`.lnk`, `.url`) and programs (`.exe`), kept in folders of your own — such as `~\shortcuts`,
where [xxthunder/shortcuts](https://github.com/xxthunder/shortcuts), a keyboard-driven launcher
and automation toolkit for Windows, installs itself and its scripts. By default the extension catalogues
`%USERPROFILE%\shortcuts\**` with those six extensions; both are settings
(see [Choosing your folders](#choosing-your-folders)).
New, deleted and renamed files show up in the search without a restart. Files in a linked subfolder (a junction or symbolic link below a pattern) still need a restart; see CPS-014.

Formerly called CmdPal.Extension.Folders.

Status: being published to the Microsoft Store — see the [backlog](docs/backlog/README.md).

## Installing

Install *Shortcuts for Command Palette* from the Microsoft Store:
<https://apps.microsoft.com/detail/9NN0F5R302LZ>, or from Command Palette's Extension Gallery
once it is published there.

## Choosing your folders

After installing, open Command Palette and type *Shortcuts*. This always finds "Shortcuts:
change your settings", which opens Shortcuts' settings; while the catalogue is empty, it also
finds the hint "Shortcuts: no files found, choose your folders", which opens them too. The
settings are also in Command Palette settings, Extensions, Shortcuts.
Enter one folder per line in *Folders*; append `\**` to include everything below it, for
example `%USERPROFILE%\shortcuts\**`.

While the catalogue is empty and the example folder `%USERPROFILE%\shortcuts` is among your
folders, typing *Shortcuts* also finds "Shortcuts: create example folder". It writes
`Hello.cmd`, `Hello.ps1` and a link "Open shortcuts folder" into it. Files that already exist
are kept.

A `.ps1` runs in PowerShell 7 (`pwsh`); install it with `winget install Microsoft.PowerShell`.

## Starting a script

A script (`.ps1`, `.bat`, `.cmd`) runs on Enter, in its own folder. Ctrl+Enter (the item's
second command) opens a list of the arguments the script was started with before, newest
first, at most 10. Typing filters the list, and its first entry runs the script with what was
typed; Enter runs the selected entry.
A `.ps1` runs in `pwsh`, with your profile loaded. Links and
programs (`.url`, `.lnk`, `.exe`) start on Enter, as before.

## Listing the catalogue

From the repository root, with the .NET 10 SDK:

```
dotnet run tools/catalog.cs -- --ext ".url .lnk .cmd .bat .ps1 .exe" "%USERPROFILE%\shortcuts\**" "%USERPROFILE%\shortcuts_private\**"
```

It prints one line per entry (name, folder, full path, tab-separated), then the number of
entries; without patterns or without `--ext` it prints its usage and exits with 2.

## Logos

The logos come from `src/CmdPal.Extension.Shortcuts/Assets/Source/Logo.png`, the logo of
xxthunder/shortcuts. After changing it, render them again:

```
dotnet run tools/logos.cs -- src/CmdPal.Extension.Shortcuts/Assets/Source/Logo.png src/CmdPal.Extension.Shortcuts/Assets store/gallery/icon.png
```

## Setting up a Windows machine

Once per machine, in PowerShell 7:

1. **winget** — `winget --version` must answer. If it is not found:
   - `Get-AppxPackage Microsoft.DesktopAppInstaller` lists App Installer → its folder is missing
     from `PATH`: add `%USERPROFILE%\AppData\Local\Microsoft\WindowsApps` to your user `Path`
     (Start → "Edit environment variables for your account") and open a new terminal;
   - otherwise: `scoop install winget`.
2. **.NET 10 SDK**: `winget install Microsoft.DotNet.SDK.10`. `dotnet --list-sdks` must show
   `10.0.x [C:\Program Files\dotnet\sdk]`. Install the SDK there rather than through Scoop: a
   runtime-only `dotnet.exe` in `C:\Program Files\dotnet`, common as an app's dependency, comes
   first on the machine `PATH` and hides an SDK on the user `PATH`.
3. **Windows SDK 10.0.26100**: `winget install Microsoft.WindowsSDK.10.0.26100`. The extension's
   build generates its WinRT projection from it; without it the build fails with "Could not find
   the Windows SDK in the registry".
4. **Developer Mode**: Settings → System → For developers. Needed to register a development
   build and for the library's link tests, which create symbolic links.
5. **GitHub CLI**: `winget install GitHub.cli`, then `gh auth login` — to check out a pull
   request for its UAT.

## Building and testing

From the repository root, with the .NET 10 SDK:

```
dotnet run build.cs -- check
```

It runs every check: the library's build and tests with coverage on every system, and on
Windows also the extension's tests and its Release build. Elsewhere it prints that it skipped
the extension's part. `scripts/gate` runs the same in the .NET SDK container, so no local SDK
is needed.

To build the installable bundle (x64 and ARM64, unsigned), on Windows:

```
dotnet run build.cs -- bundle
```

Releases: release-please keeps a release pull request open; merging it tags the version, and
the release job attaches the `.msixbundle` to the GitHub release. The repository setting
"Allow GitHub Actions to create and approve pull requests" must be on. The release pull
request gets no CI checks, because it is opened with `GITHUB_TOKEN`.

## Publishing the Store-signed bundle

The bundle that the release job attaches is unsigned. The Store re-signs the package with a
Microsoft certificate that every Windows trusts, and that signature stays valid outside the
Store. `scripts/publish-store-bundle.ps1` puts the Store-signed bundle on the GitHub release
and in WinGet, so that machines without the Store can install it ([CPS-019](docs/backlog/cps-019.md)).
It is a copy of `scripts/publish-msstore-msix.ps1` from
[8LWXpg/CmdPal-ProcessKiller](https://github.com/8LWXpg/CmdPal-ProcessKiller), with a check
added before anything is published.

Run it on Windows, in PowerShell 7, once the Store has certified the release. It needs `gh`
and `komac` (`scoop install komac`). Behind a proxy, it uses `HTTPS_PROXY` like any other tool.

```powershell
.\scripts\publish-store-bundle.ps1 -WhatIf   # download and check only
.\scripts\publish-store-bundle.ps1
```

What it does, in order:

1. **Version.** It reads the tag of the latest GitHub release, for example `v1.0.0`. The
   asset name follows from it: `karnomag.ShortcutsforCommandPalette_1.0.0.msixbundle`.
2. **Download link.** It posts the Store ID `9NN0F5R302LZ` to
   `https://store.rg-adguard.net/api/GetFiles`, with the ring `RP` (Release Preview).
   rg-adguard is a third-party service, not Microsoft's. It asks the Store's delivery service,
   which the Store app also uses, and answers with an HTML page that links every file of the
   product. The `Origin`, `Referer` and `User-Agent` headers make the call look like one from
   its web page. The script takes the last link that ends in `.msixbundle`. That link comes
   from HTML, so it holds `&amp;` instead of `&`. The script decodes it; the download server
   answers an encoded link with 403.
3. **Download.** It downloads the bundle into the temp folder. The link points to Microsoft's
   CDN (`tlu.dl.delivery.mp.microsoft.com`), so rg-adguard gives only the link; the file
   comes from Microsoft. The link carries signed query parameters and probably expires after
   some time.
4. **Check.** ProcessKiller's script has no such step. The script stops before anything is
   published unless:
   - the Authenticode signature is `Valid` and its issuer starts with
     `CN=Microsoft Marketplace CA`, the certificate the Store signs with;
   - the bundle's `AppxMetadata/AppxBundleManifest.xml` has the name
     `karnomag.ShortcutsforCommandPalette` and the release's version (`1.0.0.0`).

   An unsigned or self-signed bundle fails the first check. While the Store has not yet
   certified the release, it still serves the previous version, and the script stops with
   "Try again once the Store has certified".
5. **Publish.** `gh release upload --clobber` attaches the bundle to the release, and replaces
   an asset of the same name, so a second run does no harm. Then `komac update` reads the
   asset's URL, computes the hashes, writes the new manifest and opens a pull request in
   `microsoft/winget-pkgs`. `komac update` needs an existing package: make the first manifest
   once with `komac new`. With `-WhatIf`, the script reports this step and does not run it.

rg-adguard has no documented interface and can change or go away. Then the script fails, and
the check makes sure that it never publishes a wrong file.

## UAT and trying a development build

On Windows, in PowerShell 7, check out the branch (or pull request) and deploy it:

```powershell
gh pr checkout <n>
.\uat.ps1
```

Then run the item's own checks. `uat.ps1` stops the running extension, Command Palette
(`Microsoft.CmdPal.UI`) and PowerToys, builds the extension through `build.cs`, registers its
`AppxManifest.xml` and starts PowerToys again: the one that was running, or else the one Scoop
installed (`scoop install extras/powertoys`). PowerToys starts Command Palette anew; one that
kept running across a registration would list the extension twice. A
registered development build runs in place from its `bin` folder, which is why the extension must
be stopped before each build. Remove it with
`Get-AppxPackage karnomag.ShortcutsforCommandPalette | Remove-AppxPackage`. A development build
registered under the old package name `CmdPal.Extension.Shortcuts` must be
removed once with that old name.

## License

[MIT](LICENSE). The extension derives from Microsoft's Command Palette extension template;
see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
