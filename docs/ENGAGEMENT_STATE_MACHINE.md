# Sahno Engagement State Machine

**Status:** Accepted MVP baseline  
**Last updated:** 23 August 2026  
**Decision references:** D-025 onward in `DECISIONS.md`

## Accepted states and transitions

The MVP lifecycle states are:

`Draft → Checking Availability → Tentative → Confirmed → Completed`

The normal path can skip intermediate states when reality requires it. **Cancelled** and **Postponed** are alternative states, with controlled reopening and resumption transitions.

### Draft

The engagement has been created but has not been shared with Members.

- Visible only to the Owner and Admins.
- May contain incomplete or TBC information.
- Does not notify Members when saved.
- The Owner or an Admin can edit it.

### Checking Availability

The organisation is collecting availability from selected Members.

- Entered when the first availability request is sent.
- Selected Members receive the request and can access participant-facing information.
- The Owner and Admins can see individual responses and response totals.
- An Owner or Admin can add replacement Members without resetting existing responses.
- Removed Members lose access; their previous responses remain only in internal history.

### Tentative

The organisation is provisionally able or willing to take the engagement, but the booking has not yet been formally confirmed.

- Sahno may indicate that all required responses have arrived, but it does not enter Tentative automatically.
- The Owner or an Admin decides whether the available lineup is sufficient.
- The engagement remains visibly tentative in participating Members' schedules.
- Adding or replacing a participant does not move the engagement backwards; outstanding responses are shown as a warning.

### Confirmed

The booking has been formally confirmed, even if preparation or participant availability is not yet complete.

- Confirmation is an explicit Owner/Admin action.
- Outstanding availability does not block confirmation.
- Sahno warns and requires acknowledgement when confirming with unresolved responses.
- Any unresolved responses remain visible as a readiness issue after confirmation.
- Preparation is represented by readiness items, not by a separate lifecycle status.

### Completed

The event has occurred and an Owner or Admin has explicitly closed the engagement.

- Completion is manual, not date-driven.
- Sahno may prompt for completion after the scheduled event.
- Outstanding payments or follow-up do not block completion, but trigger a warning and remain active after completion.

### Cancelled

The opportunity or booking will not proceed.

- Available from Checking Availability, Tentative, or Confirmed.
- Requires a short cancellation reason.
- Notifies all selected Members who had access.
- Remains in engagement history.
- A private Draft may instead be discarded.

### Postponed

The engagement is delayed and may initially have no replacement date.

- Available from Checking Availability, Tentative, or Confirmed.
- Preserves the original date, engagement identity, and history.
- Immediately notifies selected Members.
- A replacement date may be TBC.
- Entering a new date marks prior availability responses outdated and prompts a new request.

### Accepted transition

- `Draft → Checking Availability` when the first availability request is sent.
- `Checking Availability → Tentative` when an Owner or Admin accepts the available lineup.
- `Tentative → Confirmed` when an Owner or Admin records formal booking confirmation.
- `Draft → Confirmed` for a booking that arrives already confirmed.
- `Checking Availability → Confirmed` when the customer confirms before the availability workflow is formally closed.
- `Confirmed → Completed` when an Owner or Admin explicitly completes the engagement.
- `Checking Availability → Cancelled`, `Tentative → Cancelled`, or `Confirmed → Cancelled` when an Owner or Admin records cancellation.
- `Checking Availability → Postponed`, `Tentative → Postponed`, or `Confirmed → Postponed` when an Owner or Admin records postponement.
- `Postponed → Checking Availability`, `Postponed → Tentative`, or `Postponed → Confirmed` when an Owner or Admin resumes it at the appropriate real-world state.
- `Cancelled → Checking Availability`, `Tentative`, or `Confirmed` when an Owner/Admin reopens it with a reason and Member notification.
- `Completed → Confirmed` when an Owner/Admin reverses a mistaken completion with a reason.

## Date changes

- A Draft date may be edited freely.
- Once Members have been notified, a significant date change uses the Postponed/rescheduling flow.
- The original date is preserved, Members are notified, and prior availability is marked outdated.

## Implementation requirements still to detail

- Exact labels and confirmation copy for each transition.
- Notification delivery channels and timing.
- Activity-history schema and retention.
- Readiness rules and dashboard presentation.
- Editing participants or dates after requests are sent.
- Status history and audit behaviour.
