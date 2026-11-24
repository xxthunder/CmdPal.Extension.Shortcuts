# Roadmap

The order of the open work, with the reasons. Each item's status is in the item itself, in the
[backlog](backlog/README.md); this page holds only the order and the reasons.

## Where it stands

Release v1.0.0 is out ([CPS-010](backlog/cps-010.md)). It installs from the Microsoft Store, and
without the Store through scoop from the bucket
[xxthunder-scoop-apps](https://github.com/xxthunder/xxthunder-scoop-apps). The Store-signed
bundle is an asset of the GitHub release v1.0.0.

## Next

| # | Item | Why at this point |
|---|---|---|
| 1 | [CPS-019](backlog/cps-019.md) · A release puts the Store-signed bundle on GitHub and in WinGet | Waits for Microsoft: the first WinGet manifest is an open pull request in `microsoft/winget-pkgs`. Once it is merged, the pull request to `microsoft/CmdPal-Extensions` puts Shortcuts in the Gallery, with both install sources. |
| 2 | [CPS-018](backlog/cps-018.md) · A release publishes the update to the Microsoft Store | Every feature after v1.0.0 needs a Store-signed release. Without it, each release goes through Partner Center by hand. |
| 3 | [CPS-016](backlog/cps-016.md) · The title shows the file's extension | A script and its `.bat` wrapper look alike in the main search; the extension in the title tells them apart. The first feature to ship through CPS-018. |
| 4 | [CPS-017](backlog/cps-017.md) · Folders can be excluded in the settings | Library folders such as `~\shortcuts\lib` fill the main search with files nobody starts. |

Done on the way: [CPS-013](backlog/cps-013.md) (one build harness), [CPS-002d](backlog/cps-002d.md)
(files appear without a restart), [CPS-011](backlog/cps-011.md) (an empty catalogue explains itself),
[CPS-010](backlog/cps-010.md) (the Store release v1.0.0).

## Later

- shortcuts [SC-089](https://github.com/xxthunder/shortcuts/blob/develop/docs/backlog/sc-089.md):
  the shortcuts install takes the extension through scoop and sets both shortcuts folders.
- [CPS-008](backlog/cps-008.md) · Requirements and traceability with sphinx-needs,
  sphinx-codelinks and ubCode: does not block a release.
