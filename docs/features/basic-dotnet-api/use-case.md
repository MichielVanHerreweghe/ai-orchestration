# Basic .NET API in Docker

## Summary
A minimal ASP.NET Core API in `HelloApi/` that answers `hello`, packaged by a `Dockerfile` so it runs as a container.

## Actors
| Actor | Role |
|-------|------|
| Developer | Builds and runs the image locally or in CI |

## Preconditions
- Docker is installed.
- No database or seed data is needed.

## Main Flow
1. Developer runs `docker build -t hello-api HelloApi`.
2. Developer runs `docker run --rm -p 8080:8080 hello-api`.
3. Developer requests `http://localhost:8080/`.
4. The API responds `200` with the body `hello`.

## Alternate Flows
- Port 8080 is taken on the host: map another host port, e.g. `-p 9090:8080`.
