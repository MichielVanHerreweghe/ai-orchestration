# Basic Dockerfile

## Summary
A `Dockerfile` at the repo root that builds an image running a small test HTTP server written in Go, so the container setup can be exercised end to end.

## Actors
| Actor | Role |
|-------|------|
| Developer | Builds and runs the image locally or in CI |

## Preconditions
- Docker is installed.
- No database or seed data is needed.

## Main Flow
1. Developer runs `docker build -t test-server .`.
2. Developer runs `docker run --rm -p 8000:8000 test-server`.
3. Developer requests `http://localhost:8000/`.
4. The server responds `200` with the body `ok`.

## Alternate Flows
- Port 8000 is taken on the host: map another host port, e.g. `-p 8080:8000`.
