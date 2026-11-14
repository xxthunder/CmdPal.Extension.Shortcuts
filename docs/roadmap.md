# Roadmap

The way to the Command Palette Extension Gallery, in order. Each item's status is in the item
itself, in the [backlog](backlog/README.md); this page holds only the order and the reasons.

## To the Gallery

| # | Item | Why at this point |
|---|---|---|
| 1 | [CPS-013](backlog/cps-013.md) · One build harness for gate, CI and UAT, with badges in the README | Every later item uses the gate, the CI jobs and the UAT script; written once here, they are not repeated in each pull request. |
| 2 | [CPS-002d](backlog/cps-002d.md) · A file added to a catalogued folder appears without a restart | Users expect a new script to show up on its own; without it, every new file needs a restart of Command Palette. |
| 3 | [CPS-011](backlog/cps-011.md) · A new user sees how to choose folders when the catalogue is empty | A stranger has no `shortcuts` folder; without the hint, the extension looks broken right after the install from the Store. |
| 4 | [CPS-010](backlog/cps-010.md) · Shortcuts is in the Command Palette Extension Gallery, installed from the Microsoft Store | The goal: the identity from Partner Center, the bundle from CI, certification, then the Gallery's pull request. |

CPS-010 starts with the author's steps in Partner Center: the package identity it reserves is
the input of the CI bundle.

## After the Gallery

- shortcuts [SC-089](https://github.com/xxthunder/shortcuts/blob/develop/docs/backlog/sc-089.md):
  the shortcuts install takes the extension from the Store and sets both shortcuts folders. It
  needs the Store's product ID from CPS-010.
- [CPS-008](backlog/cps-008.md) · Requirements and traceability with sphinx-needs,
  sphinx-codelinks and ubCode: does not block a release.
