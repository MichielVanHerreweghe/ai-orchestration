# Handover: Basic Dockerfile

## What changed
- **Repo root**: PLANNED, to be rewritten after re-implementation: Go server in `main.go` plus a multi-stage `Dockerfile`.
- No API, frontend, database or config changes.

## Test flows

### 1. Build and serve
- **Precondition**: Docker installed; port 8000 free on the host. No data or roles needed.
- **Path**:
  1. `docker build -t test-server .`
  2. `docker run --rm -p 8000:8000 test-server`
  3. `curl -i localhost:8000/`
- **Expected**: build succeeds; response is `HTTP/1.0 200 OK` with body `ok`.
- **Failure signal**: build error pulling/running the base image, connection refused, or a directory listing / 404 instead of `ok`.

### 2. Host port already taken
- **Precondition**: something already listens on 8000.
- **Path**: run `docker run --rm -p 8080:8000 test-server`, then `curl -i localhost:8080/`.
- **Expected**: same `200` / `ok`.
- **Failure signal**: any other response.

## Technical notes
- Single file, no migrations, no env vars.
- Not built or run by the implementer: Docker was unavailable. The flows above are the first real verification.

## Deviations from the plan
None recorded.

## Not included
`.dockerignore`, compose, CI image builds, health check, non-root user, pinned base image tag.
