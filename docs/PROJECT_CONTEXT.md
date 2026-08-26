# Sahno Project Context

**Purpose:** Concise memory for humans and AI coding agents  
**Last updated:** 21 August 2026

## Read this first

The canonical product specification is `../Sahno_Product_Discovery_and_MVP.md`. Read it before making product, domain-model, UX, or architecture decisions. Commercial direction is in `COMMERCIAL_STRATEGY.md`; confirmed and provisional decisions are tracked in `DECISIONS.md`; the accepted MVP permission model is in `ROLES_AND_PERMISSIONS.md`; local setup and runtime checks are documented in `LOCAL_DEVELOPMENT.md`.

If documents conflict, use this precedence:

1. Newer **Accepted** entries in `DECISIONS.md`.
2. The canonical product specification.
3. The commercial strategy.
4. This context file.

Do not silently convert open questions or hypotheses into requirements. Record material decisions in the decision log and update the affected source-of-truth document.

## Product in one paragraph

Sahno is an operational coordination product for community and performing groups. It takes an opportunity through enquiry, availability, tentative commitment, confirmation, preparation, completion, cancellation, or postponement. Organisers see what needs attention; members see what they need to know and do. The first real-world pilot is Australian Qawwal Party (AQP), while the underlying model should remain useful to other group types.

## Core product truths

- Incomplete enquiry and event information is normal; TBC fields must be supported.
- A member saying they are available does **not** confirm the booking.
- Tentative and confirmed commitments must remain visibly distinct.
- Organiser and member experiences serve different jobs.
- Critical details belong in structured fields, not only chat messages.
- Discussion, resources, rehearsals, and responsibilities belong to their engagement context.
- The system should expose non-responders, missing details, and other items needing attention.
- Notifications and reminders are core behaviour.
- Basic organiser-only payment tracking is useful; full accounting is out of scope.
- Build and validate the real workflow before broadening the product.

## Primary workflow

Enquiry → availability request → member responses → tentative/ready to confirm → confirmed → preparation → completed.

Alternative outcomes include cancelled and postponed. Exact state names and transition rules still need to be formally locked.

## Primary users

- **Owner/Admin:** organisation control, membership, roles, settings, and full organiser capability.
- **Organiser/Manager:** manages enquiries, availability, engagements, preparation, reminders, and authorised commercial information.
- **Member:** responds to availability and sees relevant schedules, event details, responsibilities, resources, and discussion.

Commercial, customer, financial, and sensitive profile data must not be visible to ordinary members by default.

## MVP focus

- Organisations, membership, and roles.
- Enquiries/engagements with incomplete information.
- Availability responses and reminders.
- Needs-response, tentative, and confirmed schedules.
- Confirmed engagement workspace.
- Responsibilities, linked rehearsals, resources, and event discussion.
- Notifications and organiser attention dashboard.
- Basic customer information and lightweight organiser-only payment tracking.

Full CRM, accounting, invoicing, payment processing, public ticketing, contracts, advanced travel, passport storage, advanced analytics, and an AI assistant are not MVP.

## Commercial model

Working principle: members participate for free; organisations pay for advanced organiser value. The current hypothesis is Free plus Sahno Pro at approximately A$15–25 per organisation per month. Packaging and pricing are not locked.

## Working discipline for agents

- Preserve the existing product specification unless explicitly asked to revise it.
- Prefer domain terms that remain generic across group types; do not hard-code AQP or music-specific assumptions into the core model.
- Keep financial and private data behind explicit authorisation boundaries.
- When a request requires an unresolved product choice, surface it and record the resolution.
- Keep implementation aligned with the documented MVP; do not add attractive but out-of-scope systems.
- Update documentation in the same change as any material product or architecture decision.

## Immediate product work

The essential MVP roles-and-permissions baseline is accepted in `ROLES_AND_PERMISSIONS.md` and decisions D-013 through D-024. The MVP engagement lifecycle is accepted in `ENGAGEMENT_STATE_MACHINE.md` and decisions D-025 through D-038. The information-architecture baseline is accepted in `INFORMATION_ARCHITECTURE.md` and decisions D-039 through D-052. The next task is low-fidelity wireframing of the primary organiser/member journey, followed by user validation, final MVP acceptance criteria, and technical architecture.

Delivery direction D-053 is accepted: Sahno is a mobile-first React Native application targeting iOS and Android. Do not assume a responsive-web-first product or defer core Owner/Admin workflows to desktop.

The primary seven-screen mobile journey is accepted as a low-fidelity direction under D-054. Treat its information flow as the starting point, while keeping visual design and detailed interaction patterns open to iteration.

Authentication and onboarding are accepted under D-055 through D-058 and consolidated in `AUTHENTICATION_AND_ONBOARDING.md`. The first implementation slices and acceptance criteria are maintained in `MVP_BUILD_BACKLOG.md`.

Technical architecture is being defined in `TECHNICAL_ARCHITECTURE.md`. D-059 accepts Expo, Expo Router, TypeScript, Expo development builds, and EAS Build for the React Native mobile foundation.

The current GitHub Actions workflow and its planned evolution are explained in `CI_CD_GUIDE.md`. Treat CI/CD as a guided learning workflow under D-072: introduce checks and deployments incrementally, explain them, observe real runs, and document operation and troubleshooting.
