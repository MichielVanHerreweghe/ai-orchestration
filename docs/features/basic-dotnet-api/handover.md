# Handover: Basic .NET API in a Docker container

## What changed

**API** - `HelloApi/HelloEndpoint.cs` is a FastEndpoints endpoint (`GET /`, anonymous) returning the plain text `hello`; `Program.cs` registers FastEndpoints. No other endpoints, no controllers.

**Container** - `HelloApi/Dockerfile` is multi-stage: SDK 10.0 runs `dotnet publish`, aspnet 10.0 runs `HelloApi.dll`. `EXPOSE 8080` (aspnet image default).

**Project** - `HelloApi/HelloApi.csproj`: `Microsoft.NET.Sdk.Web`, `net10.0`, FastEndpoints 8.3.0. `HelloApi/.gitignore` ignores build output.

**Frontend / database** - none.

## Test flows

1. **Run in Docker**
   - Precondition: Docker available; no data or sign-in needed (no seed data applies).
   - Path: `docker build -t hello-api HelloApi`, then `docker run --rm -p 8080:8080 hello-api`, then `curl -i localhost:8080/`
   - Expected: `200 OK`, body `hello`
   - Fails as: image build error (likely wrong base image tag), connection refused, or a non-200 status

2. **Run without Docker**
   - Precondition: .NET SDK 10.
   - Path: `dotnet run --project HelloApi`, then `curl -i` the printed URL
   - Expected: `200 OK`, body `hello`
   - Fails as: build error or 404

## Technical notes

- New config: none. Migrations: none. Port 8080 is the image default.
- Kept under `HelloApi/` to avoid colliding with the root `Dockerfile` in PR #2.
- The Docker build and run were **not verified** (no Docker in the build environment); `dotnet run` was.

## Deviations from the plan

None; `Send.StringAsync` exists in FastEndpoints 8.3.0 and net10.0 is supported.

## Not included

Test project, solution file, `.dockerignore`, compose, CI image builds, health checks, HTTPS, Swagger, non-root or chiseled images (all out of scope in the plan).
