# Sahno Test Deployment

**Status:** Working guide
**Last updated:** 28 September 2026
**Scope:** TEST environment only. Production is separate (D-065, D-081).

```text
Sahno app (EAS preview build, iPhone + Android)
        │  HTTPS (REST)  +  WSS (SignalR /hubs/live)
        ▼
Render web service "sahno-api-test"  ── Docker image from services/api/Dockerfile
        │  Npgsql, SSL
        ▼
Supabase PostgreSQL (used only as hosted Postgres — no Supabase SDK, no Data API)

Push:  API outbox ─► Expo push service ─► FCM (Android) / APNs (iOS)
Auth:  Auth0 (same tenant and API as local development)
```

Nothing in the code knows about Render or Supabase. The API reads a connection
string and a `PORT`; the app reads one URL. Moving hosts is a configuration
change (see [Moving to production infrastructure](#moving-to-production-infrastructure)).

---

## 1. Environments at a glance

| | Local | Test | Production |
|---|---|---|---|
| API | `dotnet run` on your PC | Render `sahno-api-test` | DigitalOcean App Platform (D-065) |
| Database | Docker Compose Postgres (`compose.yaml`) | Supabase | DigitalOcean Managed PostgreSQL |
| API URL in app | `apps/mobile/.env` (`http://192.168.x.x:5062`) | EAS env `preview` (`https://…onrender.com`) | EAS env `production` |
| App build | `npx expo run:android` / EAS `development` | EAS `preview` | EAS `production` |
| Deployed from | — | `test` branch (auto) | separate, manual (future) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Staging` | `Production` |

---

## 2. Supabase setup

1. Create a project. **Region: Southeast Asia (Singapore)** so it sits next to
   Render's Singapore region; every EF query is a round trip, and
   Singapore↔Sydney would add ~90 ms to each one.
2. Save the database password in your password manager.
3. **Turn off the Data API** (Project Settings → Data API → disable, or remove
   `public` from exposed schemas). Sahno's tables live in `public` without
   row-level security, because the API — not Supabase — enforces access. With
   the Data API on, anyone holding the project's public anon key could query
   them over REST. Sahno never uses the Data API, so there is nothing to lose.
4. Get the connection string: **Connect → Session pooler**. Use the session
   pooler (port 5432 on `*.pooler.supabase.com`), not:
   - the *direct* connection — IPv6-only, and Render has no outbound IPv6;
   - the *transaction* pooler (port 6543) — it breaks Npgsql's prepared
     statements and EF's transactions.
5. Supabase shows a `postgresql://` URI. **Npgsql does not accept URIs** —
   rewrite it in keyword form:

   ```text
   Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require;Maximum Pool Size=10;Keepalive=30
   ```

   - `SSL Mode=Require` — Supabase requires TLS. (Npgsql encrypts; it does not
     pin Supabase's certificate. `VerifyFull` with the Supabase CA is the
     stricter option if you ever want it.)
   - `Maximum Pool Size=10` — Supabase's pooler limits clients per project;
     Npgsql's default of 100 could exhaust it.
   - Copy the exact host from the dashboard (it may be `aws-1-…`).
   - If the password contains `;` or `'`, wrap it: `Password='p;ss'`.

This string is the only secret the database needs. It goes into Render and
into your terminal for migrations — nowhere else.

---

## 3. Database migrations

The API **never migrates at startup** (LOCAL_DEVELOPMENT.md). Migrations are an
explicit step you run from your PC with the repo's pinned `dotnet-ef`. There are
14 migrations, `InitialUsers` → `AddPushNotifications`; they are applied as they
are — no replacement "initial" migration is needed. They use only core
PostgreSQL features (`gen_random_uuid()` is built in from PG 13; Supabase runs 15+).

PowerShell, from `services/api`:

```powershell
dotnet tool restore
$env:ConnectionStrings__Sahno = "Host=...pooler.supabase.com;Port=5432;...;SSL Mode=Require"

# 1. Prove you are pointed at Supabase: on a new project every migration
#    should say (Pending). If they show as applied, you are on the LOCAL
#    database — the design-time factory falls back to it when the variable
#    is not set in THIS terminal.
dotnet ef migrations list --project src/Sahno.Infrastructure --startup-project src/Sahno.Infrastructure

# 2. Optional: see exactly what will run.
dotnet ef migrations script --idempotent --project src/Sahno.Infrastructure --startup-project src/Sahno.Infrastructure -o $env:TEMP\sahno-test.sql

# 3. Apply.
dotnet ef database update --project src/Sahno.Infrastructure --startup-project src/Sahno.Infrastructure

# 4. Do not leave the secret in the shell.
Remove-Item Env:ConnectionStrings__Sahno
```

`database update` only applies pending migrations inside a transaction each;
it never drops or recreates the database. Re-run the same steps whenever a
branch that adds a migration is deployed to `test` — **before** the deploy
finishes, or the new code will hit an old schema.

---

## 4. Render setup

The repo has a Blueprint, [`render.yaml`](../render.yaml), describing one Docker
web service:

| Setting | Value |
|---|---|
| Runtime | Docker, `services/api/Dockerfile`, context `services/api` |
| Build / start command | none — the Dockerfile builds; its `ENTRYPOINT` starts |
| Port | Render's `PORT` (the API binds `0.0.0.0:$PORT`) |
| Branch | `test`, auto-deploy on commit, only when `services/api/**` changes |
| Health check | `GET /health` |
| Region / plan | Singapore / Free |

Create it: Render dashboard → **New → Blueprint** → pick the GitHub repo →
Render reads `render.yaml` and asks for the `sync: false` values.

### Environment variables

| Variable | Value | Secret? |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Staging` (set by Blueprint) | no |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` (set by Blueprint) | no |
| `OpenApi__Enabled` | `true` (set by Blueprint) | no |
| `ConnectionStrings__Sahno` | Supabase keyword string (§2) | **yes** |
| `Auth0__Domain` | e.g. `dev-xxxx.au.auth0.com` — same as your user secrets | no, but keep out of Git |
| `Auth0__Audience` | e.g. `https://sahno-api.local` — same as your user secrets | no |
| `Email__ResendApiKey` | optional; unset = emails are logged, not sent | **yes** |
| `Push__ExpoAccessToken` | optional; only with Expo "enhanced push security" | **yes** |
| `Cors__AllowedOrigins__0` | optional; only if a *browser* app must call the API | no |

`PORT` is provided by Render; do not set it.

### What the API does behind Render

- **HTTPS:** Render terminates TLS and forwards plain HTTP with
  `X-Forwarded-Proto: https`. `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` makes
  ASP.NET Core trust that, so the request is treated as HTTPS. Render itself
  redirects `http://` visitors to `https://`.
- **WebSockets:** Render web services support them with no settings. SignalR
  uses WebSockets only (the app skips negotiation).
- **Health:** `/health` = process alive (no DB) — used by Render so a Supabase
  blip does not restart the container. `/health/ready` = can reach PostgreSQL
  — use it yourself after deploying. Both return only `Healthy`/`Unhealthy`.
- **OpenAPI:** `https://<service>.onrender.com/openapi/v1.json`. Endpoint
  shapes only; no secrets. (There is no Swagger UI package; paste the URL into
  any OpenAPI viewer, or use `services/api/src/Sahno.Api/Sahno.Api.http`.)

### Free plan caveats

A free instance **sleeps after 15 minutes without traffic** and takes up to a
minute to wake. Consequences for testing:

- The first request after a quiet spell is slow. The app retries; SignalR keeps
  trying every 30 s until it connects.
- The outbox worker (which sends pushes and emails) only runs while awake. A
  push caused by an action — Phone A assigning a job — is fine, because that
  action woke the server. Time-based sends (availability reminders) wait until
  something wakes it.
- For multi-day testing, switch the service to the cheapest paid instance in
  the dashboard (or `plan:` in `render.yaml`) so it never sleeps.

---

## 5. Deploying updates

`test` is the deploy branch. Anything pushed there that touches
`services/api/**` is built and deployed; CI (`.github/workflows/ci.yml`) runs on
the same push.

```bash
git switch test
git merge --ff-only main
git push origin test
```

To trial a feature branch before it reaches `main`: `git switch test`,
`git merge <branch>`, push. If the branch adds a migration, apply it to Supabase
first (§3).

Manual redeploy: Render dashboard → service → **Manual Deploy → Deploy latest
commit**. Roll back: **Events** → pick an earlier deploy → **Rollback**. (A
rollback does not undo migrations.)

## 6. Logs

Render dashboard → `sahno-api-test` → **Logs** (live tail, searchable). Useful
lines:

| Log text | Meaning |
|---|---|
| `Now listening on: http://0.0.0.0:10000` | started and bound correctly |
| `Auth0 is not configured` | `Auth0__Domain`/`Auth0__Audience` missing — every signed-in call will 401 |
| `Live connection joined organisation …` | a phone's SignalR connection succeeded |
| `Live connection refused: user … is not in organisation …` | connected with a wrong/old organisation id |
| `Outbox pass failed` | usually the database is unreachable — check `/health/ready` |
| `Failed to connect to …pooler.supabase.com` | wrong host/port/password, or the Supabase project is paused |

Secrets are never logged: connection errors name the host, not the password.

---

## 7. Mobile app configuration

The app has exactly one address setting, `EXPO_PUBLIC_API_URL`
(`apps/mobile/src/config/environment.ts`). The REST client, health check and
SignalR hub (`environment.liveHubUrl`) all derive from it; push-token
registration is an ordinary REST call, so it goes to whichever API the build
points at.

- **Local:** `apps/mobile/.env`.
- **EAS builds:** `.env` is gitignored and never reaches the EAS build server.
  Each profile in `apps/mobile/eas.json` names an EAS environment
  (`development`, `preview`, `production`); the variables come from there.

Test builds use the **preview** profile. Create its variables once (from
`apps/mobile`):

```bash
eas env:create --environment preview --name EXPO_PUBLIC_API_URL --value https://sahno-api-test.onrender.com --visibility plaintext
eas env:create --environment preview --name EXPO_PUBLIC_AUTH0_DOMAIN --value <domain> --visibility plaintext
eas env:create --environment preview --name EXPO_PUBLIC_AUTH0_CLIENT_ID --value <client id> --visibility plaintext
eas env:create --environment preview --name EXPO_PUBLIC_AUTH0_AUDIENCE --value <audience> --visibility plaintext
```

plus `GOOGLE_SERVICES_JSON` (file variable) in `preview` — see Android below.
`eas env:list --environment preview` shows what is set.

Android cleartext HTTP is now allowed **only** when the build's API URL starts
with `http://` (`app.config.ts`). A preview build for Render is HTTPS-only.

---

## 8. SignalR

- Hub: `wss://<service>.onrender.com/hubs/live?organisationId=<id>`.
- Auth: the Auth0 access token in `access_token` (accepted for the hub path
  only). A fresh token is requested for every connect and reconnect.
- CORS does not apply — the app is not a browser.
- One connection per signed-in session, for the active organisation, only
  while the app is in the foreground. Switching organisation or signing out
  replaces it; backgrounding closes it (push takes over, D-080).
- **Reconnection:** retries at 0 s, 2 s, 5 s, 10 s, then every 30 s, for as
  long as the app is open — through lost signal, Wi-Fi↔mobile handover, an API
  redeploy, or a sleeping free instance. A first connect that fails is retried
  on the same schedule. After any reconnect the organisation's data is
  refetched, since changes may have been missed.
- Single instance only. If the test (or production) API ever runs on more
  than one instance, SignalR needs a backplane (e.g. Redis) — groups live in
  process memory.

## 9. Push notifications

### How it flows (D-080)

1. After sign-in the app asks permission, gets an Expo push token, and
   `POST /api/me/push-devices` to the API the build points at.
2. An action (e.g. assigning a job) writes the notification and one outbox row
   per recipient phone in the same transaction.
3. The outbox worker sends each row to Expo, which delivers via FCM/APNs.
4. **Foreground:** the SignalR `notification` event shows Sahno's own banner;
   the OS shows nothing; a push for the same id is ignored.
   **Background / locked / terminated:** the OS shows the push.
5. **Tap** (banner, tray, lock screen, or cold start) opens `route` from the
   payload — e.g. `/engagement/<id>/jobs` for a job — switching organisation
   first if needed.

Credentials for FCM and APNs live **on the EAS project**, never in the API or
Git. The API only calls Expo's public endpoint.

### Android requirements

- A Firebase project with an Android app `app.sahno.mobile`.
- `google-services.json` uploaded to EAS as a **file** environment variable
  named `GOOGLE_SERVICES_JSON` in the `preview` environment (the config reads
  it — `app.config.ts`).
- The FCM V1 **service account key** uploaded to EAS:
  `eas credentials` → Android → Google Service Account → *for FCM V1*.
- Android 13+: the app asks for notification permission on first sign-in.
- Lock screen: notifications use the `default` channel at MAX importance and
  lock-screen visibility PRIVATE, so content is hidden only if the phone is set
  to hide sensitive notifications.
- Some manufacturers (Xiaomi, Oppo, Huawei, Samsung "sleeping apps") delay
  pushes to apps they consider unused. If a terminated-app push is late, set
  Sahno's battery usage to *Unrestricted*.

### iOS requirements

- **Paid Apple Developer Program membership** — push and installing a build on
  a real iPhone outside TestFlight need it.
- Register the test iPhone for internal distribution: `eas device:create`,
  open the link on the iPhone, install the profile.
- `eas build -p ios --profile preview` — when asked, let EAS generate the
  distribution certificate, provisioning profile and **push key (APNs)**.
- Accept the notification prompt on first sign-in. If declined: Settings →
  Sahno → Notifications.
- Lock-screen display follows Settings → Notifications → Show Previews.
- Force-quitting Sahno by swiping it away does **not** stop pushes on iOS; a
  *disabled* notification permission or Focus mode does.

---

## 10. Two-phone test

The data model supports this without any test-only code.

| | Phone A — Organiser | Phone B — Member |
|---|---|---|
| Account | Auth0 sign-in, account 1 | Auth0 sign-in, **a different** account |

1. Install the preview build on both. Sign in (Google or email code) with
   different accounts; allow notifications on both.
2. **A:** create an organisation (becomes Organiser). People → invite → share
   the code.
3. **B:** Join an organisation → enter code. (A sees B appear.)
4. **A:** create an event. On its **People** page select B for the lineup —
   a job can only go to someone who can see the event.
5. **A:** event → **Jobs** → add a job, assign to B.
6. **B** should get *"You are down for: <job>"*. Repeat step 5 with B:
   - **open** → Sahno banner at the top, no system notification;
   - **backgrounded** → system notification;
   - **locked** → notification on the lock screen;
   - **terminated** (swiped away) → system notification.
7. **B:** tap the notification → Sahno opens on that event's **Jobs** page.

Verification from the API side (Supabase → SQL editor):

```sql
select platform, device_name, disabled_at_utc, disabled_reason from push_devices;
select channel, created_at_utc, sent_at_utc, attempt_count, last_error
from outbox_messages order by created_at_utc desc limit 20;
```

---

## 11. Troubleshooting

### SignalR

| Symptom | Check |
|---|---|
| No live updates, no in-app banner | Render logs for `Live connection joined`. None → the socket never opened. |
| 401 on `/hubs/live` | `Auth0__Domain`/`Auth0__Audience` on Render must match the app's `EXPO_PUBLIC_AUTH0_*` values |
| `Live connection refused` | the user is not a member of that organisation (e.g. left, or stale active org) — switch organisation |
| Works on Wi-Fi, not on mobile data | the build is pointed at an `http://` LAN address; rebuild with the Render URL |
| Connects after ~1 min only | free instance was asleep; expected |
| `/health/ready` is 503 | database unreachable → nothing works; see Supabase below |

### Push

| Symptom | Check |
|---|---|
| No row in `push_devices` for the phone | permission denied, a simulator/emulator (push needs a real device), or the build points at another API. On Android, "Default FirebaseApp is not initialized" = `GOOGLE_SERVICES_JSON` missing from the build's EAS environment. |
| Row in `outbox_messages` with `sent_at_utc` null and `last_error` set | the message is Expo's reason. `InvalidCredentials` = FCM key / APNs key missing on EAS. `DeviceNotRegistered` = app reinstalled; open it once to re-register. |
| No outbox row at all | recipient is the actor (nobody is pushed about their own action), or the kind is bell-only (someone answered/joined) |
| `sent_at_utc` set but nothing on the phone | Expo accepted it; check the phone: notification permission, Focus/Do Not Disturb, Android battery restrictions |
| Received twice while open | should not happen (dedupe by id); report with the time |
| Tap opens Home instead of the event | the user is no longer in that organisation (by design) |

Expo's own tool, <https://expo.dev/notifications>, can send a test push to a
token from `push_devices` — it isolates "the phone/credentials" from "the API".

### EAS build

| Symptom | Check |
|---|---|
| `package.json does not exist in …/build/apps/mobile`, with `tar: … Cannot mkdir: Permission denied` in *Prepare project* | A folder on your Windows disk has the **Read-only** attribute. Windows ignores it on folders; the EAS upload turns it into "no write permission" and Linux cannot unpack into it. Clear it (PowerShell, repo root): `Get-ChildItem -Directory -Recurse -Force \| Where-Object { $_.FullName -notmatch '\\(node_modules\|\.git)(\\\|$)' } \| ForEach-Object { $_.Attributes = $_.Attributes -band (-bnot [IO.FileAttributes]::ReadOnly) }` |
| Upload is hundreds of MB | something `.easignore` should exclude (it replaces `.gitignore` for EAS). Check with `eas build:inspect --stage archive --output <new folder>` |

### Supabase

- `Failed to connect` / timeout → host/port wrong (use the **session** pooler),
  or the free project was **paused** after a week of inactivity — restore it in
  the dashboard.
- `password authentication failed` → the username must be
  `postgres.<project-ref>` on the pooler, not `postgres`.
- `Tenant or user not found` → wrong region host for the pooler.

---

## 12. Switching back to local development

Nothing on the phone needs to change unless you want the *same* phone to talk
to your PC:

1. `docker compose up -d` (repo root), apply migrations locally
   (LOCAL_DEVELOPMENT.md), `dotnet run --project services/api/src/Sahno.Api --urls http://0.0.0.0:5062`.
2. `apps/mobile/.env` keeps `EXPO_PUBLIC_API_URL=http://<LAN IP>:5062`.
3. Use the **development** build (`npx expo run:android`, or EAS
   `development`), which reads `.env` through Metro. The preview build stays
   pointed at Render. Only one of them can be installed at a time, since they
   share the bundle id; a differently signed build may need the other
   uninstalled first.

---

## 13. Moving to production infrastructure

What is host-specific, and where it lives:

| Concern | Test (Render + Supabase) | Production (e.g. DigitalOcean) | Code change? |
|---|---|---|---|
| Container | `services/api/Dockerfile` | same image | no |
| Port | `PORT` from Render | `PORT`/`http_port` from App Platform | no |
| TLS proxy | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` | same | no |
| Database | `ConnectionStrings__Sahno` → Supabase pooler | → managed Postgres (`SSL Mode=Require` or `VerifyFull`) | no |
| Migrations | `dotnet ef database update` from a PC | a controlled migration job (D-072), same tool | no |
| Auth | Auth0 env vars | production Auth0 tenant/API | no |
| App URL | EAS `preview` env | EAS `production` env | no |
| Push | EAS project credentials | same (production APNs) | no |
| Deploy trigger | `render.yaml`, `test` branch | host's own config, manual approval | no |

`render.yaml` is the only Render-specific file; Supabase appears nowhere but in
a secret. When production exists, give it its **own** database, Auth0 tenant
(or API), and EAS `production` variables — do not point production at the
Supabase test database.

---

## 14. Self-hosted test server (alternative to Render)

Render's nearest region is Singapore, and its free instance sleeps and gets a
tenth of a CPU, so from Australia the app felt slow. The same image runs on
any Docker host; `deploy/test-server/` runs it on an Ubuntu server in
Australia, next to a **Sydney** Supabase project.

```text
phones ──HTTPS/WSS──► Caddy :443 (automatic Let's Encrypt certificate)
                        └─► api:8080 (services/api/Dockerfile, not published)
                              └─► Supabase Sydney (session pooler, SSL)
```

**No domain?** Use [sslip.io](https://sslip.io): `sahno.203-0-113-10.sslip.io`
resolves to `203.0.113.10`, and Caddy can get a real certificate for it. That
keeps `https://`/`wss://`, which iOS requires and which keeps sign-in tokens
off the wire in clear text. Swapping in a real domain later is a change to
`SAHNO_HOST` and the app's `EXPO_PUBLIC_API_URL`.

### On the Farnese sandbox server: Sahno's own Caddy on port 8443

That server's ports 80/443 belong to the Farnese Caddy, and its Caddyfile is
not Sahno's to edit. So Sahno runs its own Caddy (profile `caddy`) on port
**8443**, and the app's API URL is `https://api-test.sahno.app:8443`.

- Caddy cannot answer Let's Encrypt on 80/443, so it proves the domain with
  Cloudflare's **DNS challenge** (a TXT record written through
  `CLOUDFLARE_API_TOKEN`). That needs the Cloudflare module, so the image is
  `ghcr.io/devadarshsharma/sahno-caddy`, built on GitHub from `deploy/caddy`.
- DNS: `api-test` is an **A** record to the server, **DNS only** (grey cloud).
  Proxied, traffic would enter Cloudflare — which from Australian networks
  was measured going via Singapore, ~0.3 s per request. Direct it is ~0.04 s.
- Token: Cloudflare → My Profile → API Tokens → *Edit zone DNS* template,
  zone `sahno.app` only.
- Firewall: allow **8443/tcp** (and 8443/udp for HTTP/3) in `ufw` and in the
  cloud provider's firewall, if either is active.
- Nothing of Farnese's changes: not its Caddyfile, network, or ports.
- Troubleshooting: `docker compose logs caddy` should show "certificate
  obtained successfully" for `api-test.sahno.app`. A DNS-challenge error there
  is the token (wrong zone, or missing DNS edit permission). A timeout from
  outside is the firewall.

A Cloudflare Tunnel was tried first (no inbound port at all) and dropped for
that Singapore detour.

### Prerequisites on the server (bundled or existing proxy)

- Docker Engine with the Compose plugin (`docker compose version`).
- Ports **80 and 443** free on the host and open to the internet (host
  firewall *and* the cloud provider's security group). Caddy needs 80 for the
  certificate challenge and to redirect to HTTPS.
  Check: `sudo ss -ltnp '( sport = :80 or sport = :443 )'` — no output means free.
- **A Caddy already on the server** (the usual case): only the API container
  starts (bound to `127.0.0.1:SAHNO_API_PORT`); add to the existing Caddyfile

  ```text
  api-test.sahno.app {
  	reverse_proxy 127.0.0.1:8080
  }
  ```

  and reload Caddy. It fetches the certificate itself and passes WebSockets through.
- **That Caddy runs in a container** (as on the Farnese sandbox server):
  `127.0.0.1` inside it is the container itself. Set `COMPOSE_FILE` and
  `PROXY_NETWORK` in `.env` (see `.env.example`) so the API joins the proxy's
  Docker network, and proxy to `sahno-api:8080` instead. The service is named
  `sahno-api` so it can never be confused with another project's `api`.
- **Ports 80/443 free:** start the bundled Caddy too with
  `docker compose --profile caddy up -d`.
- **Cloudflare DNS:** set the record to *DNS only* (grey cloud), so the server
  gets its own certificate and there is one TLS hop to reason about.

### First deployment

```bash
git clone https://github.com/devadarshsharma/Sahno.git /srv/sahno-test
cd /srv/sahno-test && git switch test
cd deploy/test-server
cp .env.example .env && chmod 600 .env && nano .env
docker compose pull && docker compose up -d
docker compose logs -f sahno-api             # wait for "Now listening on: http://[::]:8080"
curl http://127.0.0.1:8080/health/ready       # Healthy, on the server itself
curl https://<SAHNO_HOST>/health/ready # Healthy
```

In `.env`, a literal `$` must be written `$$`. Apply migrations to the Sydney
database exactly as in §3, with its connection string.

Point the app at it: set `EXPO_PUBLIC_API_URL` in the EAS `preview`
environment to `https://<SAHNO_HOST>` (`eas env:update`), then **rebuild** — the
URL is compiled into the app.

### Updates, logs, restart

The server never builds. A push to `test` that touches `services/api` runs
`.github/workflows/api-image.yml`: it runs the API tests and, only if they pass,
builds the image on GitHub and pushes
`ghcr.io/devadarshsharma/sahno-api:test` (and `:sha-<commit>`). Then, on the server:

```bash
cd /srv/sahno-test && git pull && cd deploy/test-server && docker compose pull && docker compose up -d
docker compose logs -f sahno-api             # the same log lines as §6
docker compose restart sahno-api
docker compose down                    # stop (certificates are kept in a volume)
```

`git pull` only matters when `deploy/test-server` itself changed. To roll back,
set `SAHNO_API_IMAGE` in `.env` to an earlier `sha-<commit>` tag (GitHub →
Packages → sahno-api) and `up -d` again. The image is private if the package
is: `docker login ghcr.io` on the server with a token that has `read:packages`,
or make the package public (the source already is). Suspend the Render service while this server is in use, so there is one
test API, not two.
