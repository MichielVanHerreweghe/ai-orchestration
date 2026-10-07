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
pr:         6
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
    title: My Little Pony look
    status: done
    commit: 3d47fac
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
- **Look (revised: My Little Pony)**: feedback on the plan asked for a My Little Pony theme, replacing the neon look (which was never implemented). Only `demo/web/src/styles.css` (and the `<head>` of `index.html`) change; the template keeps its markup and class names, apart from decorative emoji in the heading if wanted (see below). Plain CSS, no UI dependency, no web font, **no image or logo assets**.
  - **Palette**: pastel pony colours as CSS variables: lavender/purple primary (Twilight-ish `#9b6bd1`), hot pink secondary (`#ff7eb6`), sky blue and mint as extra stops. One variable block, so the palette stays a single place to change.
  - **Backdrop**: `body` gets a soft pastel gradient (pink to lavender to sky blue) with a few blurred radial blobs; light theme, `color-scheme: light`. The OS dark setting does not change it.
  - **Glass kept**: `main` and the note rows stay translucent white (`rgb(255 255 255 / .55-.7)`) with `backdrop-filter: blur(12px)` (with `-webkit-` prefix), a 1px white border and generously rounded corners, over the pastel gradient. `@supports not (backdrop-filter: blur(1px))` raises the fill opacity.
  - **Pony touches, all CSS/Unicode**: heading in a rounded system font stack (`ui-rounded, "Comic Sans MS", system-ui, sans-serif`) with a rainbow gradient text fill (`background-clip: text`, with a solid fallback colour); a rainbow stripe (`linear-gradient` of six pastel colours) as the card's top border; the count pill in pink; Add button in a purple-to-pink gradient; checked checkbox in pink via `accent-color`; decorative sparkles/stars via `::before`/`::after` pseudo-elements (`aria-hidden` by nature, content is not read as text). Done rows are dimmed and struck through as before.
  - **Not used**: character names, cutie-mark artwork, logos or show imagery. Those are Hasbro's trademarks/copyright; the theme is the colours, rainbow and sparkle style only.
  - **Accessibility**: body and muted text keep AA contrast (4.5:1) against the lightest and darkest parts of the gradient behind the glass (dark purple text, not pastel on pastel); focus ring is a solid purple outline; gradient text has a solid fallback.
  - **Motion**: only a short hover `transition`, wrapped in `@media (prefers-reduced-motion: no-preference)`.
- **Storage**: the `IsDone` column comes from `EnsureCreated`, defaulting to false.

## Alternatives rejected
- **Sort in the browser**: the server already sorts; putting the two-group order in the query keeps one place that decides order.
- **Optimistic UI update without reload**: more code for a demo whose `load()` is already the refresh path.
- **Adding EF migrations**: a new tool and files for a demo that uses `EnsureCreated` and throwaway preview databases. See Risks.
- **Keeping the Vite app and only adding Angular later / hybrid**: two frontends in one folder for one page. Replace it outright.
- **Official My Little Pony artwork, logo or font**: licensed/trademarked material, and an image dependency for one list. The pastel/rainbow style alone carries the theme.
- **A CSS framework / glassmorphism library or web font**: a handful of CSS variables, gradients and `backdrop-filter` give it; no dependency.
- **Keeping a dark scheme alongside the pastel one**: doubles the palette work for a theme that reads light by nature. Dropped.
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

### 3. My Little Pony look
- **Status note**: milestone 3 was implemented earlier as the indigo card (`e11aaac`) and replanned as neon without being built. The code on the branch must become the pony look; rework `src/styles.css` rather than skipping it as done.
- **Files**: `demo/web/src/styles.css` (rewritten), `demo/web/src/index.html` (`<meta name="color-scheme" content="light">`, `theme-color` pink). `app.ts` template stays unless a class hook or decorative heading emoji is genuinely needed.
- **Acceptance**: pastel pink/lavender/sky gradient backdrop; `main` and note rows are translucent frosted glass with white borders and round corners; rainbow top stripe on the card and rainbow gradient heading text with solid fallback; pink count pill, purple-pink Add button, pink checked checkbox; sparkle decorations via pseudo-elements only; done rows dimmed and struck through; `@supports not (backdrop-filter…)` fallback; AA text contrast; visible focus states; usable at 360px; reduced-motion respected. The heading's accessible text still reads `Notes (N open)`. No dark scheme and no image/logo assets. `npm run build` is green.
- **Tests**: none automated. Screenshot at desktop and 360px if a browser is available; otherwise say the look is unverified visually.

## Risks
- **`EnsureCreated` won't add the column to an existing database.** A long-lived local database created before this change makes `GET /api/notes` fail with an invalid column error. Signal: 500 on the notes list after upgrading. Fix for local use: drop the `Notes` database. Previews are unaffected (fresh database each time).
- **Cannot run SQL Server or Docker here**, so the end-to-end flow is unverified in this environment; the preview environment on the pull request is where it gets exercised.

- **Angular needs a toolchain this repo has not run yet.** The Dockerfile uses `node:24-alpine`; the pinned Angular version must support Node 24, checked from the CLI's `engines` at scaffold time (bump the Dockerfile node tag only if it does not). Signal: `npm ci` or `npm run build` failing in the image build on the preview.
- **Trademark/copyright.** Signal: anyone adding pony characters or logos later. Mitigation: the plan keeps to colours, rainbow and sparkles only.
- **`backdrop-filter` is unsupported or costly on old or low-end browsers.** Signal: panels look flat or text is hard to read over the gradient. Mitigation: the `@supports` fallback with a more opaque fill. Blur is applied to two surfaces only (card and rows) to keep paint cost down.
- **Pastel on pastel hurts contrast.** Signal: muted/done text under 4.5:1 over the gradient. Mitigation: dark purple text, and check the muted colour against the lightest and darkest blended background.
- **Bigger bundle and slower image build** than the Vite page. Acceptable for a demo; it only affects preview build time.

## Open questions
Assumptions made without asking:
- Route shape is `PATCH /api/notes/{id}` with `{ "isDone": bool }`.
- Within each group, notes stay newest first.
- "Theme to be My Little Pony" is read as: a pastel rainbow look (pink, lavender, sky blue, mint), sparkles and rounded type, applied to the existing page. No official artwork, logos or character names (trademarks), and no assets to download.
- The frosted-glass look from the previous feedback is kept as the card style, recoloured pastel; the neon look is dropped. If the glass was meant to go too, say so and it becomes a flat pastel card.
- The look is light only; the OS dark setting no longer changes it.
- "Build this in Angular" is read as: the `demo/web` frontend is rewritten in Angular with identical behaviour and look. The API (milestone 1) is untouched. If Angular was meant for something else (for example a new separate app), say so.
- Latest stable Angular, standalone components and signals, no SSR, no routing.
- No schema migration story is added (see Risks); dropping a stale local database is acceptable for this demo.

## Out of scope
- Editing or deleting notes, bulk "clear done", filtering, animations, a theme toggle or dark scheme, keyboard accessibility beyond a native checkbox with a visible focus style.
- Angular routing, SSR, Material, forms module, unit tests, a dev-server proxy config, EF migrations, other automated tests, changes to `preview/` or `deploy/`.

## Revision log
- Feedback on the plan: "We want the application to look neon-futuristic with frosted glass". Milestone 3 reset to `todo`; the earlier indigo/light-dark styling is superseded. Milestones 1 and 2 untouched. Approval reset: re-approve with `/feature-implement`.

- Feedback on the plan: "We want the theme to be my little pony". Milestone 3 retitled and still `todo` (the neon look was never built). Neon/dark palette superseded by a pastel pony palette; frosted glass kept. Milestones 1 and 2 untouched. Approval stays reset: re-approve with `/feature-implement`.

## Deviations

### Milestone 2 — template and styles
The `<label>` wrapper and `id="count"` hook were written into the milestone 2 template already (the plan listed the wrapper under milestone 3), so milestone 3 is only the stylesheet move. Scaffolded files `.editorconfig`, `.prettierrc`, `public/favicon.ico` were kept; `tsconfig.spec.json`, README and `.vscode` were dropped. Not verified here: browser behaviour, `docker build`, and the API end to end (no browser, Docker or SQL Server).

### Milestone 3 — dimming and gradient text
Done rows are dimmed by the darker-purple muted colour plus strike-through, not `opacity`, to keep AA contrast. The heading gradient uses deeper stops than the pastel stripe so the large text keeps 3:1. Not verified visually (no browser); build only.
