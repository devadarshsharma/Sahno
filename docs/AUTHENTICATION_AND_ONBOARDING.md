# Sahno Authentication and Onboarding

**Status:** Accepted MVP baseline  
**Last updated:** 28 August 2026
**Decision references:** D-055 through D-058 in `DECISIONS.md`

## Authentication

The MVP supports:

- passwordless email sign-in using a one-time code;
- Continue with Google; and
- Sign in with Apple.

There are no passwords or password-reset flows.

## Sign-in experience

The authentication screen presents:

1. **Continue with Google**
2. **Continue with Apple**
3. An email address field with **Continue with email**

The product does not present separate **Create account** and **Sign in** choices. The selected method either creates a Sahno account or returns the person to their existing account.

## Identity and account-linking rules

- A person has one Sahno user identity that may have multiple authentication methods linked to it.
- A provider login must not create a duplicate Sahno account when it is securely linked to an existing identity.
- Two accounts must not be linked solely because their visible email addresses appear to match.
- Sign in with Apple may provide a private relay address, so account linking must use an explicit, verified flow.
- Organisation roles and permissions come from Sahno's own membership records, never directly from Google, Apple, Auth0 profile metadata, or client input.
- An invitation is matched only after the invited email address has been securely verified or confirmed by the authenticated person.

### Accepted MVP limitation — no account linking (D-075)

The MVP runs on the Auth0 Free plan and does not implement account linking. Each sign-in method (Google, Apple, email code) resolves to its own Auth0 subject, and the API maps one subject to one Sahno user — so signing in through a different method creates a separate Sahno account. This is accepted for the MVP:

- The sign-in screen tells returning people to use the same method they originally chose.
- Identities are never matched or merged automatically by email address (Apple private-relay addresses make that unsafe), and no Auth0 Action performs automatic linking.
- The sign-in method is retained implicitly in the stored Auth0 subject prefix (`google-oauth2|…`, `apple|…`, `email|…`), which is enough to later show "Signed in with Google/Apple/Email" in account settings. An account-settings feature is not part of the first auth slice.
- Secure, explicit account linking plus a deliberate duplicate-user merge workflow are deferred backlog items (`MVP_BUILD_BACKLOG.md`, Slice 1 deferred follow-up).

## Account deletion requirement

Account deletion is separate from leaving an organisation, cancelling an organisation subscription, or deleting an organisation. Before public store release, Sahno must provide a clear in-app account-deletion path and the external deletion-request path required for Google Play distribution. Exact retention and ownership-transfer behaviour will be specified before implementation.

## Onboarding data boundary

Onboarding deliberately collects no legal names, dates of birth, or other travel-grade identity details. Those are collected just-in-time when a booking actually requires them, under D-076 in `DECISIONS.md`.

## Display name

Every account needs a display name before it can go further (D-046). Sahno's own record is the only source for it: the client never shows the identity provider's `name` claim, because the passwordless email connection sets that claim to the email address itself — not a name, and an address that D-018 keeps private from other Members by default.

The API takes email and name from the namespaced claims added by the Auth0 post-login Action. While that Action is not populating them, `users.display_name` stays null for every account regardless of sign-in method, so everyone is asked to choose a name after signing in. Once the Action delivers a genuine name (Google and Apple both supply one), those people are no longer asked; a `name` claim that merely repeats the account email is reported as no name at all.

A name the person chose always outranks a later provider hint: without that precedence, the next sign-in would carry the email-as-name claim again and silently revert it.

The name is the only identity detail collected here. The remaining profile fields in D-046 are optional and arrive with the membership-directory slice.

## First-time branching

### Valid invitation

After authentication, the person sees the organisation identity and confirms **Join organisation**. They join as a Member.

### No invitation

The person can:

- create an organisation; or
- enter/paste an invite link.

Creating an organisation makes them its Owner. Admin access cannot be granted through an invitation.

## Creating an organisation

- Organisation name: required.
- Organisation logo: optional.
- Group type: optional.
- Time zone: automatically detected and editable.

Invitations and detailed settings happen after creation.

## First Owner experience

The Owner lands on Admin Home with a dismissible setup checklist:

1. Invite your Members.
2. Create your first Enquiry.
3. Optionally add a logo.
4. Optionally review settings.

Checklist items open real features and disappear after completion or dismissal.
