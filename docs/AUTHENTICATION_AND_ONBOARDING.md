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

Each sign-in method (Google, Apple, email code) resolves to its own Auth0 subject. Sahno links them itself (D-131), with no Auth0 account linking:

- An account can have several sign-ins (`user_identities`: subject → user). The first is also kept as `users.external_subject`.
- A sign-in Sahno has not seen before, carrying a **verified** email that an account already has (compared without case), opens that account instead of creating a new one. Verified means the token's `https://sahno.app/email_verified` (or `email_verified`) claim is `true`; when the claim is absent, only the `email|` and `google-oauth2|` connections are trusted, since both hand over only addresses the person has proved they own.
- An unverified email is never matched, so typing somebody else's address cannot open their account. Apple "Hide My Email" relay addresses are unique and simply never match.
- On a linked account, an emailed-code sign-in's name claim (which is the address itself) never replaces a real name.
- The `AddUserIdentities` migration folded the duplicates made before this: an emailed-code or Google account in no organisation, whose address an older account has, gives its sign-in to the older account and is deleted.
- To make verification explicit for every connection, the post-login Action can add `api.accessToken.setCustomClaim('https://sahno.app/email_verified', event.user.email_verified)`.

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
