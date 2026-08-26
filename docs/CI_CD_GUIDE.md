# Sahno CI/CD Guide

**Status:** Working engineering guide  
**Last updated:** 26 August 2026  
**Current workflow:** `/.github/workflows/ci.yml`

## 1. Purpose

This guide explains Sahno's delivery automation in practical terms. It records what has been configured, why each part exists, how to operate it, and how it will grow from basic Continuous Integration into safe Staging and Production delivery.

The pipeline is intentionally being built incrementally as a learning exercise. Every new check or deployment step should be understood, run, and observed before more automation is added.

## 2. What has been completed

The first Sahno pull request introduced two successful CI checks:

- **CI / Mobile** — installs JavaScript dependencies, lints the mobile workspace, and runs TypeScript checking.
- **CI / API** — restores NuGet dependencies and builds the complete .NET solution in Release configuration.

Both jobs run on fresh GitHub-hosted Ubuntu runners. This proves the repository can be restored and built without relying on uncommitted files, globally installed project dependencies, or other hidden state on the developer's computer.

## 3. Development flow

The current workflow is:

```text
Local feature branch
    ↓ git push
GitHub feature branch
    ↓ pull request into main
.github/workflows/ci.yml starts
    ├─ Mobile job
    └─ API job
         ↓ both pass
Pull request becomes eligible for review and merge
```

Work is developed on a feature branch rather than directly on `main`. A pull request provides one place to inspect commits, file changes, automated checks, merge conflicts, and review discussion.

## 4. Where GitHub Actions finds the workflow

GitHub discovers YAML workflow files under:

```text
.github/workflows/
```

Sahno's current workflow is:

```text
.github/workflows/ci.yml
```

Putting `ci.yml` elsewhere would make it an ordinary file rather than an executable GitHub Actions workflow.

## 5. Workflow triggers

```yaml
on:
  pull_request:
    branches:
      - main
  push:
    branches:
      - main
```

This runs CI in two situations:

1. A pull request proposes a change to `main`.
2. A commit reaches `main`, normally after a merge.

The pull-request run validates the proposed change before merge. The push run validates the actual combined commit that now exists on `main`.

## 6. Workflow security and concurrency

```yaml
permissions:
  contents: read
```

The workflow can read repository contents but cannot write code, alter pull requests, create releases, or deploy infrastructure. This follows the least-privilege principle.

```yaml
concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

If another commit is pushed to the same branch while an older run is still active, GitHub cancels the outdated run. This saves runner time and ensures feedback relates to the newest commit.

## 7. Jobs and runners

The workflow defines two jobs:

```yaml
jobs:
  mobile:
  api:
```

Each job receives its own fresh `ubuntu-latest` virtual machine. Jobs do not automatically share installed tools, files, or process state. Both therefore check out the repository and configure their own runtime.

The Mobile and API jobs can run in parallel because neither depends on the other. Parallel execution shortens pull-request feedback time and makes failures easier to identify.

Each job also has a 15-minute timeout so a hung tool cannot consume runner capacity indefinitely.

## 8. Mobile job

### Check out the repository

```yaml
- name: Check out repository
  uses: actions/checkout@v4
```

A GitHub runner starts without Sahno's files. Checkout downloads the commit being tested into the runner workspace.

### Install pnpm

```yaml
- name: Install pnpm
  uses: pnpm/action-setup@v4
  with:
    version: 10.34.5
    run_install: false
```

CI uses the same pinned pnpm version as local development. `run_install: false` keeps dependency installation as a visible, separately named step.

### Set up Node.js

```yaml
- name: Set up Node.js
  uses: actions/setup-node@v4
  with:
    node-version: 24
    cache: pnpm
```

This installs Node.js 24 and enables caching for pnpm's downloaded package store. Caching improves later run times, but it does not bypass dependency installation or change the lock file.

### Install dependencies

```yaml
- name: Install dependencies
  run: pnpm install --frozen-lockfile
```

`--frozen-lockfile` requires `package.json` and `pnpm-lock.yaml` to agree. CI fails rather than silently resolving and writing different dependency versions.

### Lint the mobile application

```yaml
- name: Lint mobile application
  run: pnpm lint
```

ESLint checks the Expo/React Native source for configured code-quality and React correctness rules.

### Type-check the mobile application

```yaml
- name: Type-check mobile application
  run: pnpm typecheck
```

TypeScript validates imports, functions, component props, and other typed code without producing build output.

## 9. API job

### Check out the repository

The API job runs on a separate runner and therefore performs its own checkout.

### Set up .NET

```yaml
- name: Set up .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 10.0.x
```

This installs a compatible .NET 10 SDK. The `x` accepts servicing releases within .NET 10 instead of permanently pinning one patch SDK.

### Restore NuGet dependencies

```yaml
- name: Restore dependencies
  run: dotnet restore services/api/Sahno.slnx
```

Restore reads the solution/project dependency information and downloads required NuGet packages.

### Build in Release configuration

```yaml
- name: Build API
  run: >
    dotnet build services/api/Sahno.slnx
    --configuration Release
    --no-restore
```

This compiles all production projects in the configuration intended for deployment. `--no-restore` ensures the build consumes the explicit restore step and keeps dependency-resolution failures separate from compiler failures.

## 10. What the first green checks prove

The current pipeline proves:

- pnpm can reproduce the JavaScript dependency tree from the lock file.
- The mobile source passes ESLint.
- The mobile source passes TypeScript checking.
- NuGet dependencies restore successfully.
- All .NET Onion Architecture projects compile together in Release configuration.
- The repository builds on clean Linux runners rather than only on the developer's Windows computer.

The pipeline does **not yet** prove:

- The mobile app launches on iOS or Android.
- The API process starts or its health endpoint responds.
- The mobile app can communicate with the API.
- PostgreSQL mappings and migrations work.
- Authentication, permissions, or tenant isolation work.
- Staging or Production deployment succeeds.

These protections will be added as their corresponding product capabilities are implemented.

## 11. CI versus CD

The current workflow is **Continuous Integration**:

> Automatically validate proposed code changes.

It is not yet **Continuous Delivery/Deployment**:

> Package and safely deliver validated changes to Staging or Production.

The intended evolution is:

```text
Pull request
    → restore/install
    → lint and type-check
    → build
    → unit tests
    → PostgreSQL integration tests
    → architecture and permission tests

Merge to main
    → repeat required validation
    → build immutable API container
    → controlled Staging migration
    → deploy API to DigitalOcean Staging
    → run smoke checks
    → create EAS Preview build when appropriate

Manual Production approval
    → controlled Production migration
    → deploy the already-validated release
    → verify health and support rollback
```

## 12. Local equivalents

Developers should run the same core checks before pushing:

```powershell
pnpm install --frozen-lockfile
pnpm lint
pnpm typecheck
dotnet restore .\services\api\Sahno.slnx
dotnet build .\services\api\Sahno.slnx --configuration Release --no-restore
```

CI remains necessary even when local checks pass because it provides an independent clean environment and a durable result attached to the pull request.

## 13. Reading a failed run

When a check fails:

1. Open the failed check from the pull request.
2. Identify the first red step.
3. Expand that step and find the first meaningful error rather than the final generic exit-code message.
4. Reproduce the same command locally.
5. Fix the underlying source, configuration, or dependency problem.
6. Commit and push the fix to the same feature branch.
7. GitHub automatically starts a replacement run; concurrency may cancel an older run.

Do not weaken or remove a quality gate merely to make a pull request green without understanding the failure.

## 14. Pull-request and merge discipline

- Work on a focused branch.
- Keep commits understandable and review staged changes before committing.
- Push the branch and open a pull request into `main`.
- Read the file diff, not only the green status.
- Require relevant CI checks before merging.
- Resolve unexpected generated files or dependency changes before merge.
- Merge only when the change is understood and the branch represents one coherent outcome.

## 15. Next CI improvements

The next additions should follow implementation needs:

1. Backend unit and architecture test projects.
2. API integration tests with PostgreSQL Testcontainers.
3. Mobile component tests.
4. API startup/health smoke test.
5. OpenAPI-generated client consistency check.
6. Container build validation.
7. Staging environment and DigitalOcean deployment.
8. EAS development/preview build automation.
9. Manual Production approval and deployment safeguards.

Each change should be introduced separately enough that its purpose, inputs, permissions, failure modes, and recovery procedure remain understandable.

