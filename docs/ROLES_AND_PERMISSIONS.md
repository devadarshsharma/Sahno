# Sahno Roles and Permissions

**Status:** Accepted MVP baseline  
**Last updated:** 23 August 2026  
**Decision references:** D-013 through D-024 in `DECISIONS.md`

## Role model

Sahno initially supports three organisation roles:

1. **Owner** — the single ultimately accountable person.
2. **Admin** — a person trusted to operate the organisation.
3. **Member** — a participant in selected engagements.

There is no separate Organiser or Manager permission role. Owner and Admin users performing operational work may be described as organisers in ordinary product language.

## Permission matrix

| Capability | Owner | Admin | Member |
| --- | --- | --- | --- |
| View all organisation engagements | Yes | Yes | No; selected engagements only |
| Create and manage engagements | Yes | Yes | No |
| Request and review availability | Yes | Yes | Respond for self only |
| Confirm, postpone, cancel, or complete | Yes | Yes | No |
| Assign responsibilities | Yes | Yes | Update own assignments |
| Manage Members | Yes | Yes | No |
| Appoint or remove Admins | Yes | No | No |
| Transfer ownership | Yes | No | No |
| Manage organisation settings | Yes | Yes, except protected ownership/admin controls | No |
| View customer administration details | Yes | Yes | No |
| View and manage finances | Yes | Only when Owner enables it | No |
| View participant-facing notes/resources | Yes | Yes | For selected engagements |
| View Admin-only notes/resources | Yes | Yes | No |
| Participate in engagement discussion | Yes | Yes | For selected engagements |
| Moderate all discussion messages | Yes | Yes | No |

## Ownership safeguards

- Each organisation has exactly one Owner.
- Ownership transfer is explicit and separate from an ordinary role edit.
- The Owner cannot leave or be removed before transferring ownership.
- Admins cannot remove, demote, or modify the authority of the Owner or another Admin.
- Only the Owner can grant or revoke Admin status.

## Financial access

Each Admin has an individual **View and manage finances** permission. It is off by default and only the Owner can change it.

Protected financial information includes quoted and agreed amounts, deposits, balances, performer payment amounts and statuses, and financial summaries. General note or resource audience settings cannot override this protection.

## Member visibility and privacy

- Members see only engagements for which they are selected, invited, or added as participants.
- Members see their own availability response, not other Members' responses.
- Confirmed participants may see the final participant list.
- Members can see names, profile photos, and organisation functions in the Member directory.
- Phone numbers and email addresses are hidden from ordinary Members by default; each person can choose to share their own.
- Owner and Admin access to contact details is limited to legitimate organisation management.
- Customer contact details, internal notes, negotiations, booking history, and financial data are hidden from Members.

## Notes, files, and discussion

Notes, links, and uploaded files have either a **Participants** or **Admins only** audience. Participants is the default.

Selected Members can post in the engagement discussion and edit or delete their own messages. Edits show an edited indicator. Owner and Admins can remove any message for moderation.

## Future review triggers

Do not add more roles or a broad custom-permission system until real usage requires it. Revisit the model if organisations need limited event coordinators, finance-only users, team leaders, or Admins restricted to particular engagements.
