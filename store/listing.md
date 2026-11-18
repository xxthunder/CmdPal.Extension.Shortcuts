# Store listing

## Description

Shortcuts for Command Palette integrates with the Windows Command Palette to catalogue folders of your choice into its main search. The files in them show up as top-level commands, and Command Palette ranks them by how often and how recently you start them.

- Choose your folders and file extensions in the settings; the default is `%USERPROFILE%\shortcuts`.
- Scripts (`.ps1`, `.cmd`, `.bat`) run in their own folder (`.ps1` needs PowerShell 7, `pwsh`); a list of the arguments used before lets you start them again.
- Links (`.lnk`, `.url`) and programs (`.exe`) start on Enter.
- New, deleted and renamed files show up without a restart.
- An example folder with a small script gets you started.

It reads the folders you choose and keeps its settings and argument history on your machine; it sends nothing. See the privacy statement: https://github.com/xxthunder/CmdPal.Extension.Shortcuts/blob/develop/PRIVACY.md

## Notes for testers

PowerToys with Command Palette is needed. Open Command Palette (Win+Alt+Space by default), type Shortcuts, run "Shortcuts: create example folder" (the example folder is offered only while the catalogue is empty, so on a machine without `%USERPROFILE%\shortcuts` it appears), then type Hello and start Hello.cmd. Hello.ps1 needs PowerShell 7 (`pwsh`).

## Restricted capability: runFullTrust

Command Palette activates the extension as an out-of-process COM server, and the extension needs full trust to start the scripts, links and programs the user chose.


## Screenshots

Uploaded to Partner Center as the Desktop screenshots of the first release (1953x1198, cropped
to the Command Palette window):

- `screenshots/1-empty-start.png`: an empty catalogue, Command Palette searched for "shortcuts";
  Folders set to an empty folder `C:\ShortcutsEmpty`.
- `screenshots/2-in-use.png`: Command Palette searched for "deploy"; Folders set to
  `C:\ShortcutsDemo\**`, a demo folder outside the user profile (no user name in the paths) with
  `Deploy website.cmd`, `Deploy docs.ps1`, `Deploy dashboard.url`, `Deploy folder.lnk` and
  `tools\Deploy checklist.url`.
