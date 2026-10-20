# Sahno Privacy Policy

> **DRAFT for legal review — not yet published.**
> Written from what the app and API actually do as of 13 October 2026 (see
> `docs/DECISIONS.md`). Everything in **[square brackets]** is a placeholder or a
> fact to confirm. Open items for the reviewer are listed at the end. Do not link
> it from the app until those are resolved.

**Last updated:** [date of publication]

Sahno helps performing groups — bands, choirs, dance and theatre companies, community groups — organise their bookings, rehearsals and people. This policy explains what personal information Sahno holds, why, who can see it, where it is kept, and how long it is kept.

We have written it to meet the Australian Privacy Principles in the *Privacy Act 1988* (Cth).

## 1. Who we are

Sahno is operated by **[legal entity name], ABN [number]**, of **[address]** ("we", "us").

Questions, requests and complaints: **[privacy@…]**.

## 2. Organisations and the people in them

Most of what is in Sahno belongs to an **organisation**: a group created by its **Owner**, who can appoint **Admins** and invite **Members**. Each organisation's information is kept separate from every other organisation's. Being in one organisation never shows you anything from another.

The organisation decides what it records about its bookings and its customers. We hold and process that information on the organisation's behalf, to provide Sahno to it.

## 3. What we collect

**Your account**
- Your email address, and the name you choose to be shown by.
- If you sign in with Apple or Google, an identifier from that provider. We never see your Apple or Google password. If you sign in with an emailed code, we see only the email address.
- Your phone number, if you add one.

**Your place in each organisation**
- Your role (Owner, Admin or Member) and when you joined.
- What you do in the group (for example "singer" or "tabla player"), if you add it.
- Whether you share your contact details with ordinary Members. This is off until you turn it on, and you choose it separately for each organisation.
- Notes that organisers keep about members. These are for organisers only and are never shown to Members, including the member they are about.

**Events and what you do in them**
- Bookings: titles, dates, times, venues, details, readiness checklists, rehearsals, set lists, jobs, and links to resources.
- Whether you are available for an event, your answers to organisers' questions (for example about outfits), and the jobs you are assigned.
- Event chat: your messages, reactions, which messages you have hidden for yourself, and when you have read the chat. These read times are what "seen by" shows.
- A history of changes to each booking, for example "confirmed by …".

**Customers** — the people and businesses who book the group. Organisers can record a customer's name, contact person, phone, email and notes.

**Money**, visible only to the Owner and the Admins the Owner allows: fees, amounts owed and received, and payments to performers.

**Your phone**, if you allow notifications: a push token from Apple or Google, the platform (iOS or Android), and the device name. We use them only to send you Sahno notifications. We also keep the notifications we have sent you, which is what the bell shows.

**Technical logs**: requests to our servers, errors, and timings, kept so we can find and fix problems. By design these logs do not contain sign-in tokens, chat messages, money amounts, organisers' private notes, or members' contact details.

**What we do not collect**: your location, contacts, photos, or microphone. We use no advertising or tracking SDKs, and no analytics that follow you around the app. We do not buy information about you.

## 4. How we use it

- To run the service: show you your organisations, events, chat and calendar, and let organisers manage bookings and people.
- To notify you about things that involve you, in the app, by push notification, and by email for important changes such as confirmations, cancellations, and date or venue changes.
- To keep Sahno secure and working: sign-in, preventing abuse (for example, limiting how quickly invite codes can be guessed), and diagnosing faults.
- To contact you about your account or about changes to Sahno.

We do **not** sell personal information, use it for advertising, or share it with anyone for their own marketing.

## 5. Who can see it

**Inside an organisation**
- Every member can see the organisation's member list: names, roles and what each person does.
- Your phone and email are shown to Owners and Admins. Ordinary Members see them only if you choose to share them.
- An event, its chat and its details are visible to the organisers and to the people on that event.
- Money is visible only to the Owner and the Admins the Owner has allowed.
- Organisers' notes about members are visible only to organisers.

**Our service providers**, who process information only so that we can provide Sahno:

| Provider | What for | Where the data is |
|---|---|---|
| Auth0 (Okta) | Sign-in | Australia |
| Supabase | Database | Sydney, Australia |
| [Hosting provider] | Running the Sahno API | Sydney, Australia |
| Resend | Sending email | United States |
| Expo, Apple and Google | Delivering push notifications | United States and [other locations] |
| Google (Maps Platform) | Suggesting venues as you type — receives what you type in a venue field, and your country | United States and [other locations] |
| Sentry (Functional Software) | Crash and error reports from the app: what failed, on which screen, the device and app version, and an anonymous id — no names, emails or messages | United States |
| [DigitalOcean — when production moves there] | [Hosting, database, files] | [region] |

**Other cases**: we disclose information when the law requires it, or where it is needed to protect someone's safety, or our rights, as the Privacy Act allows.

## 6. Information sent overseas

Email is sent through Resend, and push notifications through Expo, Apple and Google. Each of these processes some information in the United States [and other countries]: for email, your address and the message; for push, your device's push token and the notification's text. We choose providers that commit to protecting the information, and we remain responsible for it under the Australian Privacy Principles.

## 7. If you organise a group

When you record information about other people — members' notes, customers' contact details — you are responsible for having the right to do so, and for telling those people where the law requires it. Please record only what your group needs.

## 8. How long we keep it

- **While your organisation is active**, we keep its information, so the group has its history.
- **When an Owner deletes an organisation**, it disappears for everyone at once, but the Owner can restore it for **30 days**. After 30 days the organisation and everything in it are permanently deleted. That includes its bookings, chat, rehearsals, customers, repertoire, money records, invitations, notifications, and everyone's membership. People's accounts, and their other organisations, are not affected. Members are told when an organisation is deleted.
- **Backups**: our database provider keeps backups for up to **[7] days**. Deleted information can remain in a backup until it expires, and is not used for anything in the meantime.
- **If you leave or are removed from an organisation**, your membership and your profile in that organisation are removed. Messages you sent in its events stay part of that organisation's records.
- **Your account**: to delete your account, [use *Delete account* in the app / email us at [privacy@…]]. We delete your account details within [30] days. Contributions you made to an organisation, such as chat messages, stay with that organisation [shown without your name].
- **Technical logs** are kept for [30] days.

If you are an Owner and want an organisation's records kept, for example for tax purposes, export them before deleting it. [Export is coming; until then, contact us.]

## 9. Keeping it secure

Information travels between the app and our servers over encrypted connections (HTTPS). Sign-in is handled by Auth0, and Sahno never stores passwords. On your phone, sign-in credentials are kept in the phone's secure storage. Access to our systems is limited to the people who run Sahno. No system is perfectly secure. If a data breach is likely to cause you serious harm, we will tell you and the Office of the Australian Information Commissioner, as the law requires.

## 10. Your choices and rights

- **See and correct** your information: you can edit your name and profile in the app. To ask for a copy of what we hold about you, or to correct anything else, email us. We will respond within 30 days.
- **Contact sharing**: you can turn it on or off for each organisation, at any time.
- **Notifications**: you can turn off push notifications in your phone's settings, and signing out stops them on that phone.
- **Leave** an organisation, or ask an organiser to remove you.
- **Complain**: email us first, and we will respond within 30 days. If you are not satisfied, you can complain to the Office of the Australian Information Commissioner at [oaic.gov.au](https://www.oaic.gov.au).

## 11. Children

[Sahno is not intended for children under [16]. Groups with younger members should have a parent or guardian hold the account / Decision needed — see the reviewer notes.]

## 12. Changes to this policy

If we change this policy in a way that matters, we will update the date at the top and tell you in the app before the change takes effect.

## 13. Contact

**[Legal entity name]** · **[privacy@…]** · **[address]**

---

## Reviewer notes (remove before publishing)

**Facts to confirm**
- Operating entity, ABN, address, and privacy contact.
- The Supabase plan's backup retention, and whether point-in-time recovery is on. Section 8 currently says 7 days.
- The hosting provider to name for the API, and the DigitalOcean region once production moves there (D-065). The current test API runs on a server in Sydney.
- Log retention on each host. Section 8 currently says 30 days.
- Locations for Resend and Expo processing.
- Sentry is in the table (D-117), project in the US region; section 6 (overseas) should mention it once switched on. Add a line to section 3 once it is switched on: crash reports with an anonymous id.

**Gaps in the product that this policy depends on**
1. **Account deletion does not exist yet.** Apple's App Store guideline 5.1.1(v) requires apps with sign-in to offer account deletion in the app. Section 8 needs a real path before an App Store release.
2. **Export before deleting an organisation** is promised by D-073, and is not built yet.
3. **D-073 also calls for re-authentication and notifying Admins when an organisation is deleted.** Today it needs the Owner to type the organisation's name, and *all* members are notified by push. Decide whether re-authentication is still wanted.
4. **What happens to someone's chat messages** when they leave or delete their account: kept with their name, kept anonymised, or removed. Section 8 assumes they are kept.
5. **Minimum age, or parental consent**, for groups with young members (dance schools, youth choirs).
6. **The sign-in screen** already says "you agree to our Terms of Service and Privacy Policy" but links to neither. It needs links once both are published, and there are no Terms of Service yet either.
