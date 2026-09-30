---
slug:       basic-dotnet-api
issue:      https://github.com/MichielVanHerreweghe/ai-orchestration/issues/3
branch:     feat/basic-dotnet-api
worktree:   /workspace/feat/basic-dotnet-api
convention: docs/features/<slug>/     # no docs on main; default Shape A
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
    title: Hello API with Dockerfile
    status: done
    commit: a803a9e
---

# Implementation plan: Basic .NET API in a Docker container

## Context
Issue [#3](https://github.com/MichielVanHerreweghe/ai-orchestration/issues/3), in full: "We want a basic .NET api running in a docker container it should just return hello." The only comment is the `/feature-plan` command.

## How it works today
`main` holds one empty `README.md` (`README.md:1`) and a single commit: no source, no project, no Dockerfile, no docs. There is no precedent to follow, so the doc convention defaults to Shape A in English.

An unmerged draft PR (#2, branch `feat/basic-dockerfile`) adds a Go test server with a `Dockerfile` and `main.go` at the repo root (`origin/feat/basic-dockerfile:Dockerfile`). It is a different feature, but it claims the root `Dockerfile` path, so this plan keeps everything under `HelloApi/` to avoid a merge conflict.

The container has .NET SDK 10.0.401 but no Docker, so the image build can't be verified here.

## Approach
One folder, `HelloApi/`, three files:
- `HelloApi.csproj`: `Microsoft.NET.Sdk.Web`, `net10.0`, nullable and implicit usings on.
- `Program.cs`: minimal API, `app.MapGet("/", () => "hello");`. No controllers, no extra packages.
- `Dockerfile`: multi-stage. Stage 1 `mcr.microsoft.com/dotnet/sdk:10.0` runs `dotnet publish -c Release -o /out`; stage 2 `mcr.microsoft.com/dotnet/aspnet:10.0` copies `/out` and runs `dotnet HelloApi.dll`. The aspnet image listens on port 8080 by default, so `EXPOSE 8080` and nothing else.

Build context is `HelloApi/` (`docker build -t hello-api HelloApi`), so no `.dockerignore` at the repo root is needed.

## Alternatives rejected
- Root-level `Dockerfile`: collides with PR #2.
- Controllers or `dotnet new webapi` template: Swagger, weather sample and launch settings for a one-line endpoint.
- Native AOT or `chiseled` images: smaller, but more moving parts than "just return hello" asks for.
- `docker-compose.yml`: nobody asked for it.

## Milestones

### 1. Hello API with Dockerfile
- **Files**: `HelloApi/HelloApi.csproj`, `HelloApi/Program.cs`, `HelloApi/Dockerfile`
- **Acceptance**: `dotnet build` succeeds; `dotnet run` then `curl localhost:<port>/` returns `hello` with status 200. `docker build -t hello-api HelloApi && docker run --rm -p 8080:8080 hello-api` then `curl localhost:8080/` returns `hello`.
- **Tests**: the curl above. The `dotnet run` path can be verified here; the Docker path cannot (no Docker) and must be marked unverified.

Single milestone: small enough to just do directly.

## Risks
- Docker path unverifiable here. Early signal: build failure on first manual run, most likely a wrong image tag.
- `net10.0` needs the 10.0 images to exist on MCR; if they don't, fall back to 9.0 in both places.

## Open questions
Assumptions made (nobody to ask):
- "Return hello" means `GET /` answers `200` with the plain text `hello`, not JSON.
- Latest .NET (10.0) is wanted, matching the SDK installed here.
- Port 8080 (the aspnet image default).
- Project lives in `HelloApi/` rather than the repo root, to avoid PR #2's files.

## Out of scope
Tests project, solution file, `.dockerignore`, compose, CI image builds, health checks, HTTPS, Swagger, non-default users or chiseled images.
