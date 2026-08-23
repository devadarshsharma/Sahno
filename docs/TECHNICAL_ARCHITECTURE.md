# Sahno Technical Architecture

**Status:** In definition  
**Last updated:** 23 August 2026

## Accepted mobile foundation

- React Native through Expo.
- TypeScript.
- Expo Router.
- Expo development builds.
- EAS Build for iOS and Android.
- Expo Go may be useful for trivial early experiments but is not the project's required runtime.

## Accepted backend direction

- ASP.NET Core REST API on .NET 10 LTS.
- Pragmatic Onion/Clean Architecture with separate Domain, Application, Infrastructure, API, and Contracts projects.
- Modular monolith with one primary API deployment.
- The API owns domain rules and server-side authorisation.
- Supabase is not the backend platform.
- Database, hosting, identity implementation, storage, and jobs remain open.

### Backend project dependencies

```text
Sahno.Domain
    ↑
Sahno.Application
    ↑
Sahno.Infrastructure
    ↑ composed by
Sahno.Api

Sahno.Contracts defines stable transport contracts where useful.
```

Infrastructure implements ports declared by Application. API is the composition root. Domain contains no framework or persistence dependencies.

## Accepted persistence

- PostgreSQL.
- Entity Framework Core 10 with Npgsql.
- Infrastructure-owned migrations.
- UUID identifiers and UTC timestamps.
- Optimistic concurrency for important edits.
- Database constraints alongside domain/application validation.
- No generic repository wrapper; use focused persistence abstractions only when they add domain value.

## Accepted identity architecture

- Auth0 handles email OTP, Google, Apple, access tokens, refresh tokens, and external identity linking.
- The React Native app obtains an Auth0 token; ASP.NET Core validates it.
- The API maps the Auth0 subject to an internal Sahno account.
- Sahno PostgreSQL data remains authoritative for organisations, memberships, roles, and permissions.
- Product authorisation is enforced by the .NET API and is not delegated to client state or generic identity-provider metadata.

## Accepted mobile data architecture

- TanStack Query for remote/server state.
- Zustand for small local UI and active-organisation state only.
- React Hook Form and Zod for mobile forms and immediate validation.
- Secure platform-backed credential storage for Auth0 material.
- No Redux in the MVP.
- Server-side validation remains authoritative.

## Accepted production infrastructure

- DigitalOcean App Platform hosts the containerised ASP.NET Core API.
- DigitalOcean Managed PostgreSQL hosts the production database.
- DigitalOcean Spaces stores uploaded files and organisation media.
- An App Platform Worker is introduced with scheduled reminders/durable jobs.
- Encrypted environment configuration stores deployment secrets initially.
- No Kubernetes for the MVP.
- No self-managed production PostgreSQL on a shared Droplet.
- Infrastructure adapters prevent DigitalOcean concerns from leaking into Domain or Application.

## Accepted notification architecture

- In-app notifications are persisted in PostgreSQL.
- Resend delivers transactional product email.
- Auth0 production authentication email uses a configured custom provider.
- Domain changes and Outbox messages commit atomically.
- A Worker processes Outbox messages asynchronously with retries and idempotency.
- Expo push can be added later as another delivery adapter.
- Sahno owns non-critical notification preferences.

## Accepted realtime architecture

- ASP.NET Core SignalR provides realtime discussion, availability progress, in-app notification, and active-screen Booking/Event updates.
- REST remains authoritative for durable reads and writes.
- Reconnection invalidates/refetches relevant TanStack Query data.
- SignalR channels/groups enforce organisation and Engagement authorisation server-side.

## Accepted connectivity model

- Online-first application with persisted read cache for recently loaded Event/day-of information.
- Clear offline indication and locally preserved form drafts where practical.
- Sensitive writes require API confirmation.
- Failed writes expose Retry; no general offline mutation queue in the MVP.
- Optimistic updates are limited and reversible.
- Concurrency conflicts are explicit and never silently overwrite newer data.

## Accepted diagnostics

- Serilog structured logs for API and Worker.
- Correlation IDs across mobile, API, and background processing.
- Sentry for React Native crashes and unhandled backend exceptions.
- DigitalOcean platform logs.
- API liveness and database-readiness health endpoints.
- Sensitive tokens, financial data, private notes, contacts, message bodies, and sensitive request/response bodies are excluded from logs.
- Product analytics are deferred until an explicit privacy-safe event schema exists.

## Accepted test strategy

### Backend

- xUnit Domain/Application unit tests.
- WebApplicationFactory API integration tests.
- Testcontainers with PostgreSQL.
- Onion dependency architecture tests.
- Explicit organisation-isolation and permission coverage.
- Engagement state-machine transition coverage.

### Mobile

- React Native Testing Library.
- Unit tests for state and form validation.
- Maestro critical-journey smoke tests on iOS and Android.
- Controlled API test responses or a dedicated test environment.

Risk-based coverage is prioritised over a numerical coverage target.

## Accepted repository structure

Sahno is a monorepo:

```text
apps/mobile                  Expo React Native application
services/api/src             .NET production projects
services/api/tests           .NET test projects
packages/api-client          OpenAPI-generated TypeScript client
infra                        Local and DigitalOcean configuration
docs                         Project source of truth
```

pnpm manages JavaScript workspaces. API contract generation is part of the repository workflow so backend and mobile changes remain atomic.

## Accepted environments and delivery

- GitHub with protected `main` and pull-request checks.
- Local, Staging, and Production environments.
- Docker Compose for local dependencies.
- Automatic Staging deployment from accepted `main` changes.
- Manual approval for Production.
- Migrations run as controlled deployment work, not automatically at production API startup.
- EAS Development, Preview, and Production profiles.
- Preview targets Staging; Production targets Production.
- Secrets are environment-specific and excluded from Git.

CI/CD is implemented incrementally as a guided learning exercise. The founder performs meaningful setup/runs and learns workflow triggers, jobs, permissions, secrets, artifacts, caches, environments, approvals, deployments, and troubleshooting rather than receiving an opaque finished pipeline.

## Architecture principles

- Organisation data must be strictly tenant-scoped.
- Authorisation must be enforced server-side, not only hidden in the mobile interface.
- Financial, customer, and private Member data require explicit access boundaries.
- The Engagement lifecycle and activity history must be durable and auditable.
- Mobile workflows must tolerate ordinary intermittent connectivity without silently losing user input.
- Prefer a small operational stack suitable for a side project and Customer Zero pilot.
- Avoid custom infrastructure unless it provides clear product value.

## Architecture status

The high-level MVP architecture is accepted. Package-level implementation details may still be refined during scaffolding, but material deviations require a new decision entry.
