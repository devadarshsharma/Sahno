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

## Account deletion requirement

Account deletion is separate from leaving an organisation, cancelling an organisation subscription, or deleting an organisation. Before public store release, Sahno must provide a clear in-app account-deletion path and the external deletion-request path required for Google Play distribution. Exact retention and ownership-transfer behaviour will be specified before implementation.

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
