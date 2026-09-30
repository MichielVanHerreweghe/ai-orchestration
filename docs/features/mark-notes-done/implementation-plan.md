---
slug:       mark-notes-done
issue:      https://github.com/MichielVanHerreweghe/ai-orchestration/issues/5
branch:     feat/mark-notes-done
worktree:   /workspace/feat/mark-notes-done
convention: docs/features/<slug>/
language:   en
approved:   true
database:   Notes
seeded:     false
pr:         null
artifacts:
  plan:     null
  handover: null
milestones:
  - id: 1
    title: API stores and returns done state
    status: done
    commit: 92357db
  - id: 2
    title: Checkbox, strike-through and open count in the web page
    status: done
    commit: dae39f7
  - id: 3
    title: Modern look for the web page
    status: done
    commit: 846452b
---

# Implementation plan: Mark notes as done

## Context
Issue [#5](https://github.com/MichielVanHerreweghe/ai-orchestration/issues/5):

> In the demo notes app (`demo/`), let people tick off notes they've handled.
> - Each note in the list gets a checkbox. Ticking it marks the note as done; unticking it makes it open again.
> - Done notes stay in the list, shown struck through and below the open ones.
> - The heading shows how many notes are still open, e.g. "Notes (2 open)".
> - Whether a note is done is stored in the database, so it survives a page reload.

The issue has no further comments beyond the `/feature-plan` command.

## How it works today
- `Note` has only `Id` and `Text` (`demo/api/Program.cs:44-49`); `NotesDb` is a single `DbSet<Note>` (`demo/api/Program.cs:39-42`).
- The schema is created with `EnsureCreatedAsync` in a retry loop (`demo/api/Program.cs:11-25`). There are no EF migrations in the repo (`git ls-files demo` lists none). `EnsureCreated` does not alter an existing table.
- Endpoints: `GET /api/notes` ordered by `Id` descending (`demo/api/Program.cs:27`) and `POST /api/notes` (`demo/api/Program.cs:28-33`). Nothing updates a note.
- The page is plain JS: `load()` fetches the list and renders one `<li>` per note (`demo/web/src/main.js:4-7`); the form posts and reloads (`demo/web/src/main.js:9-18`). The heading is static `<h1>Notes</h1>` (`demo/web/index.html:9`).
- nginx proxies `/api/` to the API (`demo/web/nginx.conf:5-7`), so a new API route needs no proxy change.
- Previews run SQL Server without a volume (`preview/sql.yaml`), so every preview starts with an empty database and `EnsureCreated` builds the new column. Readiness probes `/api/notes` (`preview/api.yaml:27-30`).
- There is no test project in `demo/`.

## Approach
Follow the existing shape: minimal API endpoints in `Program.cs`, plain DOM code in `main.js`.

- **API**: add `bool IsDone` to `Note`. `GET /api/notes` orders by `IsDone` then `Id` descending, so open notes come first and done ones last, and the server owns the ordering. Add `PATCH /api/notes/{id}` taking `{ "isDone": bool }`, returning the note or 404.
- **Web**: render each `<li>` with a checkbox (`checked = note.isDone`) and the text; done notes get a strike-through via `<s>` or `text-decoration`. On change, PATCH and call `load()` (same pattern as the form submit). The heading is set from the count of `!isDone` notes: `Notes (N open)`. The static `<h1>` gets an id so `load()` can set its text.
- **Look (added after feedback)**: one new plain stylesheet `demo/web/src/style.css`, imported from `main.js` so Vite bundles it. No CSS framework, icon set or web font (no new dependency, no external request from the container). Centred card on a soft background, system font stack, rounded inputs with a visible focus ring, an accent-coloured Add button, list rows as cards with a subtle border and hover state, native checkbox styled with `accent-color`, done rows muted plus struck through, an open-count "pill" in the heading, and a `prefers-color-scheme: dark` variant via CSS variables. Layout stays usable at phone width. `main.js` only gains class names and wraps checkbox and text in a `<label>` so the whole row is the click target.
- **Storage**: the `IsDone` column comes from `EnsureCreated`, defaulting to false.

## Alternatives rejected
- **Sort in the browser**: the server already sorts; putting the two-group order in the query keeps one place that decides order.
- **Optimistic UI update without reload**: more code for a demo whose `load()` is already the refresh path.
- **Adding EF migrations**: a new tool and files for a demo that uses `EnsureCreated` and throwaway preview databases. See Risks.
- **Separate `/done` route with POST/DELETE**: PATCH with a body is one route and covers both directions.

## Milestones

### 1. API stores and returns done state
- **Files**: `demo/api/Program.cs`
- **Acceptance**: `Note` has `IsDone` (default false). `GET /api/notes` returns open notes before done ones, each group newest first, and includes `isDone`. `PATCH /api/notes/{id}` with `{"isDone":true}` persists and returns the note; unknown id returns 404. `dotnet build demo/api` is green.
- **Tests**: no test project exists and adding one is out of proportion for this change; verify with `dotnet build` and, where SQL Server is available, curl (POST three notes, PATCH one, GET and check order, PATCH back).

### 2. Checkbox, strike-through and open count in the web page
- **Files**: `demo/web/src/main.js`, `demo/web/index.html`
- **Acceptance**: each note shows a checkbox; ticking/unticking PATCHes and refreshes the list; done notes are struck through and last; the heading reads `Notes (N open)` and updates on add, tick and untick, and after reload it reflects stored state. `npm run build` in `demo/web` is green.
- **Tests**: walk the three acceptance scenarios from the issue by hand (see the use case).

### 3. Modern look for the web page
- **Files**: `demo/web/src/style.css` (new), `demo/web/src/main.js` (class names, `<label>` wrapper, stylesheet import), `demo/web/index.html` (heading markup for the count pill, class names)
- **Acceptance**: the page shows the centred card layout, styled form and note rows described in Approach, in both light and dark colour schemes and at 360px width. Done notes are muted and struck through. Text/background contrast is at least WCAG AA, and the checkbox and input show a visible keyboard focus state. Behaviour from milestones 1 and 2 is unchanged, and the heading text still reads `Notes (N open)` (the pill may wrap the number, but the accessible text stays the same). `npm run build` in `demo/web` is green.
- **Tests**: no automated tests. If a browser is available, screenshot light, dark and narrow widths and attach them to the PR; otherwise say the styling is unverified visually.

## Risks
- **`EnsureCreated` won't add the column to an existing database.** A long-lived local database created before this change makes `GET /api/notes` fail with an invalid column error. Signal: 500 on the notes list after upgrading. Fix for local use: drop the `Notes` database. Previews are unaffected (fresh database each time).
- **Cannot run SQL Server or Docker here**, so the end-to-end flow is unverified in this environment; the preview environment on the pull request is where it gets exercised.

## Open questions
Assumptions made without asking:
- Route shape is `PATCH /api/notes/{id}` with `{ "isDone": bool }`.
- Within each group, notes stay newest first.
- "Modern" is read as a clean card layout, system fonts, soft colours and dark-mode support; no brand colours were given, so an indigo accent is used and is one CSS variable to change.
- No schema migration story is added (see Risks); dropping a stale local database is acceptable for this demo.

## Out of scope
- Editing or deleting notes, bulk "clear done", filtering, animations, a theme toggle (dark follows the OS setting), keyboard accessibility beyond a native checkbox with a visible focus style.
- EF migrations, automated tests, changes to `preview/` or `deploy/`.
