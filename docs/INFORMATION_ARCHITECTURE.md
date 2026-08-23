# Sahno Information Architecture

**Status:** Accepted mobile-first baseline for wireframing  
**Last updated:** 23 August 2026

## Accepted terminology

| Context | User-facing term |
| --- | --- |
| Incoming or early opportunity | Enquiry |
| Owner/Admin pipeline | Booking |
| Member schedule and participation | Event |
| Internal domain model | Engagement |

The underlying object remains one Engagement through its lifecycle; labels adapt to the user's task and context.

## Member Home

Information priority:

1. Needs your response.
2. Next confirmed event.
3. Tentative events.
4. Recent important changes.
5. Later upcoming events.

This is a content hierarchy rather than a fixed visual design. Look and feel will evolve through wireframing and implementation.

## Owner/Admin Home

Information priority:

1. Needs attention: unanswered requests, missing critical details, overdue payments, and unresolved work.
2. Upcoming confirmed bookings.
3. Tentative bookings.
4. New enquiries.
5. Recent activity.

The “what needs action?” principle is fixed; presentation remains open to design iteration.

## Initial primary navigation

| Member | Owner/Admin |
| --- | --- |
| Home | Home |
| Events | Bookings |
| Calendar | Calendar |
| Members | Members |
| More | More |

Notifications appear in the top bar. This model is provisional until prototype testing.

## Owners/Admins who participate

Owners and Admins use a combined experience rather than switching modes. Administrative screens also expose their own availability requests, assignments, and participant information where relevant.

## Multiple organisations

An account can belong to multiple organisations with different roles. An organisation switcher changes the active context. All operational, membership, permission, customer, and financial data remains scoped to that organisation. A combined cross-organisation calendar is deferred beyond MVP.

## Joining an organisation

The MVP is invitation-only. An Owner/Admin sends an email invitation or revocable shareable link. The invitee signs in or creates an account and joins as a Member. There is no public organisation directory or open join flow.

## Member profile

Required: display name. Optional: profile photo, organisation function/skill, phone number, and Admin-only notes. Email comes from the account. Phone and email are private from ordinary Members by default. Sensitive travel and identity information is deferred.

## Booking and Event workspace

Logical sections:

1. Overview.
2. People.
3. Responsibilities.
4. Rehearsals.
5. Resources.
6. Discussion.
7. Admin.

Members see permitted participant-facing information only. Admin contains customer details, private notes, permitted financial information, and activity history. The section model follows the canonical product specification; its visual implementation remains open.

## Event readiness

Readiness appears as a checklist of missing or completed operational items. An Owner/Admin can mark an item Not required. The MVP does not calculate or emphasise a readiness percentage.

## Notifications

Important activity appears in an in-app notification surface. Availability requests, reminders, booking-state changes, and major logistical changes also use email in the MVP. Native push is deferred until the mobile delivery strategy is decided.

## Calendar

The Sahno calendar distinguishes Needs response, Tentative, and Confirmed items. Members can add Tentative or Confirmed Events to a personal calendar, with Tentative clearly included in the title or status. Automatic two-way calendar synchronisation is deferred.

## Creating an Enquiry

Only a title is required to save a Draft. A proposed date or date range is required before an availability request can be sent. Customer, venue, and exact timing information may remain TBC.

## Booking and Event lists

Owner/Admin Bookings groups: New enquiries/Drafts, Checking Availability, Tentative, Confirmed, Postponed, and History containing Completed and Cancelled.

Member Events groups: Needs your response, Tentative, Confirmed, and History containing Completed and Cancelled Events in which the Member participated.

Search and filters sit around these logical groups. Their exact visual arrangement is deliberately flexible for prototyping.

## Implementation details still to test

- Exact navigation presentation and responsive behaviour.
- Tabs versus filters versus grouped scrolling lists.
- Booking/Event workspace layout.
- Organisation settings layout.
- Empty, loading, error, and first-use states.

## Delivery context

Wireframes target a mobile-first React Native application for iOS and Android. Owner/Admin workflows must be fully usable on phone-sized screens; the initial product does not depend on a desktop experience.

## Authentication

The MVP offers passwordless email one-time codes, Continue with Google, and Sign in with Apple. There is no password creation or password-reset flow.

## First-time onboarding

After authentication, a valid invitation leads to an organisation preview and Join action. Without an invitation, the person can create an organisation or enter/paste an invite link. Creation assigns Owner; invited joining assigns Member.

## Creating an organisation

Required: organisation name. Optional: logo and group type. Time zone is detected automatically but can be edited. Invitations and detailed setup happen after creation.
