# Sahno Authentication and Onboarding

**Status:** Accepted MVP baseline  
**Last updated:** 23 August 2026  
**Decision references:** D-055 through D-058 in `DECISIONS.md`

## Authentication

The MVP supports:

- passwordless email sign-in using a one-time code;
- Continue with Google; and
- Sign in with Apple.

There are no passwords or password-reset flows.

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

