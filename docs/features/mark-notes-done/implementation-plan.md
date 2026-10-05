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
    title: Rebuild the web page in Angular with checkbox, strike-through and open count
    status: done
    commit: 413bdd4
  - id: 3
    title: Port the modern look to the Angular app
    status: done
    commit: e11aaac
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
- **Web (revised: Angular)**: replace the Vite/plain-JS app in `demo/web` with an Angular app (current stable CLI is 22.x, confirm at scaffold time). Scaffold with `ng new` (CSS, no SSR, no routing, `--skip-tests`, `--skip-git`) into `demo/web`, keeping `package.json`'s `name` of `web`. One standalone `App` component (inline template, signals) and nothing else: no services, no routing, no state library.
  - `provideHttpClient()` in `app.config.ts`. The component holds `notes = signal<Note[]>([])` and `open = computed(() => notes().filter(n => !n.isDone).length)`.
  - `load()` GETs `/api/notes`; the form submit POSTs then `load()`; a checkbox `change` PATCHes `{ isDone }` then `load()`. Same refresh-by-reload pattern as today, so server ordering stays the only ordering.
  - Template: heading `Notes <span id="count">({{ open() }} open)</span>`, the form, and `@for (note of notes(); track note.id)` rendering `<li [class.done]="note.isDone"><label><input type="checkbox" [checked]="note.isDone" (change)="toggle(note, $event)"> @if (note.isDone) { <s>{{ note.text }}</s> } @else { <span>{{ note.text }}</span> }</label></li>`. Form uses a template reference variable, no `FormsModule`.
  - Build output moves to `dist/web/browser`; the Dockerfile's copy line and nothing else in it changes. `nginx.conf` is unchanged (`try_files $uri /index.html` already serves an Angular SPA, `/api/` still proxies). `preview/` and the CI matrix entry (`context: demo/web`) are unchanged.
- **Look (kept, ported)**: the existing `style.css` is moved to `src/styles.css` (Angular's global stylesheet) with the same rules, so the look is identical: plain CSS, no new UI dependency, no web font, system font stack, indigo accent, dark variant, phone layout. Class names carry over into the Angular template.
- **Storage**: the `IsDone` column comes from `EnsureCreated`, defaulting to false.

## Alternatives rejected
- **Sort in the browser**: the server already sorts; putting the two-group order in the query keeps one place that decides order.
- **Optimistic UI update without reload**: more code for a demo whose `load()` is already the refresh path.
- **Adding EF migrations**: a new tool and files for a demo that uses `EnsureCreated` and throwaway preview databases. See Risks.
- **Keeping the Vite app and only adding Angular later / hybrid**: two frontends in one folder for one page. Replace it outright.
- **Angular Material / a UI kit**: the existing CSS already gives the look; a kit is a large new dependency for one list.
- **`FormsModule` / reactive forms**: one text input; a template reference variable is enough.
- **Separate `/done` route with POST/DELETE**: PATCH with a body is one route and covers both directions.

## Milestones

### 1. API stores and returns done state
- **Files**: `demo/api/Program.cs`
- **Acceptance**: `Note` has `IsDone` (default false). `GET /api/notes` returns open notes before done ones, each group newest first, and includes `isDone`. `PATCH /api/notes/{id}` with `{"isDone":true}` persists and returns the note; unknown id returns 404. `dotnet build demo/api` is green.
- **Tests**: no test project exists and adding one is out of proportion for this change; verify with `dotnet build` and, where SQL Server is available, curl (POST three notes, PATCH one, GET and check order, PATCH back).

### 2. Rebuild the web page in Angular with checkbox, strike-through and open count
- **Files**: `demo/web/` scaffold (`angular.json`, `tsconfig*.json`, `package.json`, `package-lock.json`, `src/index.html`, `src/main.ts`, `src/app/app.ts`, `src/app/app.config.ts`); deleted: `demo/web/index.html`, `demo/web/src/main.js`; `demo/web/Dockerfile` (copy path `dist/web/browser`); `README.md` ("Vite frontend" becomes "Angular frontend").
- **Acceptance**: the Angular app does what the vanilla page did: checkbox per note, tick/untick PATCHes and refreshes, done notes struck through and last, heading `Notes (N open)` updating on add, tick, untick and after reload. `npm ci && npm run build` in `demo/web` is green and `dist/web/browser/index.html` exists. `docker build demo/web` is not runnable here (no Docker); the Dockerfile change is checked by reading that the copied path matches the build output. No `vite` dependency remains.
- **Tests**: scaffold with `--skip-tests`; no automated tests, consistent with the earlier plan. Walk the three scenarios from the issue by hand where a browser is available.

### 3. Port the modern look to the Angular app
- **Files**: `demo/web/src/styles.css` (moved from `src/style.css`, registered in `angular.json`), `demo/web/src/app/app.ts` (class names, `<label>` wrapper in the template), `demo/web/src/index.html` (viewport meta, title)
- **Acceptance**: same as the previous milestone 3: centred card, styled form and rows, muted struck-through done rows, count pill, AA contrast, visible focus states, light and dark schemes, usable at 360px. The heading's accessible text still reads `Notes (N open)`. `npm run build` is green. The old `src/style.css` is gone.
- **Tests**: none automated. Screenshot light, dark and narrow if a browser is available; otherwise say the styling is unverified visually.

## Risks
- **`EnsureCreated` won't add the column to an existing database.** A long-lived local database created before this change makes `GET /api/notes` fail with an invalid column error. Signal: 500 on the notes list after upgrading. Fix for local use: drop the `Notes` database. Previews are unaffected (fresh database each time).
- **Cannot run SQL Server or Docker here**, so the end-to-end flow is unverified in this environment; the preview environment on the pull request is where it gets exercised.

- **Angular needs a toolchain this repo has not run yet.** The Dockerfile uses `node:24-alpine`; the pinned Angular version must support Node 24, checked from the CLI's `engines` at scaffold time (bump the Dockerfile node tag only if it does not). Signal: `npm ci` or `npm run build` failing in the image build on the preview.
- **Bigger bundle and slower image build** than the Vite page. Acceptable for a demo; it only affects preview build time.

## Open questions
Assumptions made without asking:
- Route shape is `PATCH /api/notes/{id}` with `{ "isDone": bool }`.
- Within each group, notes stay newest first.
- "Modern" is read as a clean card layout, system fonts, soft colours and dark-mode support; no brand colours were given, so an indigo accent is used and is one CSS variable to change.
- "Build this in Angular" is read as: the `demo/web` frontend is rewritten in Angular with identical behaviour and look. The API (milestone 1) is untouched. If Angular was meant for something else (for example a new separate app), say so.
- Latest stable Angular, standalone components and signals, no SSR, no routing.
- No schema migration story is added (see Risks); dropping a stale local database is acceptable for this demo.

## Out of scope
- Editing or deleting notes, bulk "clear done", filtering, animations, a theme toggle (dark follows the OS setting), keyboard accessibility beyond a native checkbox with a visible focus style.
- Angular routing, SSR, Material, forms module, unit tests, a dev-server proxy config, EF migrations, other automated tests, changes to `preview/` or `deploy/`.

## Deviations

### Milestone 2 — template and styles
The `<label>` wrapper and `id="count"` hook were written into the milestone 2 template already (the plan listed the wrapper under milestone 3), so milestone 3 is only the stylesheet move. Scaffolded files `.editorconfig`, `.prettierrc`, `public/favicon.ico` were kept; `tsconfig.spec.json`, README and `.vscode` were dropped. Not verified here: browser behaviour, `docker build`, and the API end to end (no browser, Docker or SQL Server).
