# AGENTS.md

Instructions for agents working in this repository.

## What this is

A PowerToys Command Palette extension in C#: it catalogues folders of the user's choice into
the main search. The design and the work are in the [backlog](docs/backlog/README.md); the
epic [CPS-002](docs/backlog/cps-002.md) holds the overall design.
The [roadmap](docs/roadmap.md) holds the order of the items up to the Extension Gallery.

## Layout

- `src/FoldersCatalog/` — the catalogue library, `net10.0`, no Windows dependency: which
  files count, settings parsing, debouncing. Everything that can be tested without Windows
  lives here.
- `src/CmdPal.Extension.Shortcuts/` — the extension, `net10.0-windows`, MSIX, from the
  template of Command Palette's *Create extension*. It only maps catalogue entries onto
  commands; keep logic out of it.
- `build.cs`: the one build script; the gate, CI and the author call it
  (`dotnet run build.cs -- check`).
- `uat.ps1`: deploys the checkout for a UAT, Windows only.
- `bucket/`: the scoop manifest; this repository is a scoop bucket. `scripts/publish-store-bundle.ps1`
  sets its version, URL and hash.
- `tests/` — the library's tests, and the extension's tests (Windows only).

## Working here

- Test first: a change to the library starts with a failing test.
- Run `scripts/gate` before every commit. It runs `build.cs check` in the
  `mcr.microsoft.com/dotnet/sdk:10.0` container, so no local .NET SDK is needed.
- The extension builds only on Windows — on the author's Windows machine or in CI.
- Code goes through a pull request into `develop`, from a feature branch
  `feature/<ITEM-ID>-<slug>`; history stays linear (rebase, no merge commits).
- Backlog and docs changes go straight onto `develop`.
- Everything here is English: code, comments, docs, backlog, commit messages.
- Commit messages follow Conventional Commits and name the item ID.

## Closed loop

- **Gate**: `scripts/gate` runs `build.cs check` locally; the extension's build needs a
  Windows runner, so the CI checks of the pull request are the gate.
- **Gate runs in**: the .NET SDK container, started by `scripts/gate`.
- **Rubric**: none of its own yet.
