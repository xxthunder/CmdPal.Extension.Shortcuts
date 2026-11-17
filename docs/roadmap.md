# Roadmap

The way to the Command Palette Extension Gallery, in order. Each item's status is in the item
itself, in the [backlog](backlog/README.md); this page holds only the order and the reasons.

## To the Gallery

| # | Item | Why at this point |
|---|---|---|
| 1 | [CPS-016](backlog/cps-016.md) · The subtitle shows the file's full path | A script and its `.bat` wrapper look alike in the main search; the full path tells them apart. Part of the first release. |
| 2 | [CPS-017](backlog/cps-017.md) · Folders can be excluded in the settings | Library folders such as `~\shortcuts\lib` fill the main search with files nobody starts. Part of the first release. |
| 3 | [CPS-010](backlog/cps-010.md) · Shortcuts is in the Command Palette Extension Gallery, installed from the Microsoft Store | The goal: the identity from Partner Center, the bundle from CI, certification, then the Gallery's pull request. |

Done on the way: [CPS-013](backlog/cps-013.md) (one build harness), [CPS-002d](backlog/cps-002d.md)
(files appear without a restart), [CPS-011](backlog/cps-011.md) (an empty catalogue explains itself).

CPS-010 starts with the author's steps in Partner Center: the package identity it reserves is
the input of the CI bundle.

## After the Gallery

- shortcuts [SC-089](https://github.com/xxthunder/shortcuts/blob/develop/docs/backlog/sc-089.md):
  the shortcuts install takes the extension from the Store and sets both shortcuts folders. It
  needs the Store's product ID from CPS-010.
- [CPS-008](backlog/cps-008.md) · Requirements and traceability with sphinx-needs,
  sphinx-codelinks and ubCode: does not block a release.
