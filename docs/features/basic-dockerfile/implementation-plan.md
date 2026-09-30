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
    status: done
    commit: 0216970
---

# Implementation plan: Basic Dockerfile

## Context
Issue [#1](https://github.com/MichielVanHerreweghe/ai-orchestration/issues/1), in full: "We want a basic dockerfile that's able to run a small test http server." Feedback on the first plan: "I would rather use go", so the server is written in Go instead of Python.

## How it works today
The repo holds only an empty `README.md` (`README.md:1`) and one commit: no source, no Dockerfile, no docs, no language or framework. There is no precedent to follow, so the doc convention defaults to Shape A in English.

## Approach
Two files at the repo root:
- `main.go`: a stdlib `net/http` server, ~10 lines, answering `ok` on every request on `:8000`. No module file or dependencies needed: `go build main.go` works without `go.mod`.
- `Dockerfile`: multi-stage. Stage 1 `golang:alpine` runs `CGO_ENABLED=0 go build -o /server main.go`; stage 2 is `scratch`, copies the binary, exposes 8000, runs `/server`. Static binary means the final image is just the server.

Go needs a compile step, so unlike the Python version this adds one source file; that is the cost of the requested language.

## Alternatives rejected
- Python `http.server` (previous plan): superseded by the feedback.
- Single-stage `golang` image running `go run`: ships the whole toolchain for a 10-line server.
- `docker-compose.yml`: nobody asked for it.

## Milestones

### 1. Go test HTTP server in a Dockerfile
- **Files**: `main.go`, `Dockerfile` (replaces the Python one)
- **Acceptance**: `docker build` succeeds; `docker run -p 8000:8000` then `curl localhost:8000/` returns `ok` with status 200.
- **Tests**: the curl above. Neither Docker nor Go is available in this container, so it can only be verified by hand elsewhere.

Single milestone, small enough to just do directly. The code from the earlier Python milestone (`aa179db`) is replaced, not kept alongside.

## Risks
- Unverifiable here (no Docker, no Go toolchain). Early signal: build failure on first manual run.

## Open questions
Assumptions made (nobody to ask):
- "Small test http server" means a static server answering `200 ok`, not an app with routes. Go per feedback.
- Port 8000.
- Floating `golang:alpine` tag is acceptable for a test image.
- `scratch` final stage is fine since nothing needs a shell, certs or a healthcheck.

## Out of scope
`.dockerignore`, compose, CI image builds, health checks, non-root user, real application code.

## Deviations

### Milestone 1 — verification
Neither Go nor Docker is available in this container, so the build and the `curl` check were not run. The code is unverified beyond review.
