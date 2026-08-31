# Sahno Local Development

**Last updated:** 28 August 2026

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

## Database migrations

EF Core migrations live in `services/api/src/Sahno.Infrastructure/Migrations` and are created with the repository-pinned `dotnet-ef` tool (`services/api/dotnet-tools.json`). The API **never applies migrations automatically at startup**; applying them is always an explicit step.

Apply migrations to the local database (from `services/api`, with PostgreSQL running):

```powershell
dotnet tool restore
dotnet ef database update --project src/Sahno.Infrastructure --startup-project src/Sahno.Infrastructure
```

Create a new migration after changing the persisted model:

```powershell
dotnet ef migrations add <Name> --project src/Sahno.Infrastructure --startup-project src/Sahno.Infrastructure
```

Design-time commands use `SahnoDbContextFactory`, which reads `ConnectionStrings__Sahno` or falls back to the local development connection string above.

Integration tests start their own disposable PostgreSQL container (Testcontainers) and apply migrations programmatically; they never touch the developer database and require Docker to be running.

## Authentication (Auth0)

Auth0 proves identity (D-063). The mobile app signs in with Google, Apple, or a passwordless email one-time code through a unified **Continue** flow; the API validates Auth0-issued JWTs and maps the canonical subject to a Sahno user (`GET /api/me` creates the minimal local record on first authenticated contact).

Auth0 collapses linked login methods into one canonical subject, so one Sahno user maps to one subject. **Until an explicit account-linking flow is implemented, signing in through two unlinked Auth0 identities (for example Google and Apple with the same visible email) creates two separate Sahno users.** Identities are never linked automatically by matching email addresses; a secure linking and duplicate-user merge workflow is a later slice.

### Manual Auth0 Dashboard setup (you must do this yourself)

These steps need your Auth0 account and cannot be automated from the repository:

1. Create an Auth0 tenant (note the domain, e.g. `dev-abc123.au.auth0.com`).
2. Create a **Native** application. Note the **Client ID**. Under application settings:
   - **Allowed Callback URLs** and **Allowed Logout URLs** (both lists, exactly as written — lowercase, no trailing slash, replace `{domain}` with your tenant domain):

     ```text
     sahno://{domain}/android/app.sahno.mobile/callback
     sahno://{domain}/ios/app.sahno.mobile/callback
     ```

   - Enable **Refresh Token Rotation**.
   - Advanced Settings → Grant Types: enable **Authorization Code**, **Refresh Token**, and **Passwordless OTP**.
3. Create an **API** (Applications → APIs) with an identifier such as `https://api.sahno.dev` — this is the audience. Type the identifier by hand (a pasted invisible character once produced persistent “Service not found” errors). Enable **Allow Offline Access**. Creating an API also auto-creates a “(Test Application)” machine-to-machine client — harmless; ignore it.
4. **Authorize the application for the API**: newer Auth0 tenants enforce per-application API access. Under Applications → your app → **API Access**, grant **User-delegated Access** to the Sahno API (green tick). Without this, sign-in fails with “Service not found” or “Client is not authorized to access resource server”.
5. Connections for the application:
   - **google-oauth2**: Auth0's shared developer keys work for local testing; create a Google Cloud OAuth client before any production release.
   - **apple**: requires an Apple Developer account (Services ID, Sign in with Apple private key, Team ID). Until configured, the Continue with Apple button fails with a recoverable error.
   - **email** (Passwordless → Email): set to one-time code, and — like the API grant — enable it **for the application** on the connection’s Applications tab, or sign-in fails with “connection is disabled”. Auth0's built-in email sender is fine for local testing; production email uses the custom provider decided in D-066.

### Mobile configuration

Copy `apps/mobile/.env.example` to `.env` and fill in the Auth0 values (public client values — safe to ship, never secrets). `react-native-auth0` contains native code, so **the app no longer runs in Expo Go**; the project uses `expo-dev-client`, which gives the installed development app the Development Servers launcher (force-stop and reopen the app to reach it, then pick or type the Metro URL — useful whenever the computer’s Wi-Fi IP changes). Create a development build:

```powershell
cd apps/mobile
npx expo run:android
```

The Auth0 config plugin reads `EXPO_PUBLIC_AUTH0_DOMAIN` at build time for the native callback registration — after changing it, rebuild the development build. Without Auth0 configuration the app still boots, but authentication actions fail with a configuration message.

### API configuration

Set the Auth0 values with user secrets (from `services/api/src/Sahno.Api`) or environment variables — never in committed appsettings:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Auth0:Domain" "dev-abc123.au.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://sahno-api.local"
```

Without these the API logs a startup warning and rejects every authenticated request (health endpoints keep working).

### Verifying the flow

1. Start PostgreSQL, apply migrations, start the API (`--urls http://0.0.0.0:5062` so a phone can reach it), start the dev build.
2. Sign in with Google or an email code. The home screen should show your profile and a `Sahno account <id>` line — that id comes from `GET /api/me` and proves token validation and user mapping.
3. Kill and reopen the app: the session should restore from secure storage without showing sign-in.
4. Sign out: the app returns to the sign-in screen; reopening does not restore the session.

## Port-conflict troubleshooting

If readiness reports password or connection failures while the Docker container itself is healthy, check whether another PostgreSQL installation owns the configured host port:

```powershell
Get-NetTCPConnection -LocalPort 5434 -State Listen
docker compose ps
```

Keep the host port in `compose.yaml` and `appsettings.Development.json` identical. Changing only one creates a connection mismatch.
