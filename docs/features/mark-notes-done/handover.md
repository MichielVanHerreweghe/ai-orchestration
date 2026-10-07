# Handover: Mark notes as done

Not verified end to end: no browser, SQL Server or Docker were available. `dotnet build demo/api` and `npm ci && npm run build` in `demo/web` are green; the flows below have not been clicked through, and the look is unverified visually.

## What changed
- **API** (`demo/api/Program.cs`): `Note.IsDone` (default false). `GET /api/notes` returns open notes first, done notes last, each group newest first. New `PATCH /api/notes/{id}` with `{ "isDone": bool }` returns the note, or 404 for an unknown id.
- **Frontend** (`demo/web`): the Vite/plain-JS page is replaced by an Angular 22 app, one standalone `App` component (`src/app/app.ts`), signals, `HttpClient`, no routing. Each note has a checkbox; ticking or unticking PATCHes and reloads the list (`app.ts:56-59`). Done notes are struck through (`<s>`). The heading reads `Notes (N open)`, computed from the list (`app.ts:37`), `0` until the first load.
- **Look** (`demo/web/src/styles.css`): My Little Pony theme: pastel pink/lavender/sky gradient background, frosted-glass card and rows, rainbow top stripe, gradient heading, sparkle in the card corner, pink pill for the open count, rounded font stack. Light only (no dark mode). Done rows use a darker purple plus strike-through. No official artwork, logos or fonts.
- **Build**: `demo/web/Dockerfile` copies `dist/web/browser`; `README.md` says Angular. `nginx.conf`, `preview/` and CI unchanged.
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

### 6. Look
- **Precondition**: flow 1 state, any modern browser; repeat at 360px width.
- **Path**: 1. Open the page. 2. Hover a note row. 3. Tab to the checkbox and the Add button.
- **Expected**: pastel gradient page, frosted card with rainbow stripe, readable heading and pink count pill, done note visibly muted but legible, row lifts on hover, visible focus outline, no horizontal scroll at 360px.
- **Failure signal**: unreadable text, missing stripe/blur (a plain white card is the fallback when blur is unsupported), overflow.

## Technical notes
- `PATCH /api/notes/{id}`, body `{ "isDone": true|false }`; 404 if the id is unknown.
- No migrations. A local database created before this change lacks `IsDone`, so `GET /api/notes` returns 500. Drop the `Notes` database. Previews start fresh and are unaffected.
- No new config keys.

## Deviations from the plan
- The plan was revised mid-way: the frontend was first plain JS with a modern stylesheet, then rebuilt in Angular, then restyled twice (neon frosted glass, finally My Little Pony). Behaviour is unchanged. The API is as first planned.
- Milestone 3 (look): done rows are dimmed with a darker muted colour plus strike-through rather than `opacity`, and the heading gradient uses deeper stops than the stripe, to keep contrast. Contrast was reasoned from the CSS, not measured.
- Milestone 2 already contained the `<label>` wrapper and `id="count"`, so milestone 3 was only the stylesheet move. Scaffolded `.editorconfig`, `.prettierrc`, `public/favicon.ico` were kept; `tsconfig.spec.json`, README and `.vscode` dropped.

## Not included
Editing or deleting notes, bulk clear, filtering, theme toggle, dark mode, keyboard accessibility beyond a native checkbox, Angular routing/SSR/Material, EF migrations, automated tests, `preview/` and `deploy/` changes.
