# Roadmap

The way to the Command Palette Extension Gallery, in order. Each item's status is in the item
itself, in the [backlog](backlog/README.md); this page holds only the order and the reasons.

## To the Gallery

| # | Item | Why at this point |
|---|---|---|
| 1 | [CPS-016](backlog/cps-016.md) · The subtitle shows the file's full path | A script and its `.bat` wrapper look alike in the main search; the full path tells them apart. Part of the first release. |
| 2 | [CPS-017](backlog/cps-017.md) · Folders can be excluded in the settings | Library folders such as `~\shortcuts\lib` fill the main search with files nobody starts. Part of the first release. |
| 3 | [CPS-019](backlog/cps-019.md) · A release puts the Store-signed bundle on GitHub and in WinGet | The goal: the Gallery's pull request, with the `winget` install source next to the Store's (taken over from CPS-010). |

Done on the way: [CPS-013](backlog/cps-013.md) (one build harness), [CPS-002d](backlog/cps-002d.md)
(files appear without a restart), [CPS-011](backlog/cps-011.md) (an empty catalogue explains itself),
[CPS-010](backlog/cps-010.md) (the Store release v1.0.0).

## After the Gallery

- shortcuts [SC-089](https://github.com/xxthunder/shortcuts/blob/develop/docs/backlog/sc-089.md):
  the shortcuts install takes the extension from the Store and sets both shortcuts folders. It
  needs the Store's product ID from CPS-010.
- [CPS-008](backlog/cps-008.md) · Requirements and traceability with sphinx-needs,
  sphinx-codelinks and ubCode: does not block a release.
