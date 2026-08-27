# Sahno --- Product Discovery, Vision & MVP Specification

**Status:** Working product specification\
**Product name:** Sahno\
**Tagline:** *Make it happen, together.*\
**Primary initial use case:** Community and performing groups\
**Initial real-world pilot:** Australian Qawwal Party (AQP)

------------------------------------------------------------------------

## 1. Executive Summary

Sahno is a community and group coordination platform designed to solve
the operational problems that happen around real-world events.

The original idea began as a better way for groups to communicate about
events, availability and reminders. Through discussion of the actual
workflow used by a performing group, the problem became much clearer:
the difficult part is not simply creating an event or maintaining a
group chat. The difficult part is taking an opportunity from an initial
enquiry through availability, confirmation, preparation and successful
completion without important information or responsibilities getting
lost.

Today, much of this coordination happens in WhatsApp chats, personal
calendars and people's memories. Potential dates are mixed with
confirmed events. Some members respond immediately while others need to
be chased. Venues, performance times and sound-check times can arrive
very late. Rehearsal information, repertoire, equipment responsibilities
and dress instructions are scattered through messages. Organisers have
to remember what is still missing. Even performer payments can be
forgotten.

Sahno aims to provide one structured place where organisers can see what
needs attention and members can quickly understand:

-   What do I need to respond to?
-   What events am I potentially or definitely committed to?
-   Where do I need to be?
-   What time is sound check or call time?
-   What am I responsible for bringing or doing?
-   What changed?
-   What still needs to happen?

The long-term opportunity is broader than music. The same coordination
problems exist in choirs, dance groups, drama groups, cultural
associations, spiritual/community organisations and other volunteer or
semi-professional groups.

------------------------------------------------------------------------

## 2. How the Idea Emerged

The initial concept was closer to a messaging/event application for
organisations: create events, ask members whether they are available,
send reminders, maintain a calendar and allow event-related discussion.

Competitor exploration, particularly around products such as Spond and
choir-oriented applications, showed that many individual features
already exist. Spond, for example, can collect responses and remind
people who have not responded.

However, the existing products felt either sports-oriented, too simple
for the actual performing-group workflow, or focused on
practice/repertoire rather than the organiser's complete operational
process.

The key product insight became:

> Sahno should not simply be another group calendar or RSVP application.
> It should help an organiser take an opportunity from enquiry to
> completion while keeping every participant informed and coordinated.

------------------------------------------------------------------------

## 3. Real-World Problem: The Current AQP Workflow

### 3.1 An enquiry arrives

A potential customer contacts an organiser through any channel, for
example:

-   Phone
-   Email
-   Website
-   Instagram/social media
-   Personal contact

At this point the booking is not confirmed. Often only partial
information is available.

Example:

-   Proposed date: 17 October
-   Location: Canberra
-   Time: TBC
-   Venue: TBC
-   Event type: private/cultural event

### 3.2 Availability is requested

The organisers ask the group whether members are available on the
proposed date.

Some people respond immediately. Others can take one or two days. The
organiser then manually checks who has not answered and sends follow-up
messages naming those people.

This creates several problems:

-   Outstanding responses are easy to lose.
-   Organisers repeatedly chase people manually.
-   Members may forget whether they responded.
-   Multiple potential dates are mixed together in the same chat.

### 3.3 Available does not mean confirmed

Even if everyone is available, the event may still not happen.

The organiser must return to the customer, continue
discussions/contracts and eventually tell the group that the engagement
is:

-   Confirmed
-   Cancelled
-   Postponed
-   Still tentative

This distinction is essential.

### 3.4 Preparation begins after confirmation

Once confirmed, the group discusses:

-   Songs/numbers to perform
-   Rehearsal dates
-   YouTube/reference links
-   Lyrics
-   Musical scale
-   Equipment
-   Who is bringing what
-   Dress/attire
-   Venue
-   Performance time
-   Sound-check/call time
-   Interstate travel where relevant

Much of this information currently exists as chronological WhatsApp
messages rather than structured information attached to the specific
event.

### 3.5 Critical details may arrive very late

A particularly strong real-world pain point is that venue, timing and
sound-check details may be supplied close to the event.

For example, the venue for a next-day event may only be shared the day
before, with the sound-check time supplied even later.

The main day-of question for members is often simply:

> Where do I need to be, at what time, and what am I responsible for?

### 3.6 Payments can be forgotten

Commercial arrangements are primarily handled by organisers and are not
normally discussed with ordinary members.

However, performer payment obligations can be forgotten. This revealed
that simple organiser-only payment tracking could solve a genuine
operational problem.

------------------------------------------------------------------------

## 4. Product Vision

Sahno should become the operational home for a community or performing
group.

It should have two complementary experiences.

### Member experience

> **What do I need to know or do?**

Members should have a simple experience focused on:

-   Requests requiring a response
-   Tentative commitments
-   Confirmed commitments
-   Upcoming events
-   Changes to important details
-   Rehearsals
-   Their responsibilities
-   Relevant resources
-   Event discussion

### Organiser experience

> **What do I need to organise or chase?**

Organisers need an operational dashboard focused on:

-   New/potential enquiries
-   Availability progress
-   Outstanding responses
-   Tentative bookings
-   Confirmed bookings
-   Missing event information
-   Upcoming deadlines/events
-   Responsibilities that are not assigned
-   Outstanding performer payments
-   Other items requiring attention

This distinction is a central Sahno design principle.

------------------------------------------------------------------------

## 5. Core Product Lifecycle

The main object was initially discussed as an **Engagement** rather than
merely an Event because it has a lifecycle before it becomes a confirmed
event.

Conceptually:

**Enquiry → Checking Availability → Tentative / Ready to Confirm →
Confirmed → Preparation → Completed**

Alternative outcomes:

-   Cancelled
-   Postponed

An engagement is allowed to exist with incomplete information.

For example:

> Canberra Performance\
> 17 October\
> Canberra\
> Time: TBC\
> Venue: TBC\
> Status: Checking availability

The application should not force organisers to invent details they do
not yet know.

------------------------------------------------------------------------

## 6. Availability Workflow

Availability is a core Sahno feature.

An organiser creates a potential engagement and chooses which members
are required.

Members receive a request such as:

> **Are you available?**\
> Saturday 17 October\
> Canberra\
> Time: TBC

Response options:

-   Available
-   Maybe
-   Unavailable

The organiser sees an aggregate view such as:

> 8 / 11 responded\
> 7 available\
> 1 maybe\
> 3 awaiting response

The organiser can remind outstanding members without manually
identifying them in a group chat.

Sahno should eventually support sensible automatic reminders, but the
MVP can begin with manual reminders plus notification infrastructure.

------------------------------------------------------------------------

## 7. Tentative vs Confirmed Commitments

This distinction is fundamental.

If a member says they are available, the date should appear in their
schedule as a **tentative commitment**, not as a confirmed booking.

Example:

> 🟡 17 Oct --- Canberra Performance\
> Tentative --- you've said you're available

Once the organiser confirms the booking:

> 🟢 17 Oct --- Canberra Performance\
> Confirmed

If it falls through:

> 🔴 17 Oct --- Canberra Performance\
> Cancelled

This gives members a future-facing view of both potential and confirmed
commitments.

------------------------------------------------------------------------

## 8. My Schedule

Members should be able to answer:

> What have I committed to over the next few months?

The schedule should distinguish:

### Needs response

Events where the member has not yet answered an availability request.

### Tentative

Potential engagements for which the member has said they are available.

### Confirmed

Bookings that the organiser has formally confirmed.

Potential views:

-   Upcoming
-   Month
-   Calendar

The product should provide a stronger future-events experience than the
alternatives that originally felt too simplistic.

------------------------------------------------------------------------

## 9. Confirmed Engagement Workspace

Once an engagement becomes confirmed, it becomes the operational
workspace for that event.

Potential sections:

### Overview

-   Event name
-   Date
-   City/location
-   Venue
-   Arrival/call time
-   Sound-check time
-   Performance/start time
-   Status
-   Dress code
-   Notes

### People

-   Participating members
-   Their role in the event
-   Attendance/confirmation state

### Rehearsals

Related rehearsal/practice sessions.

### Responsibilities

Who is bringing or doing what.

### Repertoire / resources

Songs, lyrics, recordings, reference links and files.

### Discussion

A conversation specifically attached to the event.

This contextual structure is important. A discussion about the Canberra
event should live inside the Canberra event rather than among messages
about several unrelated bookings.

------------------------------------------------------------------------

## 10. Event Readiness

A strong product concept discovered during the discussion is **event
readiness**.

Rather than expecting the organiser to remember what is missing, Sahno
can show it explicitly.

Example:

> **Qawwali Night --- 72% ready**
>
> ✓ Members confirmed\
> ✓ Venue\
> ✓ Performance time\
> ⚠ Sound-check missing\
> ✓ Dress code\
> ○ Set list incomplete\
> ✓ Equipment assigned

The purpose is not necessarily to obsess over a percentage. The
important idea is:

> Sahno should tell the organiser what has not yet been organised.

This can become one of the strongest differentiators of the product.

------------------------------------------------------------------------

## 11. Rehearsals and Related Activities

Rehearsals should not necessarily appear as unrelated standalone events.

A performance may have related activities:

> Canberra Performance\
> ↳ Rehearsal #1\
> ↳ Rehearsal #2\
> ↳ Final rehearsal

A rehearsal may have:

-   Date/time
-   Location
-   Attendance
-   Notes
-   Discussion
-   Related repertoire

The underlying event/activity model should remain generic enough to
support:

-   Performance
-   Rehearsal
-   Meeting
-   Workshop
-   Recording
-   Community event
-   Other

------------------------------------------------------------------------

## 12. Responsibilities / "Who's Bringing What?"

This is a simple feature with broad applicability.

Example for a music group:

  Item / responsibility   Requirement   Assigned to
  ----------------------- ------------- -------------
  Tabla                   C# scale      Anuraag
  Harmonium               Standard      Kuldeep
  Mic stands              4             Adarsh
  PA system               Full system   Unassigned

The same feature can work for other groups:

-   Drama: props
-   Dance: costumes
-   Cultural association: food, tables, banners
-   Community event: volunteers, equipment, setup tasks

Therefore the underlying concept should be **responsibilities/tasks**,
not a music-specific "equipment" table.

------------------------------------------------------------------------

## 13. Repertoire and Resources

Sahno should not attempt to become specialised rehearsal software in the
MVP.

For a music group, a reusable repertoire entry could include:

> Tajdar-e-Haram\
> Scale: F#\
> Lyrics\
> YouTube/reference link\
> Audio/file\
> Notes

An event can then select items from the organisation's repertoire rather
than repeatedly uploading the same lyrics and links.

The same generic resource concept can eventually adapt to:

-   Choir: songs/sheet music
-   Dance: routines/music
-   Drama: scripts/scenes
-   Community organisations: documents/resources

Advanced music features are outside the initial MVP.

------------------------------------------------------------------------

## 14. Dress / Attire

An event can contain dress instructions such as:

> Black sherwani\
> White pants\
> Black shoes

An image/reference may eventually be attached.

This is simple but valuable because attire instructions are otherwise
easily buried in chat history.

------------------------------------------------------------------------

## 15. Travel

Interstate travel was identified as a meaningful future feature.

Potential information:

-   Flights
-   Accommodation
-   Ground transport
-   Travellers
-   Arrival/departure information

Member profiles may eventually hold private travel information such as:

-   Legal/full name
-   Date of birth
-   Dietary requirements
-   Emergency contact

Sensitive information should be role-protected. Passport details should
not be part of the initial MVP without a separate security decision.

Travel management is **not currently required for the first MVP**, but
the architecture should not make it impossible later.

------------------------------------------------------------------------

## 16. Customer / Enquiry Information

For organisers, Sahno can begin an engagement as an enquiry.

Basic customer information should be supported early:

-   Contact name
-   Organisation/company
-   Phone
-   Email
-   Event type
-   Event name
-   Proposed date
-   City/location
-   Venue if known
-   Timing if known
-   Notes

A future version may evolve this into a lightweight customer history/CRM
where organisers can see repeat customers and previous engagements.

A full CRM is not required for MVP.

------------------------------------------------------------------------

## 17. Finance and Payment Tracking

The product should **not become accounting software**.

However, basic event financial tracking is operationally useful.

Potential organiser-only fields:

-   Quoted amount
-   Agreed amount
-   Deposit required
-   Deposit received
-   Balance due

More importantly for the real initial use case, Sahno should support
simple performer payment obligations.

Example:

  Member       Amount Status
  ---------- -------- ---------
  Member A      \$400 Paid
  Member B      \$450 Pending
  Member C      \$350 Pending

This can feed the organiser dashboard:

> 2 performer payments outstanding

The MVP should keep this intentionally lightweight:

-   No bookkeeping
-   No tax calculations
-   No Xero integration
-   No Stripe/payment processing
-   No invoicing engine

Those can be considered later.

Financial information must be protected by role/permission and should
not automatically be visible to ordinary members.

------------------------------------------------------------------------

## 18. Organiser Dashboard

The organiser dashboard may become one of Sahno's strongest paid/product
differentiators.

The dashboard should answer:

> **What do I need to chase?**

Example:

### Needs Attention

> Melbourne --- 2 members haven't responded\
> Canberra --- venue missing\
> Sydney --- sound-check time missing\
> 2 performer payments outstanding

### Pipeline

-   Potential engagements
-   Checking availability
-   Tentative / awaiting customer confirmation
-   Confirmed
-   This month

### Upcoming

A clear list of the next confirmed/tentative engagements.

Potential future commercial summary:

-   Potential booking value
-   Confirmed value
-   Received
-   Outstanding

The MVP should prioritise operational attention over sophisticated
analytics.

------------------------------------------------------------------------

## 19. Member Home / Day-of Experience

The member experience should remain much simpler than the organiser
experience.

A member opening Sahno near an event should immediately see:

> **Saturday --- Qawwali Night**
>
> Venue: Bryan Brown Theatre\
> Performance: 6:30 PM\
> Sound check: 3:30 PM\
> Dress: Black sherwani / white pants
>
> **You're responsible for** - Harmonium - Mic stands
>
> **Rehearsal** Thursday 7:00 PM
>
> **Latest update** Venue updated 2h ago

The day-of design principle is:

> **Where do I need to be, when do I need to be there, and what am I
> responsible for?**

------------------------------------------------------------------------

## 20. Notifications

Notifications are essential to the product.

Examples:

### Availability request

> Can you attend Canberra --- 17 Oct?

### Outstanding response reminder

> Your availability is still required for Canberra --- 17 Oct.

### Booking confirmed

> Canberra Performance has been confirmed.

### Event updated

> Venue has been added.

### Event reminder

> Canberra Performance tomorrow. Sound check: 3:30 PM. You're bringing:
> Harmonium.

### Important missing information

Organiser-facing:

> Event is 5 days away and sound-check time is still missing.

The exact automatic reminder rules should be refined during product
design.

------------------------------------------------------------------------

## 21. Event Discussion

Sahno should support contextual event discussion.

This addresses one of the weaknesses identified in alternative tools and
the core WhatsApp problem.

Rather than all organisational discussion living in one group chat:

-   Event discussion belongs to the event.
-   Rehearsal discussion can belong to the rehearsal.
-   Resources remain attached to the relevant engagement.

Sahno does not need to replace all social conversation. It needs to
prevent operational information from becoming lost in general
conversation.

------------------------------------------------------------------------

## 22. Roles and Permissions

Detailed permissions are the next product-design task, but the initial
conceptual roles are:

### Owner / Admin

-   Manage organisation
-   Manage members and roles
-   Access organisation settings
-   Full organiser capabilities

### Organiser / Manager

-   Create enquiries/engagements
-   Request availability
-   Send reminders
-   Confirm/cancel/postpone engagements
-   Manage event details
-   Assign responsibilities
-   Manage rehearsals/resources
-   Access permitted customer/commercial information
-   Track payments if authorised

### Member

-   View engagements relevant to them
-   Respond to availability
-   View tentative/confirmed schedule
-   View event details
-   View assigned responsibilities
-   Participate in event discussion
-   Access relevant resources

Commercial/customer/private member data must not automatically be
visible to every member.

The exact permission matrix remains to be locked.

------------------------------------------------------------------------

## 23. MVP Scope

The current proposed Sahno MVP contains the following core systems.

### Organisation and membership

-   Create organisation
-   Invite/join members
-   Member profiles
-   Roles

### Engagements

-   Create enquiry/potential engagement
-   Incomplete/TBC information allowed
-   Engagement status/lifecycle
-   Confirm/cancel/postpone

### Availability

-   Select required members
-   Available / Maybe / Unavailable
-   Outstanding response view
-   Reminder to non-responders

### Schedule

-   Needs response
-   Tentative commitments
-   Confirmed commitments
-   Upcoming/month/calendar views

### Confirmed event workspace

-   Date
-   Venue
-   City
-   Arrival/call time
-   Sound-check
-   Start/performance time
-   Dress
-   Notes

### Responsibilities

-   Assign tasks/items to members
-   Track assigned/unassigned/completed state

### Rehearsals

-   Related activities linked to parent engagement
-   Basic attendance/details

### Resources

-   Links
-   Lyrics/text
-   Recordings/files
-   Basic reusable repertoire

### Discussion

-   Event-specific conversation

### Notifications

-   New availability request
-   Reminders
-   Confirmation
-   Important event changes
-   Upcoming event reminder

### Organiser dashboard

-   Needs attention
-   Outstanding responses
-   Potential engagements
-   Confirmed engagements
-   Missing critical details
-   Upcoming events

### Basic organiser-only information

-   Customer/contact information
-   Private organiser notes
-   Lightweight payment tracking

------------------------------------------------------------------------

## 24. Explicitly Not MVP

The following ideas are valuable but should not be allowed to explode
the first release:

-   Full customer CRM
-   Full accounting
-   Xero integration
-   Stripe/payment processing
-   Public ticketing
-   Contract management/e-signatures
-   Detailed budgeting/accounting reports
-   Advanced travel booking
-   Passport storage
-   Advanced analytics
-   AI assistant
-   Voice/video calling
-   Advanced choir rehearsal features
-   Sheet music annotation
-   Sophisticated audio player
-   Large-scale public social network
-   Complex expense management

The architecture may anticipate some of these, but they should not block
delivery of the core workflow.

------------------------------------------------------------------------

## 25. Product Differentiation / USP

Sahno is not simply:

-   A group chat
-   A calendar
-   An RSVP application
-   A choir practice tool
-   A sports team organiser

The strongest current positioning is:

> **Sahno helps community organisers take an opportunity from enquiry →
> availability → confirmed booking → preparation → completion, while
> keeping every member clear on what they need to do.**

Another way to express the product promise:

> **Sahno tells organisers what needs attention and tells members what
> they need to know.**

Potential differentiation pillars:

1.  **Engagement lifecycle**, not just events.
2.  **Tentative vs confirmed commitments.**
3.  **Outstanding-response management.**
4.  **Event readiness / missing-information awareness.**
5.  **Contextual event workspace and discussion.**
6.  **Responsibilities / who's bringing what.**
7.  **Linked rehearsals and reusable resources.**
8.  **Organiser operational dashboard.**
9.  **Simple organiser-only payment obligations.**
10. **Designed for community/performing groups rather than primarily
    sports teams.**

------------------------------------------------------------------------

## 26. Initial Target Market

Sahno should initially be built and validated using a real performing
group as **Customer Zero**.

Initial suitable segments include:

-   Qawwali/music groups
-   Choirs
-   Bands
-   Dance groups
-   Drama/theatre groups
-   Cultural organisations
-   Community associations
-   Spiritual/religious/community gathering groups
-   Volunteer event groups

The product should be generic underneath while allowing terminology and
workflows to evolve for different organisation types later.

------------------------------------------------------------------------

## 27. Validation Strategy

Rather than attempting a global launch immediately:

1.  Build for the actual initial group workflow.
2.  Continue using existing communication alongside early Sahno
    versions.
3.  Put every real enquiry and booking into Sahno.
4.  Observe where organisers still return to WhatsApp/spreadsheets.
5.  Observe which features members actually use.
6.  Record missing workflows.
7.  Refine after approximately 10--20 real engagements.
8.  Test with a second organisation type before generalising too
    aggressively.

A particularly useful validation question is:

> **Would Sahno have prevented this information/task/payment from being
> forgotten or lost?**

------------------------------------------------------------------------

## 28. Business Model Direction

The likely business model is organisation-led rather than charging every
member.

### Members

Potentially free for core participation:

-   Respond to availability
-   View schedule
-   Receive notifications
-   View event information
-   Discussion
-   Responsibilities
-   Resources

### Organisation / Sahno Pro

Potential paid capabilities:

-   Advanced organiser dashboard
-   Customer/enquiry pipeline
-   Advanced reminders/automation
-   Financial/payment tracking
-   Reports/analytics
-   Travel management
-   More storage
-   Advanced permissions
-   Future integrations

The reasoning is simple: the organiser receives the largest operational
benefit and is therefore the most natural paying customer.

Pricing has **not** been locked.

### Subscription cancellation and organisation data

Cancelling Sahno Pro does not delete the organisation or its operational
history. Paid access continues until the end of the current paid billing
period, after which the organisation moves to Free or a clearly labelled
Restricted state.

Existing Engagements, messages, files, membership records, and financial
history are preserved. Where Free-tier limits are exceeded, Sahno may prevent
new paid-only or over-limit material while keeping existing material readable
and exportable. Resubscribing restores paid functionality without requiring
the organisation to be rebuilt.

Permanent organisation deletion is a separate Owner-only workflow with
re-authentication, explicit confirmation, Admin notification, an export
opportunity, and a recovery period. Exact grace and retention periods remain
open decisions.

------------------------------------------------------------------------

## 29. Brand

### Locked product name

# Sahno

The naming process explored many alternatives, including Duavata,
Ayvero, HyMate, Mately, Sahora, Tivra, Corda, PlanIt and numerous
invented derivatives.

The final decision was to lock **Sahno**.

### Selected v0.1 visual direction

The selected direction for validation uses an abstract human-shaped **S** mark
formed by coordinated orange and teal curves, a deep navy wordmark and primary
app-icon background, and warm off-white neutral surfaces. **Bricolage
Grotesque** is the selected wordmark and display-typography candidate.

This is a selected direction rather than a final production asset. Exact vector
geometry, colour values, body typography, platform exports, accessibility, and
small-size behaviour must be validated before final lock. The detailed source
of truth is `docs/brand/BRAND_VAULT.md` and decision D-074.

### Current tagline

> **Make it happen, together.**

### Brand direction

The preferred logo concept uses an **S-shaped mark formed by two people
/ two complementary shapes coming together**.

The concept communicates:

-   People
-   Collaboration
-   Togetherness
-   Movement
-   Coordination

The current visual direction has explored:

-   Teal
-   Orange
-   Dark/navy text
-   Friendly rounded UI
-   Clean modern typography
-   Rounded cards
-   Simple app-icon treatment

The logo direction is liked but the full formal brand system is not yet
finalised.

------------------------------------------------------------------------

## 30. Design Direction

The first generated visual concepts were intentionally inspirational
rather than final UX.

The next design work should use the actual Sahno workflow rather than
generic event-app dashboards.

Important screens to prototype include:

1.  Sign in / onboarding
2.  Create or join organisation
3.  Member Home / My Schedule
4.  Needs Your Response
5.  Availability response
6.  Organisation Home
7.  Organiser Dashboard
8.  Create Enquiry / Engagement
9.  Availability management
10. Engagement workspace
11. Responsibilities
12. Rehearsal / related activity
13. Discussion
14. Resources/repertoire
15. Basic payment tracking
16. Notifications/activity

The prototype should prove the core journey before visual polish becomes
the priority.

------------------------------------------------------------------------

## 31. Primary Prototype Journey

A key Figma/prototype flow should demonstrate this complete journey:

1.  An organiser receives an enquiry for a Canberra performance.
2.  They create the engagement with only the information currently
    known.
3.  They select the required members.
4.  Sahno sends availability requests.
5.  A member receives a notification.
6.  The member opens **Needs Your Response**.
7.  The member chooses **Available**.
8.  The date appears in their schedule as **Tentative**.
9.  The organiser sees response progress.
10. Outstanding members are reminded.
11. Everyone becomes available.
12. The organiser completes the customer discussion externally.
13. The organiser marks the engagement **Confirmed**.
14. Members receive a confirmation.
15. The tentative schedule item becomes confirmed.
16. Venue and timing are added as they become known.
17. Sound-check is added.
18. Responsibilities are assigned.
19. A rehearsal is linked.
20. Repertoire/resources are attached.
21. Event-specific discussion occurs.
22. On event day, the member sees the essential information immediately.
23. After the event, organiser-only payment obligations can be tracked.
24. Engagement is completed.

If this workflow feels substantially easier than WhatsApp + personal
calendars + memory, the Sahno MVP is solving the intended problem.

------------------------------------------------------------------------

## 32. Product Principles

### 1. Incomplete information is normal

Do not require organisers to know venue/time/etc. when an enquiry is
first created.

### 2. Availability is not confirmation

A member being available and a customer confirming the booking are
separate states.

### 3. Organisers and members need different interfaces

Organisers manage exceptions and missing work. Members need clarity.

### 4. Important information should be structured

Venue, sound-check, responsibilities and status should not depend on
searching chat history.

### 5. Context matters

Messages, files, rehearsals and responsibilities should be attached to
the relevant engagement.

### 6. Sahno should reduce chasing

The system should make outstanding responses and missing information
obvious.

### 7. Do not become accounting software

Track operational payment obligations without rebuilding Xero.

### 8. Build for a real customer first

A real group's actual workflow is more valuable than hypothetical
feature brainstorming.

### 9. Keep the underlying model generic

Solve the performing-group problem first without hard-coding Sahno so
tightly that it cannot support other communities.

### 10. Product before code

The agreed working approach is:

**Product definition → low-fidelity design → validate flows → lock MVP →
architecture → implementation**

Implementation tooling such as Claude Code should be used after the
product decisions are sufficiently clear, rather than asking an AI
coding agent to invent the product while coding it.

------------------------------------------------------------------------

## 33. Current Locked / Strong Decisions

### Locked

-   Product name: **Sahno**
-   Tagline direction: **Make it happen, together.**
-   Build initially around a real community/performing-group workflow.
-   Separate organiser and member experiences.
-   Engagement lifecycle rather than treating everything as a simple
    confirmed event.
-   Availability response states: Available / Maybe / Unavailable.
-   Tentative and confirmed commitments must be distinguishable.
-   Confirmed events have a structured workspace.
-   Sound-check/call time is a first-class event field.
-   Responsibilities / who's bringing what is part of the product.
-   Rehearsals can be linked to engagements.
-   Event-specific discussion is important.
-   Notifications/reminders are core, not optional.
-   Organiser dashboard focuses on what needs attention.
-   Basic organiser-only performer payment tracking belongs in the MVP
    direction.
-   Full accounting is outside MVP.
-   The current preferred logo direction is an S/two-people
    collaboration mark.

### Still to lock

-   Exact role/permission matrix
-   Exact engagement status names
-   Whether "Engagement" is visible terminology or only an
    internal/domain term
-   Exact MVP navigation
-   Exact notification/reminder rules
-   Event readiness scoring vs simple checklist
-   Exact payment fields and visibility
-   Customer/enquiry fields required at creation
-   File/storage limits
-   Pricing/subscription tiers
-   Final colour palette and typography
-   Technical architecture and stack
-   Database/domain model
-   API design
-   Mobile/web delivery strategy
-   App-store/developer account setup
-   Trademark/domain/company registration decisions

------------------------------------------------------------------------

## 34. Immediate Next Steps

### Step 1 --- Roles and permissions

Define exactly what each role can:

-   View
-   Create
-   Edit
-   Confirm
-   Cancel
-   Remind
-   Assign
-   Discuss
-   Access financially
-   Access privately

### Step 2 --- Engagement state machine

Lock the status lifecycle and allowed transitions.

### Step 3 --- Information architecture

Define the Sahno navigation and screen map.

### Step 4 --- Low-fidelity Figma prototype

Prototype the primary organiser/member journey without spending
excessive time on visual polish.

### Step 5 --- Test with real organisers/members

Use the initial group as Customer Zero.

### Step 6 --- Lock MVP specification

Convert findings into acceptance criteria and implementation phases.

### Step 7 --- Technical architecture

Only after the workflow is stable, decide the implementation
architecture and hand it to the coding workflow.

------------------------------------------------------------------------

## 35. North-Star Test

The clearest test for Sahno remains:

> **Can a real group run its bookings through Sahno without relying on
> someone remembering who hasn't replied, whether a booking is
> confirmed, where the venue is, when sound check is, what everyone is
> bringing, what needs rehearsing, or who still needs to be paid?**

If Sahno can reliably answer those questions, it is solving a real
operational problem.

------------------------------------------------------------------------

*This document consolidates the Sahno product discovery discussions to
date. It is intended to evolve as product decisions are validated and
locked.*
