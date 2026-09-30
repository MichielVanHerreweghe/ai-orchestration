# Handover: Mark notes as done

Not verified end to end: SQL Server and Docker were unavailable when this was built. Builds are green; the flows below have not been clicked through.

## What changed
- **API** (`demo/api/Program.cs`): `Note.IsDone` (default false). `GET /api/notes` returns open notes first, done notes last, each group newest first. New `PATCH /api/notes/{id}` with `{ "isDone": bool }` returns the note, or 404 for an unknown id.
- **Frontend** (`demo/web/src/main.js`, `index.html`): each note has a checkbox; ticking or unticking PATCHes and reloads the list. Done notes are struck through (`<s>`). The heading reads `Notes (N open)`. Static heading text is `Notes (0 open)` until the first load.
- **Database**: new `IsDone` column, created by `EnsureCreated`. No migrations.

## Test flows

### 1. Tick a note
- **Precondition**: app running on a fresh `Notes` database; three open notes exist (add "A", "B", "C" via the form).
- **Path**: 1. Open the page. 2. Tick the checkbox of "A".
- **Expected**: "A" is struck through and moves below "C" and "B"; heading reads "Notes (2 open)".
- **Failure signal**: note stays put or is not struck through; heading count unchanged; network error on PATCH.

### 2. Persistence
- **Precondition**: flow 1 done.
- **Path**: 1. Reload the page.
- **Expected**: "A" is still ticked, struck through and last; heading "Notes (2 open)".
- **Failure signal**: "A" is open again after reload.

### 3. Untick
- **Precondition**: one done note and two open notes.
- **Path**: 1. Untick the done note.
- **Expected**: it loses the strike-through, returns among the open notes in newest-first order; heading "Notes (3 open)".
- **Failure signal**: stays struck through or at the bottom.

### 4. Empty list
- **Precondition**: fresh `Notes` database, no notes.
- **Path**: 1. Open the page.
- **Expected**: heading "Notes (0 open)".
- **Failure signal**: a stale or missing count.

### 5. Adding updates the count
- **Precondition**: any state.
- **Path**: 1. Add a note.
- **Expected**: heading count goes up by one.

## Technical notes
- `PATCH /api/notes/{id}`, body `{ "isDone": true|false }`; 404 if the id is unknown.
- No migrations. A local database created before this change lacks `IsDone`, so `GET /api/notes` returns 500. Drop the `Notes` database. Previews start fresh and are unaffected.
- No new config keys.

## Deviations from the plan
None recorded; the diff matches the plan.

## Not included
Editing or deleting notes, bulk clear, filtering, keyboard accessibility beyond a native checkbox, EF migrations, automated tests, `preview/` and `deploy/` changes.
