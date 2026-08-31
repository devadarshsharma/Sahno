# Sahno MVP Build Backlog

**Status:** Initial build-ready baseline  
**Last updated:** 23 August 2026  
**Platform:** React Native for iOS and Android

This backlog translates the accepted Sahno product decisions into implementation slices. Detailed UI copy and visual styling remain iterative.

## Slice 1 — Application foundation and authentication

### Outcome

A person can install Sahno, authenticate, and maintain a secure session.

### Acceptance criteria

- Email one-time-code authentication works without passwords.
- Google and Apple authentication are available.
- Successful authentication creates or retrieves one Sahno account.
- Authentication errors are actionable and do not expose whether unrelated accounts exist.
- A returning authenticated person resumes their session securely.
- Sign out is available from More/settings.

### Deferred follow-up — secure account linking and duplicate-user merge

The MVP accepts the Auth0 Free-plan limitation (D-075): no account linking, one Auth0 subject per Sahno user, and sign-in guidance telling returning people to reuse their original method. A wrong-method sign-in creates an empty duplicate account but never destroys data — the original account and all its material remain intact under the original method. A later slice must add, in this order:

1. **Guided redirect (first story):** when sign-in would create a new Sahno user whose securely verified email already belongs to an existing user, do not create the duplicate — tell the person which method their account uses ("This email is registered with Google — Continue with Google"). This is a deliberate, documented exception to the no-account-existence-disclosure rule, limited to the person’s own verified email.

2. An explicit, secure account-linking flow (both identities verified — never automatic linking by matching email addresses, which Apple private-relay addresses make unsafe).
3. A deliberate merge workflow for the duplicate Sahno users that can exist when a person signed in through two identities before they were linked.

## Slice 2 — Organisation onboarding and switching

### Outcome

A person can create or join a private organisation and switch between organisations.

### Acceptance criteria

- A valid invitation shows the organisation identity before joining.
- An invitation is single-use: it is matched only after the invited email is securely verified, acceptance binds it to exactly one Sahno account, and it cannot be accepted again.
- Invitation acceptance adds the person as a Member.
- A person without an invitation can create an organisation or enter an invite link.
- Organisation creation requires only a name.
- Logo and group type are optional; time zone is detected and editable.
- The creator becomes the single Owner.
- One account can hold different roles in multiple organisations.
- Switching organisations strictly changes the data and permissions context.
- A new Owner sees the dismissible setup checklist.

**Delivery note (31 Aug 2026):** implemented with shareable link-code invitations (D-077). Deferred within this slice: logo upload (needs DigitalOcean Spaces), email-delivered invitations (needs the notifications slice), and the checklist actions for enquiries, logo, and settings (features arrive in later slices).

## Slice 3 — Membership, roles, and privacy

### Outcome

The Owner/Admin can manage a private group without exposing protected information.

### Acceptance criteria

- Formal roles are Owner, Admin, and Member only.
- Exactly one Owner exists per organisation.
- Only the Owner can appoint or remove Admins.
- Owner transfer is explicit; the Owner cannot leave before transferring ownership.
- Owner/Admins can invite, edit, and remove ordinary Members.
- Member removal requires confirmation.
- Admin financial access is individually controlled by the Owner and off by default.
- Members see directory identity/function but not private phone/email unless shared.
- One person cannot use role changes to escalate their own authority.

## Slice 4 — Enquiries and lifecycle

### Outcome

An Owner/Admin can represent a real opportunity from incomplete Draft through closure.

### Acceptance criteria

- A Draft can be saved with only a title.
- A proposed date/date range is required before requesting availability.
- Venue, time, and customer information can remain TBC.
- Supported states are Draft, Checking Availability, Tentative, Confirmed, Postponed, Completed, and Cancelled.
- Accepted transitions and safeguards match `ENGAGEMENT_STATE_MACHINE.md`.
- Preparation is represented by readiness, not a separate state.
- Completion is manual.
- Cancellation, postponement, reopening, and reversal require the documented reasons and notifications.
- State changes and significant date changes remain in activity history.

## Slice 5 — Availability

### Outcome

Members can answer availability privately, while Owner/Admins can build a viable lineup.

### Acceptance criteria

- Owner/Admin selects Members and sends availability requests.
- Responses are Available, Maybe, or Unavailable.
- A Member sees only their own response.
- Owner/Admin sees individual responses and totals.
- Non-responders are clearly identifiable and can be reminded.
- Adding replacement Members does not reset existing responses.
- Removed Member responses remain only in internal history.
- Advancing to Tentative is manual.
- Confirmation with outstanding responses is allowed after explicit warning.
- Outstanding availability remains visible after confirmation.

## Slice 6 — Member Home, Events, and calendar

### Outcome

A Member immediately understands what requires action and what they have committed to.

### Acceptance criteria

- Member Home prioritises Needs your response, next Confirmed Event, Tentative Events, important changes, then later Events.
- Events are grouped into Needs response, Tentative, Confirmed, and relevant History.
- Only selected/invited Events are visible to a Member.
- Sahno calendar distinguishes Needs response, Tentative, and Confirmed.
- Tentative or Confirmed Events can be added to a personal calendar.
- Personal-calendar Tentative entries remain clearly labelled.

## Slice 7 — Booking/Event workspace and readiness

### Outcome

The group can prepare a Confirmed Event without relying on scattered chat messages.

### Acceptance criteria

- Logical workspace sections are Overview, People, Responsibilities, Rehearsals, Resources, Discussion, and Admin.
- Members see participant-facing information only.
- Customer contact, private notes, negotiation, and financial data remain protected.
- Readiness is an actionable checklist, not a percentage.
- Readiness items can be marked Not required.
- Missing critical information appears in Owner/Admin Needs attention.
- Day-of Member view prioritises venue, arrival/call time, sound check, start time, personal responsibilities, and dress.

## Slice 8 — Responsibilities, rehearsals, and resources

### Outcome

Preparation work and reusable material stay attached to the correct Engagement.

### Acceptance criteria

- Owner/Admin creates and assigns responsibilities.
- Members can update their own assigned responsibilities but not organisation-wide details.
- Rehearsals are linked to a parent Engagement.
- Notes, links, and files support Participants or Admins-only audiences.
- Participants is the default audience.
- General audience settings cannot expose protected financial information.
- Reusable repertoire/resources can be attached to an Event.

## Slice 9 — Contextual discussion

### Outcome

Participants coordinate inside the relevant Event context.

### Acceptance criteria

- Only people with Event access can access its discussion.
- Participants can create, edit, and delete their own messages.
- Edited messages display an edited indicator.
- Members cannot modify another person's messages.
- Owner/Admins can remove any message for moderation.

## Slice 10 — Notifications and attention dashboard

### Outcome

Members receive important changes and organisers know what to chase.

### Acceptance criteria

- Important activity creates in-app notifications.
- Availability requests, reminders, confirmation, postponement, cancellation, and major date/venue/time changes also send email.
- Owner/Admin Home prioritises unanswered requests, missing critical details, overdue payments, and unresolved work.
- Upcoming Confirmed, Tentative, New enquiries, and recent activity follow Needs attention.
- Native push is not required for the pilot architecture but can be added later.

## Slice 11 — Customer and lightweight finance administration

### Outcome

Owner/authorised Admins can track commercial operational facts without Sahno becoming accounting software.

### Acceptance criteria

- Owner/Admins can store basic customer/contact information and private notes.
- Members never see customer contacts, negotiations, prices, deposits, balances, or payments.
- Only the Owner and explicitly authorised Admins can view/manage financial fields.
- Performer payment obligations can remain open after an Event is Completed.
- Outstanding payments continue to appear administratively until resolved.
- Tax, bookkeeping, invoicing, and payment processing are outside this slice.

## Pilot release gate

The MVP is ready for Customer Zero when AQP can complete the primary organiser/member journey on iOS and Android without losing lifecycle state, availability, critical Event details, responsibilities, discussion context, or outstanding payment obligations.

