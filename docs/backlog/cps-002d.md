# [CPS-002d] A file added to a catalogued folder appears without a restart

**Status**: In Progress
**Priority**: Medium
**Component**: `src/FoldersCatalog/`, `src/CmdPal.Extension.Shortcuts/`

**Summary**:
As a user of Command Palette, I want a new script to show up in the search right after I save
it, so that I do not restart the palette or regenerate anything.

**Description**:
A `FileSystemWatcher` on each catalogued folder — with subdirectories for a `\**` pattern —
reports created, deleted and renamed files. Changes are debounced: a burst of events, such as
a `git pull` in `~\shortcuts`, rebuilds the catalogue once, after the folder has been quiet for
a short time, and then raises `ItemsChanged`. The debounce is part of the library and tested
there with a fake clock.

**Technical Notes**:
- From CPS-002a's review: `Catalog.Build` throws when a folder disappears between its
  existence check and its enumeration (`DirectoryNotFoundException`), or when a link target's
  ancestor cannot be traversed (`UnauthorizedAccessException`). Watcher-driven rebuilds meet
  both — a rebuild must not end the catalogue on them.

**Scope Decisions**:

- 2026-11-15 (final review): Folders missing at start are not watched until the settings change or Command Palette restarts; a watched folder that is deleted stops being watched the same way.
- 2026-11-15 (final review): Linked subfolders below a `\**` pattern are not watched: Windows' FileSystemWatcher does not descend into reparse points; follow-up CPS-014.
- 2026-11-15 (final review): The debounce is built on `TimeProvider` (about 50 lines, tested with FakeTimeProvider) rather than taken from Rx `Throttle`: System.Reactive would be a heavy dependency for one operator in an AOT-compatible library.
- 2026-11-15 (final review): The quiet time is 500 ms.
- 2026-11-15 (final review): A file created between a catalogue walk and the start of its watcher (milliseconds, at start and on a settings change) appears with the next change in that tree.

**Acceptance Criteria**:
- [ ] A burst of change events leads to one rebuild after the quiet time, with tests
- [ ] A file created in a catalogued folder appears in the main search without a restart of Command Palette
- [ ] A deleted file disappears from the main search without a restart
