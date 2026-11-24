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

Without the Store, install it with [scoop](https://scoop.sh), from the bucket
[xxthunder-scoop-apps](https://github.com/xxthunder/xxthunder-scoop-apps):

```powershell
scoop bucket add xxthunder-scoop-apps https://github.com/xxthunder/xxthunder-scoop-apps
scoop install xxthunder-scoop-apps/cmdpal-shortcuts
```

Scoop downloads the Store-signed bundle from the GitHub release and adds it with
`Add-AppxPackage`; it needs no admin rights, no Developer Mode and no Store. `scoop update`
updates the package in place and keeps your settings; `scoop uninstall cmdpal-shortcuts`
removes it.

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
the release job attaches the `.msixbundle` to the GitHub release, and a `store` job then
submits it to the Microsoft Store
([Publishing to the Microsoft Store](#publishing-to-the-microsoft-store)). The repository
setting "Allow GitHub Actions to create and approve pull requests" must be on. The release pull
request gets no CI checks, because it is opened with `GITHUB_TOKEN`.

## Publishing to the Microsoft Store

After `bundle`, the `store` job of `.github/workflows/release.yml` downloads the bundle from the
GitHub release and runs `msstore publish` for the product `9NN0F5R302LZ` ([CPS-018](docs/backlog/cps-018.md)).
The first publication was by hand ([CPS-010](docs/backlog/cps-010.md)); the job updates an
existing free app and does nothing else with the Store listing.

**One-time setup**

1. Associate the Partner Center account with a Microsoft Entra ID tenant; see
   <https://learn.microsoft.com/en-us/windows/apps/publish/store-submission-api>.
2. In Partner Center, Account settings → Users, add an Entra application and give it the
   Manager role. Click the application's name there and copy its Tenant ID and Client ID.
3. In the Azure portal, Microsoft Entra ID → App registrations → the app → Certificates &
   secrets → Client secrets → New client secret. The lifetime is at most 24 months; Microsoft
   recommends less than 12. The value is shown only once; note the expiry date.
4. Read the Seller ID in Partner Center, Account settings → Identifiers (the msstore docs call
   it "Publisher ID" or "Seller ID").
5. Store the four values as repository secrets; each command asks for the value:

```
gh secret set MSSTORE_TENANT_ID
gh secret set MSSTORE_SELLER_ID
gh secret set MSSTORE_CLIENT_ID
gh secret set MSSTORE_CLIENT_SECRET
```

A missing secret fails the job with `<NAME> is not set; see README, Publishing to the Microsoft
Store`.

**Draft or submit**

The job leaves a draft submission, unless the repository variable `STORE_NO_COMMIT` is `false`.
Unset, or any other value, means a draft. For the first release: check the draft in Partner
Center and submit it there; then let later releases submit themselves:

```
gh variable set STORE_NO_COMMIT --body false
```

Back to drafts: `gh variable set STORE_NO_COMMIT --body true`.

The next release replaces a draft that was not submitted. While a submission is still in
certification, `msstore publish` cannot replace it and the `store` job fails; run the failed job
again once certification is done (command under "Renewing the client secret").

**Renewing the client secret**

Before the expiry date, create a new client secret at the same place in the Azure portal, store
it with the command below, then delete the old secret in the Azure portal:

```
gh secret set MSSTORE_CLIENT_SECRET
```

An expired secret fails the `store` job at the msstore step with the Entra ID error
`AADSTS7000222` ("The provided client secret keys are expired"). The release is then on GitHub
but not in the Store. After renewing the secret, run the failed job again:

```
gh run rerun <run-id> --failed
```

`<run-id>` is the run ID from `gh run list --workflow release.yml`.

## Publishing the Store-signed bundle

The bundle that the release job attaches is unsigned. The Store re-signs the package with a
Microsoft certificate that every Windows trusts, and that signature stays valid outside the
Store. `scripts/publish-store-bundle.ps1` puts the Store-signed bundle on the GitHub release
and into the scoop and WinGet manifests, so that machines without the Store can install it ([CPS-019](docs/backlog/cps-019.md)).
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
   an asset of the same name, so a second run does no harm. It fills the template
   `scoop/cmdpal-shortcuts.json` with the version, the asset's URL and the bundle's SHA-256,
   and writes the result through `gh api` as `bucket/cmdpal-shortcuts.json` into the bucket
   repository `xxthunder/xxthunder-scoop-apps`, where scoop users get it. Change the manifest
   in the template, not in the bucket. Scoop's own `checkver` would see the release before the
   bundle is on it. Then
   `komac update` reads the
   asset's URL, computes the hashes, writes the new manifest and opens a pull request in
   `microsoft/winget-pkgs`. komac does not use the login of `gh`: the script hands it the
   token of `gh auth token` as `GITHUB_TOKEN`, unless `GITHUB_TOKEN` is already set.
   `komac update` needs an existing package. While `microsoft/winget-pkgs` has none, the
   script stops after the upload and prints the `komac new` command for the first manifest,
   which is made once by hand. komac opens its pull request from a fork of
   `microsoft/winget-pkgs` in the account of `gh`, and does not create one: without it, the
   script stops and prints `gh repo fork microsoft/winget-pkgs --clone=false`. With `-WhatIf`, the script reports this step and does not run it.

rg-adguard has no documented interface and can change or go away. Then the script fails, and
the check makes sure that it never publishes a wrong file.

## UAT and trying a development build

Uninstall an installed release first: Windows does not register a development build over the
Store-signed package of the same name, and `uat.ps1` then fails in `Add-AppxPackage` with
`0x80073CF9` ("Rejecting a request to register from AppxBundleManifest.xml because the manifest
is not in the package root"). Installed with Scoop:

```powershell
scoop uninstall cmdpal-shortcuts
```

Installed from the Store: `Get-AppxPackage karnomag.ShortcutsforCommandPalette | Remove-AppxPackage`.
After the UAT, remove the development build (see below) and install the release again.

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
