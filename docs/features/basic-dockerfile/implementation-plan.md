---
slug:       basic-dockerfile
issue:      https://github.com/MichielVanHerreweghe/ai-orchestration/issues/1
branch:     feat/basic-dockerfile
worktree:   /workspace/feat/basic-dockerfile
convention: docs/features/<slug>/     # no prior docs; default Shape A
language:   en
approved:   true
database:   null
seeded:     false
pr:         null
artifacts:
  plan:     null
  handover: null
milestones:
  - id: 1
    title: Dockerfile serving a test HTTP server
    status: in_progress
    commit: null
---

# Implementation plan: Basic Dockerfile

## Context
Issue [#1](https://github.com/MichielVanHerreweghe/ai-orchestration/issues/1), in full: "We want a basic dockerfile that's able to run a small test http server." No comments add requirements.

## How it works today
The repo holds only an empty `README.md` (`README.md:1`) and one commit: no source, no Dockerfile, no docs, no language or framework. There is no precedent to follow, so the doc convention defaults to Shape A in English.

## Approach
One `Dockerfile` at the repo root: `python:3-alpine` base, write an `index.html` containing `ok`, expose 8000, run `python -m http.server 8000`. The Python stdlib server means no application code, no dependencies and no build step.

## Alternatives rejected
- Node/Go/other custom server: needs source files and a dependency story for a throwaway server.
- `busybox httpd` / nginx: fine, but python's stdlib is equally small and easier to read.
- `docker-compose.yml`: nobody asked for it.

## Milestones

### 1. Dockerfile serving a test HTTP server
- **Files**: `Dockerfile`
- **Acceptance**: `docker build` succeeds; `docker run -p 8000:8000` then `curl localhost:8000/` returns `ok` with status 200.
- **Tests**: the curl above. Docker is not available in this container, so it can only be verified by hand elsewhere.

This is a single-milestone feature, small enough to just do directly.

## Risks
- Unverifiable here (no Docker). Early signal: build failure on first manual run.

## Open questions
Assumptions made (nobody to ask):
- "Small test http server" means a static server answering `200 ok`, not an app with routes. Python because it needs no code.
- Port 8000.
- Floating `python:3-alpine` tag is acceptable for a test image.

## Out of scope
`.dockerignore`, compose, CI image builds, health checks, non-root user, real application code.
