# Sahno Local Development

**Last updated:** 27 August 2026

## Prerequisites

- Docker Desktop using Linux containers.
- .NET 10 SDK.
- Node.js and pnpm for the mobile workspace.

## Local PostgreSQL

Sahno runs PostgreSQL 17 through the root `compose.yaml` file. Start it from the repository root:

```powershell
docker compose up -d
docker compose ps
```

The container listens on port `5432` internally and is published on Windows host port `5434`. The non-default host port avoids conflicts with locally installed PostgreSQL services.

Development connection details:

```text
Host: localhost
Port: 5434
Database: sahno
Username: sahno
Password: sahno_local_password
```

These credentials are for local development only. Production credentials must come from environment configuration or the hosting platform's secret management.

Docker stores the database in the named volume `sahno_sahno-postgres-data`, so ordinary container restarts preserve data.

## Run the API

With PostgreSQL healthy, run:

```powershell
dotnet run --project .\services\api\src\Sahno.Api --urls http://localhost:5062
```

The Development connection string is defined in `services/api/src/Sahno.Api/appsettings.Development.json`. The double-underscore environment variable form can override it when needed:

```powershell
$env:ConnectionStrings__Sahno = "Host=localhost;Port=5434;Database=sahno;Username=sahno;Password=sahno_local_password"
```

Remove a temporary override with:

```powershell
Remove-Item Env:ConnectionStrings__Sahno -ErrorAction SilentlyContinue
```

## Health endpoints

- `GET /api/health` is the liveness check. It proves the API process and routing are running.
- `GET /health/ready` is the readiness check. It opens a real EF Core/Npgsql connection to PostgreSQL.

Verify both endpoints:

```powershell
Invoke-RestMethod http://localhost:5062/api/health
Invoke-WebRequest http://localhost:5062/health/ready
```

A ready environment returns `Healthy` from both, with HTTP 200 from the readiness endpoint. HTTP 503 means the API is alive but cannot currently connect to PostgreSQL.

## Stop the environment

Stop the API with `Ctrl+C`, then stop the containers:

```powershell
docker compose down
```

This preserves the database volume. Use `docker compose down -v` only when intentionally deleting all local Sahno database data and recreating the database from scratch.

## Current persistence state

`SahnoDbContext` and PostgreSQL dependency injection are configured in `Sahno.Infrastructure`. No empty baseline migration is created. The first migration should be generated with the first real persisted domain model.

Integration tests currently validate API liveness without requiring a developer-run database. PostgreSQL-backed integration tests will use Testcontainers so CI receives an isolated real database.

## Port-conflict troubleshooting

If readiness reports password or connection failures while the Docker container itself is healthy, check whether another PostgreSQL installation owns the configured host port:

```powershell
Get-NetTCPConnection -LocalPort 5434 -State Listen
docker compose ps
```

Keep the host port in `compose.yaml` and `appsettings.Development.json` identical. Changing only one creates a connection mismatch.
