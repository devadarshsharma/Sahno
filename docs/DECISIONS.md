# Sahno Decision Log

**Purpose:** Record durable product, commercial, design, and technical decisions  
**Last updated:** 21 August 2026

## How to use this log

- Add a dated entry when a material choice is accepted, changed, or reversed.
- Use status **Accepted**, **Provisional**, **Superseded**, or **Rejected**.
- Accepted decisions override older conflicting material.
- Provisional entries document direction but are not implementation requirements.
- Link to supporting specifications or research where available.
- Never delete an old decision when it changes; mark it Superseded and add the replacement.

---

## D-001 — Product name

**Date:** 17 August 2026  
**Status:** Accepted

The product name is **Sahno**.

**Rationale:** Selected after exploring multiple alternatives and now used as the stable product identity.

---

## D-002 — Tagline direction

**Date:** 17 August 2026  
**Status:** Accepted

Use **“Make it happen, together.”** as the current tagline direction.

**Note:** The product specification treats this as the current tagline; the broader formal brand system remains open.

---

## D-003 — Initial market and Customer Zero

**Date:** 17 August 2026  
**Status:** Accepted

Build first around the real workflow of a community/performing group, using Australian Qawwal Party (AQP) as Customer Zero. Keep the underlying domain model generic enough to support other group types later.

---

## D-004 — Engagement lifecycle, not simple events

**Date:** 17 August 2026  
**Status:** Accepted

Sahno models the lifecycle before and after confirmation rather than treating every item as an already-confirmed event. Incomplete information is valid, and availability is separate from booking confirmation.

**Open detail:** Exact state names, transitions, and whether “Engagement” is user-facing terminology.

---

## D-005 — Separate organiser and member experiences

**Date:** 17 August 2026  
**Status:** Accepted

Organisers need an exception- and attention-oriented operational view. Members need a simple view of responses, commitments, essential event details, and responsibilities.

---

## D-006 — Structured engagement workspace

**Date:** 17 August 2026  
**Status:** Accepted

Confirmed engagements have a structured workspace. Sound-check/call time is a first-class field. Responsibilities, linked rehearsals, resources, and contextual event discussion are core product concepts.

---

## D-007 — Notifications and organiser attention

**Date:** 17 August 2026  
**Status:** Accepted

Notifications and reminders are core functionality. The organiser dashboard should focus on outstanding responses, missing information, upcoming work, and other items requiring attention.

---

## D-008 — Lightweight payment tracking boundary

**Date:** 17 August 2026  
**Status:** Accepted

Support basic organiser-only performer payment obligations and related operational fields. Do not build full accounting, tax, invoicing, or payment processing into the MVP.

---

## D-009 — Organisation-led commercial model

**Date:** 21 August 2026  
**Status:** Provisional

Members should generally participate for free. The organisation should be the paying customer for advanced organiser capabilities.

**Rationale:** Organisers receive the clearest measurable value through reduced chasing, better readiness, pipeline visibility, and payment oversight. Requiring individual member subscriptions would impede group adoption.

**Validation required:** Confirm willingness to pay with real organisers and observe whether the proposed paid features create a natural upgrade.

---

## D-010 — Initial Free/Pro packaging

**Date:** 21 August 2026  
**Status:** Provisional

Begin validation with a simple Free and Sahno Pro packaging hypothesis. Keep the core member experience and basic organisation workflow available for free. Put advanced dashboard, automation, pipeline, customer history, payment tracking, reporting, storage, and permissions in the paid-value discussion.

**Open detail:** Exact entitlements, member/event limits, storage limits, and whether any proposed paid capability needs a free version.

---

## D-011 — Initial price hypothesis

**Date:** 21 August 2026  
**Status:** Provisional

Test a Sahno Pro price in the range of **A$15–25 per organisation per month**.

This is a research hypothesis, not an approved launch price.

---

## D-012 — Documentation structure

**Date:** 21 August 2026  
**Status:** Accepted

Keep `Sahno_Product_Discovery_and_MVP.md` as the canonical product specification in its existing location. Store supporting commercial strategy, project context, and decisions under `docs/`.

Material decisions should be recorded here and reflected in the relevant source-of-truth document.

---

## Open decisions queue

The following items require future decisions and should receive their own entries when resolved:

- Reconcile and formally record the detailed role/permission matrix discussed after the on-disk product specification was written.
- Engagement states, transition rules, cancellation, and postponement.
- User-facing terminology for engagement/event/activity.
- MVP navigation and information architecture.
- Notification and reminder rules.
- Readiness checklist versus scoring.
- Exact payment fields and visibility.
- Required customer/enquiry fields at creation.
- Pricing, tier limits, billing frequency, and trial policy.
- Final brand system.
- Technical architecture, domain model, API, and delivery platforms.
- Business, domain, trademark, and developer-account setup.

---

## D-013 — Organisation ownership

**Date:** 23 August 2026  
**Status:** Accepted

Each Sahno organisation has exactly **one Owner**. Ownership can be transferred to another eligible organisation member. An organisation may have multiple Admins.

**Rationale:** A single Owner provides clear ultimate accountability, while multiple Admins allow operational responsibility to be shared.

**Required safeguards:**

- The Owner cannot leave or be removed until ownership has been transferred.
- Ownership transfer must be an explicit action and must not happen through an ordinary role edit.
- Admins cannot remove or demote the Owner.

---

## D-014 — Authority to manage Admins

**Date:** 23 August 2026  
**Status:** Accepted

Only the organisation Owner can grant or revoke the **Admin** role.

Admins may manage ordinary members and lower-level operational roles, subject to the final permission matrix, but they cannot appoint or remove other Admins.

**Rationale:** Admin access carries broad organisational authority. Reserving it for the single Owner maintains a clear chain of accountability and prevents privilege escalation between peers.

---

## D-015 — Initial organisation roles

**Date:** 23 August 2026  
**Status:** Accepted

Sahno will initially support three formal organisation roles only:

1. **Owner**
2. **Admin**
3. **Member**

There will not be a separate Organiser or Manager role in the initial product. “Organiser” may be used descriptively for an Owner or Admin performing operational work, but it is not a permission role.

**Rationale:** A small role model is easier for groups to understand and administer. Additional roles should only be introduced when real usage demonstrates a clear need.

---

## D-016 — Admin financial-access permission

**Date:** 23 August 2026  
**Status:** Accepted

Financial information is protected by an individual **View and manage finances** permission for Admins.

- The permission is **off by default** when a Member becomes an Admin.
- Only the Owner can enable or disable it.
- It is configured separately for each Admin.
- Members cannot access organisation financial information.
- Admins do not require this permission to manage ordinary engagement operations and non-financial customer details.

Financial information includes quoted or agreed fees, deposits, balances, performer payment amounts and statuses, and financial summaries.

**Rationale:** This preserves a simple three-role model while giving the Owner explicit control over sensitive commercial information.

---

## D-017 — Admin management of Members

**Date:** 23 August 2026  
**Status:** Accepted

Admins can:

- invite new Members;
- edit Member profiles and organisation-specific details; and
- remove Members from the organisation.

Removing a Member requires an explicit confirmation step.

Admins cannot use Member-management actions to remove, demote, or modify the authority of the Owner or another Admin. Admin-role changes remain exclusive to the Owner under D-014.

---

## D-018 — Member directory and contact privacy

**Date:** 23 August 2026  
**Status:** Accepted

Organisation Members can see each other's:

- display name;
- profile photo; and
- organisation or group function, such as singer, musician, volunteer, or coordinator.

Phone numbers and email addresses are private from other ordinary Members by default. Each person can explicitly choose to share their own contact details with other Members of the organisation.

The Owner and Admins may access Member contact details when required for legitimate organisation management. This administrative access does not make those details visible in the ordinary Member directory.

**Rationale:** Members need enough identity context to coordinate while retaining control over personal contact information.

---

## D-019 — Engagement-management authority

**Date:** 23 August 2026  
**Status:** Accepted

The Owner and all Admins can:

- create enquiries and engagements;
- select Members and request availability;
- edit engagement and event details;
- send reminders;
- confirm, postpone, cancel, and complete engagements; and
- create and assign responsibilities.

Members cannot control the engagement lifecycle or edit organisation-wide engagement details. They can respond to requests, view information relevant to them, participate in permitted discussion, and update their own assigned responsibilities.

An Admin's access to financial engagement fields remains governed separately by D-016.

---

## D-020 — Engagement visibility for Members

**Date:** 23 August 2026  
**Status:** Accepted

Members can see only engagements for which they have been selected, invited, or otherwise added as a participant.

The Owner and Admins can see all engagements belonging to the organisation, subject to the separate financial-access restriction in D-016.

**Rationale:** Early enquiries and engagements involving other lineups may be private or irrelevant to a Member. Participation-based visibility keeps the Member experience focused and protects unnecessary booking information.

---

## D-021 — Visibility of availability responses

**Date:** 23 August 2026  
**Status:** Accepted

The Owner and Admins can see each selected Member's availability response and the overall response summary.

While availability is being collected, a Member can see only their own response. Members cannot see whether another person answered Available, Maybe, or Unavailable.

Once an engagement is confirmed, participating Members can see the final participant list, but not the individual availability answers collected earlier.

**Rationale:** Availability is personal operational information. Keeping responses private reduces social influence and encourages Members to answer based on their genuine availability.

---

## D-022 — Member access to customer and commercial information

**Date:** 23 August 2026  
**Status:** Accepted

Participating Members can see the operational event information required for their involvement, including the event name, venue, schedule, dress instructions, assigned responsibilities, participant-facing notes, and relevant resources.

The following remain hidden from Members:

- customer phone numbers and email addresses;
- private organiser notes;
- customer negotiations and booking history;
- quoted or agreed prices;
- deposits and outstanding balances;
- performer payment amounts and statuses; and
- organisation financial summaries.

The Owner can access all of this information. Admin access to financial fields remains governed by D-016; non-financial administrative customer information is available to Admins.

---

## D-023 — Audience control for engagement notes and resources

**Date:** 23 August 2026  
**Status:** Accepted

Engagement notes, links, and uploaded files support two audience settings:

- **Participants** — visible to selected participating Members, the Owner, and Admins.
- **Admins only** — visible only to the Owner and Admins.

The default audience is **Participants**.

Financial content remains subject to D-016 even if it is associated with an engagement. Creators must not be able to expose protected financial data to Members by changing a general resource audience setting.

**Rationale:** Most event resources should be easy to share with participants, while internal planning material needs a simple protected space.

---

## D-024 — Engagement discussion participation and moderation

**Date:** 23 August 2026  
**Status:** Accepted

Members selected for an engagement can participate in its discussion.

- Members can create messages and edit or delete their own messages.
- Members cannot edit or delete another person's messages.
- Edited messages display an **edited** indicator.
- The Owner and Admins can remove any discussion message when moderation is required.
- Only people who can access the engagement can access its discussion.

**Rationale:** Discussion should support active coordination while preserving author control, basic transparency, and administrative moderation.

---

## D-025 — Draft and first availability transition

**Date:** 23 August 2026  
**Status:** Accepted

A newly created engagement begins in **Draft**.

- Draft engagements are visible only to the Owner and Admins.
- Incomplete information is allowed.
- Creating or saving a Draft does not notify Members.
- Sending the first availability request changes the engagement from **Draft** to **Checking Availability**.
- Only the selected Members receive the request and gain access to the participant-facing engagement information.

**Transition:** `Draft → Checking Availability`

**Rationale:** Draft gives organisers a private preparation step before Members are notified.

---

## D-026 — Manual transition to Tentative

**Date:** 23 August 2026  
**Status:** Accepted

Sahno does not automatically advance an engagement when availability responses reach a threshold.

The system displays response progress and may indicate that all required responses have been received, but an Owner or Admin must decide whether the available lineup is sufficient and manually move the engagement from **Checking Availability** to **Tentative**.

**Transition:** `Checking Availability → Tentative`

**Meaning of Tentative:** The organisation is provisionally able or willing to take the engagement, but the booking has not yet been formally confirmed.

**Rationale:** Response counts alone cannot determine lineup suitability or the state of the customer negotiation.

---

## D-027 — Changing participants during availability collection

**Date:** 23 August 2026  
**Status:** Accepted

While an engagement is **Checking Availability**, an Owner or Admin can add replacement Members and send availability requests to them without restarting the availability check.

- Existing selected Members' responses remain recorded.
- Newly selected Members receive a fresh availability request.
- A removed Member loses access to the engagement.
- The removed Member's prior response remains available only in the internal activity history.
- The engagement remains in Checking Availability until an Owner/Admin advances or ends it.

**Rationale:** Real lineups change during coordination, and replacing one participant should not erase valid responses from everyone else.

---

## D-028 — Collecting additional availability while Tentative

**Date:** 23 August 2026  
**Status:** Accepted

If an Owner or Admin adds or replaces a Member while an engagement is **Tentative**, the engagement remains Tentative while the new availability response is collected.

Sahno must show a prominent outstanding-response warning, such as **“Availability still required from 1 Member.”** It does not automatically move the engagement back to Checking Availability.

**Rationale:** The engagement's position with the customer has not necessarily moved backwards merely because its participant lineup is being adjusted.

---

## D-029 — Confirmation with outstanding availability

**Date:** 23 August 2026  
**Status:** Accepted

An Owner or Admin can mark an engagement **Confirmed** while one or more selected Members still have an outstanding availability response.

Before completing the action, Sahno must show a warning and require explicit acknowledgement. After confirmation, unresolved availability remains prominently visible as a lineup-readiness issue until resolved.

**Rationale:** Customer booking confirmation and participant-lineup completion are separate facts. Sahno should warn about the operational risk without blocking an accurate booking status.

---

## D-030 — Direct transition to Confirmed

**Date:** 23 August 2026  
**Status:** Accepted

Sahno does not require every engagement to pass through every lifecycle state. An Owner or Admin can record a booking as Confirmed through any of these transitions:

- `Draft → Confirmed`
- `Checking Availability → Confirmed`
- `Tentative → Confirmed`

The action records the booking's real status. Selected Members receive the appropriate confirmation or availability communication chosen by the Owner/Admin, and unresolved participant availability remains visible under D-029.

**Rationale:** Some bookings arrive already confirmed or become confirmed before a formal availability workflow is complete. The system should represent reality rather than require fictional intermediate steps.

---

## D-031 — Preparation is readiness, not a lifecycle state

**Date:** 23 August 2026  
**Status:** Accepted

**Preparation** is not a separate engagement status in the initial product.

After confirmation, the engagement remains **Confirmed** while Sahno tracks preparation through readiness data such as venue, timing, participant lineup, rehearsals, resources, attire, and responsibilities.

The normal forward path is `Confirmed → Completed`.

**Rationale:** A separate Preparation status would require manual maintenance while duplicating information that the readiness model can express more accurately.

---

## D-032 — Manual engagement completion

**Date:** 23 August 2026  
**Status:** Accepted

An engagement is marked **Completed** manually by an Owner or Admin. Passing the scheduled end time or event date does not automatically complete it.

Sahno may prompt the Owner/Admin to review and complete the engagement after the event has occurred.

**Transition:** `Confirmed → Completed`

**Rationale:** Responsibilities, operational follow-up, notes, or payments may still require attention after the scheduled event time.

---

## D-033 — Completion with outstanding follow-up

**Date:** 23 August 2026  
**Status:** Accepted

An Owner or Admin can mark an engagement **Completed** while performer payments or other follow-up items remain outstanding.

Sahno must warn about unresolved items before completion. Outstanding financial obligations and administrative follow-up remain open and continue to appear on the relevant Owner/Admin dashboard until resolved.

**Rationale:** Event completion and financial or administrative settlement are related but separate facts.

---

## D-034 — Discarding Drafts and cancelling shared engagements

**Date:** 23 August 2026  
**Status:** Accepted

- A **Draft** may be discarded because it has not been exposed to Members.
- Engagements in **Checking Availability**, **Tentative**, or **Confirmed** can transition to **Cancelled**.
- Cancellation requires a short reason.
- All selected Members who had access to the engagement are notified.
- A Cancelled engagement remains in history and is not permanently deleted through the ordinary cancellation action.

**Transitions:**

- `Checking Availability → Cancelled`
- `Tentative → Cancelled`
- `Confirmed → Cancelled`

**Rationale:** Once Members have been involved, cancellation is meaningful shared history and should be communicated and retained.

---

## D-035 — Postponing an engagement

**Date:** 23 August 2026  
**Status:** Accepted

Engagements in **Checking Availability**, **Tentative**, or **Confirmed** can transition to **Postponed**.

- The same engagement is retained rather than creating an unrelated replacement.
- The original date and full engagement history are preserved.
- The replacement date may initially be TBC.
- Selected Members are notified immediately.
- When a new date is entered, previous availability responses are marked outdated.
- Sahno prompts the Owner/Admin to request availability for the new date.

**Transitions:**

- `Checking Availability → Postponed`
- `Tentative → Postponed`
- `Confirmed → Postponed`

**Rationale:** Postponement changes when an engagement may occur without erasing its identity, context, or prior coordination history.

---

## D-036 — Resuming a postponed engagement

**Date:** 23 August 2026  
**Status:** Accepted

When a replacement date is known, an Owner or Admin chooses the resumed state that represents reality:

- **Checking Availability** — fresh availability requests are sent.
- **Tentative** — the replacement date is provisionally agreed; unresolved availability is flagged.
- **Confirmed** — the replacement booking is formally confirmed; unresolved availability triggers the confirmation warning established in D-029.

Previous availability responses remain in history but are not treated as responses for the replacement date.

**Transitions:**

- `Postponed → Checking Availability`
- `Postponed → Tentative`
- `Postponed → Confirmed`

**Rationale:** The rescheduled booking may resume at different points depending on customer agreement and lineup coordination.

---

## D-037 — Reopening Cancelled and Completed engagements

**Date:** 23 August 2026  
**Status:** Accepted

An Owner or Admin can reopen a **Cancelled** engagement if the opportunity or booking returns. They select the appropriate active state—Checking Availability, Tentative, or Confirmed—and must provide a reason. Previously selected Members are notified, and old availability remains historical unless it still relates to the unchanged date and is deliberately retained by a future explicit rule.

An Owner or Admin can return a **Completed** engagement to **Confirmed** when completion was recorded by mistake. A reason is required.

Both actions are recorded in the engagement activity history and do not erase the prior cancellation or completion event.

**Transitions:**

- `Cancelled → Checking Availability | Tentative | Confirmed`
- `Completed → Confirmed`

**Rationale:** Real bookings can return and administrators can make mistakes. Reversal should be possible without sacrificing auditability or participant awareness.

---

## D-038 — Date changes after Member notification

**Date:** 23 August 2026  
**Status:** Accepted

A Draft engagement's date can be edited freely because Members have not been notified.

After an availability request or other Member notification has been sent, a significant date change must use the postponement/rescheduling flow rather than silently replacing the date.

The flow:

- preserves the original date in activity history;
- notifies selected Members;
- marks previous availability responses outdated; and
- prompts for fresh availability on the replacement date.

**Rationale:** A Member's availability is tied to a date. Silent date replacement would make existing responses misleading and could cause missed commitments.

---

## D-039 — User-facing engagement terminology

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses terminology appropriate to each context:

- **Enquiry** for an incoming or early-stage opportunity.
- **Booking** in the Owner/Admin pipeline as the opportunity progresses.
- **Event** in the Member experience and schedule.
- **Engagement** as the internal domain term connecting the complete lifecycle.

Example: an Admin sees **New enquiry** and **Confirmed bookings**, while a Member sees **Upcoming events**.

**Rationale:** “Engagement” is useful for the underlying model but may feel formal or unclear in everyday group coordination.

---

## D-040 — Member Home information priority

**Date:** 23 August 2026  
**Status:** Accepted

The Member Home prioritises information in this order:

1. **Needs your response**
2. **Next confirmed event**
3. **Tentative events**
4. **Recent important changes**
5. **Later upcoming events**

This decision locks the information hierarchy, not a specific visual layout. Cards, typography, spacing, colours, interaction details, and exact presentation can change during design and implementation testing.

**Rationale:** The screen should answer “What must I do?” first, followed by “Where do I need to be next?”

---

## D-041 — Owner/Admin Home information priority

**Date:** 23 August 2026  
**Status:** Accepted

The Owner/Admin Home prioritises information in this order:

1. **Needs attention** — unanswered requests, missing critical details, overdue payments, and other unresolved work.
2. **Upcoming confirmed bookings**
3. **Tentative bookings**
4. **New enquiries**
5. **Recent activity**

This locks the information hierarchy and the principle that actionable exceptions come first. It does not lock a visual layout.

**Rationale:** The administrative Home should immediately answer “What do I need to organise or chase?”

---

## D-042 — Initial primary navigation

**Date:** 23 August 2026  
**Status:** Provisional

Use the following starting navigation model for prototyping:

| Member | Owner/Admin |
| --- | --- |
| Home | Home |
| Events | Bookings |
| Calendar | Calendar |
| Members | Members |
| More | More |

Notifications use a bell or similar control in the top bar rather than occupying a primary navigation destination.

The structure is deliberately provisional and should be tested for crowding, comprehension, and frequency of use during prototyping.

**Rationale:** Both roles retain a familiar structure while the central list uses terminology appropriate to their task.

---

## D-043 — Combined Admin and participant experience

**Date:** 23 August 2026  
**Status:** Accepted

An Owner or Admin who is also an event participant uses one combined experience rather than switching between Admin and Member modes.

- They retain the Owner/Admin navigation and administrative Home.
- Their own availability requests, assignments, and upcoming participation remain prominently accessible.
- Within a booking, they can access management controls and their personal participant information.
- The MVP does not include a separate “View as Member” mode switch.

**Rationale:** Roles and personal participation coexist in real groups. A mode switch would add navigation and mental overhead without changing the underlying person's responsibilities.

---

## D-044 — Membership in multiple organisations

**Date:** 23 August 2026  
**Status:** Accepted

One Sahno account can belong to multiple organisations and can hold a different role in each organisation.

- The app provides an organisation switcher.
- Bookings, Members, permissions, customer data, and finances are strictly scoped to the selected organisation.
- A combined cross-organisation calendar is not required for the MVP and may be considered later.

**Rationale:** People commonly participate in or manage more than one group. Supporting this in the core account and data model avoids a difficult architectural limitation while keeping the first interface focused.

---

## D-045 — Invitation-only organisation membership

**Date:** 23 August 2026  
**Status:** Accepted

Organisation membership is invitation-only in the MVP.

- The Owner and Admins can invite people by email or a shareable invite link.
- There is no public organisation search or open join flow.
- Invitees must sign in or create an account before joining.
- An invitation always joins the person as a Member.
- Only the Owner can subsequently promote a Member to Admin.
- Invite links can be revoked and may have an optional expiry.

**Rationale:** Private invitations match the trust model of real groups and avoid public discovery, moderation, and accidental-access complexity in the first release.

---

## D-046 — Minimum MVP Member profile

**Date:** 23 August 2026  
**Status:** Accepted

The MVP Member profile contains:

- **Display name** — required.
- **Profile photo** — optional.
- **Organisation function or skill** — optional; for example singer, tabla player, or volunteer.
- **Phone number** — optional and private by default.
- **Email address** — sourced from the account and private by default.
- **Internal notes** — optional and visible only to the Owner/Admins.

Legal name, date of birth, emergency contact, dietary information, and travel-document data are not part of the initial profile. They should be introduced only when a validated workflow requires them and appropriate privacy controls have been designed.

**Rationale:** The profile should contain only what is needed for identity and group coordination in the MVP.

---

## D-047 — Booking and Event detail structure

**Date:** 23 August 2026  
**Status:** Accepted

The Booking/Event workspace follows the canonical Sahno product specification and contains these logical sections:

1. **Overview** — status, date, venue, times, dress, and important participant-facing notes.
2. **People** — selected participants and administrative availability information.
3. **Responsibilities** — who is doing or bringing what.
4. **Rehearsals** — activities linked to the parent engagement.
5. **Resources** — repertoire, files, and links.
6. **Discussion** — contextual engagement conversation.
7. **Admin** — customer details, private notes, permitted finances, and activity history.

Members see only the sections and content permitted by the accepted permissions model. The logical structure is accepted, but whether it appears as tabs, cards, nested pages, or a scrolling view remains open for prototype testing.

**Rationale:** This preserves the structured engagement workspace defined in `Sahno_Product_Discovery_and_MVP.md` while separating operational, participant, and protected administrative information.

---

## D-048 — Checklist-based Event readiness

**Date:** 23 August 2026  
**Status:** Accepted

The MVP represents Event readiness as an actionable checklist rather than a percentage score.

Initial readiness items include:

- participant lineup resolved;
- venue added;
- call or sound-check time added;
- performance or start time added;
- responsibilities assigned;
- dress instructions added;
- rehearsal organised; and
- repertoire or resources ready.

An Owner/Admin can mark an item **Not required** when it does not apply to that Event. The exact default items may vary by event type as the model is validated.

**Rationale:** A checklist tells organisers what action remains. A percentage risks implying precision without adding operational clarity and can be reconsidered after real usage.

---

## D-049 — MVP notification channels

**Date:** 23 August 2026  
**Status:** Accepted

The MVP uses:

- **In-app notifications** for important activity.
- **Email notifications** for availability requests, reminders, confirmation, postponement, cancellation, and major date, venue, or time changes.

Native push notifications are deferred until the mobile delivery approach is decided. Push infrastructure is not a prerequisite for the initial pilot.

Users should eventually be able to control non-critical notification preferences, but critical booking-state changes must remain reliably communicated.

**Rationale:** In-app plus email provides a dependable pilot workflow without prematurely committing to native mobile infrastructure.

---

## D-050 — MVP calendar behaviour

**Date:** 23 August 2026  
**Status:** Accepted

Sahno's calendar displays:

- items needing the Member's response;
- Tentative Events; and
- Confirmed Events.

Members can add Tentative or Confirmed Events to their personal calendar. Tentative entries must remain clearly labelled **Tentative**.

Automatic two-way synchronisation with Google Calendar, Apple Calendar, or Outlook is deferred beyond the MVP.

**Rationale:** Members need a useful future-commitment view and a simple path into their personal calendar without requiring complex external synchronisation for the pilot.

---

## D-051 — Minimum Enquiry information

**Date:** 23 August 2026  
**Status:** Accepted

Only an **Enquiry title** is required to save a Draft.

Before sending an availability request, the Owner/Admin must provide a proposed date or date range. Venue, exact times, and customer contact information may remain TBC or absent at that stage.

**Rationale:** Early opportunities commonly arrive with incomplete information. Sahno should preserve what is known without forcing organisers to invent details, while ensuring Members receive enough date context to answer availability meaningfully.

---

## D-052 — Booking and Event list groupings

**Date:** 23 August 2026  
**Status:** Accepted

The Owner/Admin **Bookings** view groups engagements by:

- New enquiries and Drafts;
- Checking Availability;
- Tentative;
- Confirmed;
- Postponed; and
- History: Completed and Cancelled.

The Member **Events** view groups visible engagements by:

- Needs your response;
- Tentative;
- Confirmed; and
- History: Completed and Cancelled Events in which the Member participated.

Search and filtering are supported around these logical groups. The exact ordering, tab structure, filters, and visual arrangement may change during prototyping and implementation testing without changing the underlying lifecycle model.

**Rationale:** Role-appropriate groupings make the same Engagement domain understandable as an administrative pipeline and a personal participation schedule.

---

## D-053 — Mobile-first React Native delivery

**Date:** 23 August 2026  
**Status:** Accepted

Sahno will be designed and built mobile-first using **React Native**, targeting both **iOS and Android**.

- Primary workflows must be comfortable on phone-sized screens.
- Owner/Admin operations must not depend on a desktop layout.
- iOS and Android should share the core product experience while respecting platform conventions where useful.
- A web or desktop experience is not the primary delivery target for the initial build and can be considered separately later.

**Rationale:** Members and organisers need to respond, coordinate, and check event details while mobile. React Native supports both target mobile platforms from a shared application codebase.

---

## D-054 — Primary mobile journey wireframe direction

**Date:** 23 August 2026  
**Status:** Accepted

The initial low-fidelity mobile journey is accepted as the direction for further design:

1. Owner/Admin Home prioritising needs-attention work.
2. Create a minimally complete Enquiry.
3. Select Members and track availability.
4. Member responds without seeing others' answers.
5. Available response creates a Tentative commitment.
6. Confirmed Booking uses readiness and a structured workspace.
7. Member day-of view prioritises location, timing, responsibilities, and dress.

This acceptance validates the flow and information hierarchy, not final branding, visual design, copy, spacing, or component styling. Those remain iterative.

---

## D-055 — MVP authentication methods

**Date:** 23 August 2026  
**Status:** Accepted

The Sahno mobile MVP supports:

- passwordless email sign-in using a one-time code;
- Continue with Google; and
- Sign in with Apple.

The MVP does not use passwords and therefore does not require password creation, storage, or password-reset flows.

Email authentication aligns naturally with organisation invitations. Sign in with Apple is included alongside Google for the iOS release and its login-services requirements.

**Reference:** [Apple App Review Guidelines — Login Services](https://developer.apple.com/app-store/review/guidelines/#login-services)

---

## D-056 — First-time onboarding branches

**Date:** 23 August 2026  
**Status:** Accepted

After authentication:

- A person who followed a valid invitation sees the organisation identity and confirms **Join organisation**.
- A person without an invitation can choose **Create an organisation** or enter/paste an invite link.
- Creating an organisation makes that person its Owner.
- Joining through an invitation makes that person a Member.
- Admin access is never granted through an invitation and remains a later Owner-only promotion under D-014 and D-045.

**Rationale:** The flow supports both founders and invited participants without adding public organisation discovery or ambiguous role selection.

---

## D-057 — Minimum organisation creation fields

**Date:** 23 August 2026  
**Status:** Accepted

Creating an organisation collects:

- **Organisation name** — required.
- **Organisation logo** — optional.
- **Group type** — optional; initial examples include music, choir, dance, theatre, community, and other.
- **Time zone** — detected automatically and editable.

Invitations, Member functions, and additional settings are handled after creation rather than extending initial onboarding.

**Rationale:** A person should be able to establish the organisation quickly and enter the real workflow without completing non-essential setup.

---

## D-058 — New Owner setup checklist

**Date:** 23 August 2026  
**Status:** Accepted

After creating an organisation, the Owner lands on the Admin Home with a small dismissible setup checklist:

- Invite your Members.
- Create your first Enquiry.
- Optionally add an organisation logo.
- Optionally review organisation settings.

Each item opens the corresponding real product feature rather than a separate tutorial. The checklist disappears when completed or dismissed.

**Rationale:** The checklist guides the Owner toward first value without turning setup into a long mandatory onboarding sequence.

---

## D-059 — React Native framework and build tooling

**Date:** 23 August 2026  
**Status:** Accepted

Sahno will use:

- **Expo** as the React Native framework;
- **Expo Router** for application navigation;
- **TypeScript**;
- **Expo development builds** for development and device testing; and
- **EAS Build** for iOS and Android application builds.

The project will not depend on Expo Go as its long-term development or production runtime. Native capabilities may be integrated through supported Expo modules, config plugins, and native project generation when required.

**Rationale:** This matches the founder's existing comfort while reducing setup and mobile build friction without preventing native capabilities.

**References:**

- [React Native environment setup](https://reactnative.dev/docs/environment-setup)
- [Expo development builds](https://docs.expo.dev/develop/development-builds/introduction/)

---

## D-060 — Custom ASP.NET Core backend

**Date:** 23 August 2026  
**Status:** Accepted

Sahno will use a custom **ASP.NET Core Web API** backend. Supabase is explicitly rejected as the MVP backend platform.

The API will own Sahno's domain rules, organisation authorisation, engagement lifecycle transitions, availability workflows, notification orchestration, and protected financial/customer operations.

The exact .NET runtime version, database, hosting platform, identity implementation, file storage, and background-job approach are decided separately.

**Rationale:** A custom .NET backend matches the founder's preferred technology and provides direct control over the product's domain and security boundaries.

---

## D-061 — .NET runtime and Onion Architecture

**Date:** 23 August 2026  
**Status:** Accepted

The backend uses **ASP.NET Core on .NET 10 LTS** as a REST API and follows a pragmatic Onion/Clean Architecture.

Initial solution projects:

- `Sahno.Domain` — entities, value objects, domain rules, domain events, and domain exceptions; no infrastructure dependencies.
- `Sahno.Application` — use cases, application services, commands/queries, ports/interfaces, validation, and authorisation orchestration; depends on Domain.
- `Sahno.Infrastructure` — persistence, identity adapters, storage, email, external services, and implementation of Application ports; depends inward on Application and Domain.
- `Sahno.Api` — HTTP endpoints, middleware, authentication plumbing, API composition, and dependency injection; composes Application and Infrastructure.
- `Sahno.Contracts` — stable API request/response contracts where separation materially helps mobile-client integration.
- Corresponding unit, integration, and architecture test projects.

The system is a **modular monolith** with one primary API deployment. Feature boundaries include Organisations, Membership, Engagements, Availability, Responsibilities, Rehearsals, Resources, Discussion, Notifications, and Finance. Microservices are not part of the MVP.

A background Worker may be introduced for scheduled reminders and durable asynchronous work when required, without splitting the domain into distributed services.

Dependency direction must remain inward: Domain knows nothing about Application, Infrastructure, or API; Application knows nothing about concrete Infrastructure.

**Rationale:** This structure matches the founder's preferred design, protects the domain model from framework concerns, and remains appropriate for a side project when implemented as one deployable modular system.

**Reference:** [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

---

## D-062 — PostgreSQL and EF Core persistence

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses:

- **PostgreSQL** as the primary relational database;
- **Entity Framework Core 10**;
- the **Npgsql** Entity Framework Core provider;
- EF Core migrations owned by `Sahno.Infrastructure`;
- UUID identifiers;
- UTC timestamps;
- optimistic concurrency protection for important editable records; and
- database constraints in addition to application and domain validation.

The architecture will not introduce a generic repository abstraction over EF Core. Focused repositories or domain-oriented persistence abstractions may be used where they provide meaningful boundaries or testability.

**Rationale:** Sahno's organisation, membership, lifecycle, availability, preparation, and finance data is strongly relational. PostgreSQL provides robust integrity while EF Core integrates naturally with the selected .NET architecture.

**Reference:** [Npgsql Entity Framework Core provider](https://www.npgsql.org/efcore/)

---

## D-063 — Auth0 identity provider

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses **Auth0** as its managed identity provider for:

- passwordless email one-time codes;
- Google sign-in;
- Sign in with Apple;
- access and refresh token handling; and
- account identity and social-account linking.

The React Native app authenticates with Auth0 and presents an access token to the ASP.NET Core API. The API validates the token and maps the external subject to a Sahno account.

Auth0 proves identity only. Sahno's own PostgreSQL database and .NET API remain authoritative for profiles, organisations, memberships, Owner/Admin/Member roles, Admin financial permission, and all domain authorisation.

No organisation role or protected permission is accepted directly from client-supplied data or general Auth0 profile metadata.

**Rationale:** Managed identity avoids building sensitive OTP delivery, social-provider integration, token rotation, and account-linking infrastructure while preserving complete control over Sahno's product data and authorisation.

**References:**

- [Auth0 React Native quickstart](https://auth0.com/docs/quickstart/native/react-native)
- [Auth0 ASP.NET Core Web API quickstart](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)

---

## D-064 — React Native data, state, forms, and credential storage

**Date:** 23 August 2026  
**Status:** Accepted

The Sahno mobile application uses:

- **TanStack Query** for API server state, caching, retries, mutations, and invalidation;
- **Zustand** only for small client-side state such as active organisation context and temporary UI state;
- **React Hook Form** with **Zod** for form state and immediate client validation; and
- platform-secure credential storage for Auth0 credentials and refresh material.

Redux is not part of the MVP. Sensitive credentials must not be stored in plain AsyncStorage. Client validation improves usability, but ASP.NET domain/application validation and database constraints remain authoritative.

**Rationale:** Separating server state, small UI state, form state, and credentials keeps the mobile architecture understandable and reduces unnecessary global-state complexity.

---

## D-065 — DigitalOcean production infrastructure

**Date:** 23 August 2026  
**Status:** Accepted

Sahno's initial production infrastructure will use:

- **DigitalOcean App Platform** for the containerised ASP.NET Core API;
- **DigitalOcean Managed PostgreSQL** for the production database;
- **DigitalOcean Spaces** for organisation logos, files, recordings, and resources; and
- an **App Platform Worker** when scheduled reminders and durable background jobs are implemented.

Application secrets initially use App Platform's encrypted environment-variable configuration. Error monitoring is handled separately. Kubernetes and a self-managed production PostgreSQL database on a general-purpose Droplet are not part of the MVP architecture.

All DigitalOcean integrations remain behind Infrastructure-layer ports so the Domain and Application projects are not coupled to the hosting provider.

**Rationale:** This provides managed database backups, storage, and straightforward container deployment with lower expected operational complexity and cost than the considered Azure setup, while avoiding the backup and recovery burden of an all-in-one Droplet.

**References:**

- [DigitalOcean App Platform](https://docs.digitalocean.com/products/app-platform/)
- [DigitalOcean Managed PostgreSQL](https://docs.digitalocean.com/products/databases/postgresql/)
- [DigitalOcean Spaces](https://docs.digitalocean.com/products/spaces/)

---

## D-066 — Durable notifications, Resend, and Outbox

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses:

- **Resend** for transactional product email;
- a production custom email-provider configuration for Auth0 authentication emails;
- persisted in-app notifications in PostgreSQL;
- a background Worker for asynchronous delivery; and
- a **transactional Outbox pattern** for domain-triggered notification work.

Domain actions such as confirmation, postponement, cancellation, and significant Event changes commit their state and corresponding Outbox records in the same database transaction. The Worker processes Outbox records with retries and idempotency so an email-provider outage does not roll back or corrupt the underlying Booking action.

Expo push notifications may be added later as another delivery adapter without changing the domain event or notification model. Sahno stores user notification preferences for non-critical channels.

**Rationale:** Notification intent must not be lost between a successful domain change and an external provider call. Separating durable intent from delivery improves reliability and keeps Resend out of the Domain/Application core.

**References:**

- [Resend documentation](https://resend.com/docs)
- [Auth0 custom email providers](https://auth0.com/docs/customize/email/smtp-email-providers)

---

## D-067 — SignalR realtime delivery

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses **ASP.NET Core SignalR** for realtime delivery of:

- Event discussion messages;
- availability-response progress;
- new in-app notifications; and
- important Booking/Event updates while relevant screens are active.

REST remains the authoritative data interface. SignalR does not replace durable API reads or writes. After reconnecting, the mobile application invalidates and refreshes relevant TanStack Query data to recover from missed events.

SignalR connections and groups must be authorised by the ASP.NET Core API using Sahno's organisation and Engagement permissions. A client cannot join an organisation or Engagement channel merely by knowing its identifier.

**Rationale:** Realtime coordination improves discussion and organiser awareness, while REST-backed recovery prevents correctness from depending on an uninterrupted mobile connection.

**Reference:** [ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction)

---

## D-068 — Online-first mobile behaviour

**Date:** 23 August 2026  
**Status:** Accepted

The Sahno MVP is **online-first with graceful offline reading**, not a fully offline-synchronised application.

- Recently loaded Events and day-of details remain readable from persisted local query cache.
- The application clearly indicates offline state.
- Unsaved form text is preserved locally where practical.
- Availability responses, discussion messages, lifecycle changes, permissions, and financial writes are not treated as complete until confirmed by the API.
- Failed writes remain visible with a Retry action and an accurate failure state.
- The MVP does not implement a general offline mutation queue.
- Optimistic updates are limited to low-risk interactions and roll back visibly when rejected.
- Concurrency conflicts return explicit feedback rather than silently overwriting newer server data.

**Rationale:** Essential mobile information remains useful during poor connectivity while avoiding unsafe conflict resolution for booking, availability, permission, and finance data.

---

## D-069 — Monitoring, logging, and diagnostic privacy

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses:

- **Serilog** for structured ASP.NET Core and Worker logs;
- correlation/request identifiers across mobile requests, API handling, and background work;
- **Sentry** for React Native crashes and unhandled API/Worker exceptions;
- DigitalOcean platform logs for operational inspection; and
- API liveness and database-readiness health endpoints.

Logs and diagnostic events must never intentionally include access or refresh tokens, customer financial values, private organiser notes, Member contact details, or discussion-message contents. Sensitive request and response bodies are not logged.

Privacy-safe product analytics are deferred until specific validation questions and an explicit event schema are defined.

**Rationale:** The pilot requires enough diagnostic context to investigate failures without creating a secondary store of sensitive organisation data.

---

## D-070 — Risk-focused automated testing

**Date:** 23 August 2026  
**Status:** Accepted

Backend testing uses:

- **xUnit** for Domain and Application unit tests;
- ASP.NET Core `WebApplicationFactory` for API integration tests;
- **Testcontainers** with real PostgreSQL for persistence and integration behaviour;
- architecture tests that enforce Onion project dependency rules;
- explicit permission/tenant-isolation tests for protected endpoints; and
- comprehensive Engagement lifecycle-transition tests.

Mobile testing uses:

- **React Native Testing Library** for components and user flows;
- unit tests for client state and validation;
- **Maestro** smoke/end-to-end tests for the critical iOS and Android journey; and
- controlled API responses through test doubles or a dedicated test API environment.

Testing prioritises organisation isolation, permissions, lifecycle transitions, availability, invitations, protected finance, and other high-risk behaviour. The project does not optimise for an arbitrary coverage percentage.

**Rationale:** Sahno's largest risks are incorrect access and workflow state, not untested presentation details. Real PostgreSQL integration coverage reduces false confidence from database substitutes.

---

## D-071 — Sahno monorepo structure

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses one monorepo with this baseline structure:

```text
Sahno/
├─ apps/
│  └─ mobile/
├─ services/
│  └─ api/
│     ├─ Sahno.slnx
│     ├─ src/
│     │  ├─ Sahno.Api/
│     │  ├─ Sahno.Application/
│     │  ├─ Sahno.Contracts/
│     │  ├─ Sahno.Domain/
│     │  └─ Sahno.Infrastructure/
│     └─ tests/
├─ packages/
│  └─ api-client/
├─ infra/
└─ docs/
```

- `apps/mobile` contains the Expo React Native application.
- `services/api` contains the .NET Onion Architecture solution and tests.
- `packages/api-client` contains the generated TypeScript API client.
- `infra` contains local containers and deployment configuration.
- `docs` remains the project source of truth.
- **pnpm** manages JavaScript/TypeScript workspace dependencies.
- The TypeScript API client is generated from the ASP.NET Core OpenAPI document.

**Rationale:** Contract, backend, mobile, infrastructure, test, and documentation changes can be reviewed and committed atomically without introducing separate-repository coordination overhead.

---

## D-072 — Environments, CI/CD, and guided implementation

**Date:** 23 August 2026  
**Status:** Accepted

Sahno uses:

- a GitHub repository with protected `main`;
- pull-request checks for mobile quality gates, .NET build/tests, PostgreSQL integration tests, architecture tests, and generated OpenAPI-client consistency;
- **Local**, **Staging**, and **Production** environments;
- Docker Compose for local API and PostgreSQL dependencies;
- automatic Staging deployment from accepted `main` changes;
- manually approved Production deployment;
- controlled database-migration deployment jobs rather than automatic production migrations during API startup;
- EAS Development, Preview, and Production profiles;
- Preview mobile builds connected to Staging only; and
- strict environment-specific secret separation outside Git.

### Guided learning requirement

CI/CD and GitHub automation will be implemented as a collaborative learning workflow.

- Codex explains the purpose, triggers, permissions, jobs, steps, secrets, artifacts, caching, and failure behaviour before or while each workflow is introduced.
- The founder performs meaningful setup and execution steps rather than receiving an unexplained completed pipeline.
- Workflows are introduced incrementally, beginning with CI checks before deployment automation.
- Each stage is run, observed, deliberately understood, and corrected before adding the next stage.
- Documentation explains how to operate and troubleshoot the pipeline after initial setup.

**Rationale:** Sahno should gain a reliable delivery pipeline while also building the founder's practical GitHub Actions and CI/CD knowledge.
