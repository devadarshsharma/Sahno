using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Application.Notifications;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Notifications;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Rehearsal notifications and scheduled reminders (D-085). The scheduler runs
/// on the test clock, in the organisation's own time zone, and each test uses
/// its own day so they never send each other's reminders.
/// </summary>
public sealed class ReminderTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    private const string Zone = "Australia/Sydney";

    [Fact]
    public async Task ARehearsalIsAnnouncedThenRemindedOnTheMorning()
    {
        var org = await NewOrganisationAsync("rehearse", members: 2);
        var engagement = await NewDraftAsync(org, "Saturday gig", LocalToday().AddDays(5), startTime: null);
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org, org.Members[1], engagement.Id, "Unavailable");

        var day = LocalToday().AddDays(1);
        var booked = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/rehearsals",
            new SaveRehearsalRequest(null, day, new TimeOnly(19, 0), new TimeOnly(21, 0), "Community Hall", null));
        booked.EnsureSuccessStatusCode();

        // Booking tells the expected lineup — not the person who declined,
        // not the organiser who booked it.
        Assert.Contains(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalScheduled");
        Assert.DoesNotContain(await BellAsync(org.Members[1], org), row => row.Kind == "RehearsalScheduled");
        Assert.DoesNotContain(await BellAsync(org.Owner, org), row => row.Kind == "RehearsalScheduled");

        // The upcoming list follows the same rule.
        Assert.Single(await UpcomingAsync(org.Members[0], org));
        Assert.Empty(await UpcomingAsync(org.Members[1], org));

        // Not before 8:00 on the day; at 8:05 it goes, once.
        await SendDueAtAsync(day, new TimeOnly(7, 55));
        Assert.DoesNotContain(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalReminder");

        await SendDueAtAsync(day, new TimeOnly(8, 5));
        var reminder = Assert.Single(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalReminder");
        Assert.Equal($"/engagement/{engagement.Id}/files", reminder.Route);
        Assert.Contains("Community Hall", reminder.Body);
        Assert.DoesNotContain(await BellAsync(org.Members[1], org), row => row.Kind == "RehearsalReminder");
        // The organiser running it is reminded too, though not on the lineup (D-115).
        Assert.Single(await BellAsync(org.Owner, org), row => row.Kind == "RehearsalReminder");

        Assert.Equal(0, await SendDueAtAsync(day, new TimeOnly(8, 10)));
    }

    [Fact]
    public async Task TheEventDayReminderCarriesEachPersonsOpenJobs()
    {
        var org = await NewOrganisationAsync("dayof", members: 3);
        var day = LocalToday().AddDays(2);
        var engagement = await NewDraftAsync(org, "Wedding", day, new TimeOnly(19, 0));
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org, org.Members[0], engagement.Id, "Available");
        await RespondAsync(org, org.Members[1], engagement.Id, "Available");
        await RespondAsync(org, org.Members[2], engagement.Id, "Unavailable");

        var job = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/responsibilities",
            new CreateResponsibilityRequest("Bring the PA", null, org.MemberIds[0]));
        job.EnsureSuccessStatusCode();

        foreach (var status in new[] { "Tentative", "Confirmed" })
        {
            var move = await org.Owner.PostAsJsonAsync(
                $"{Engagements(org)}/{engagement.Id}/transition",
                new TransitionEngagementRequest(status, null));
            move.EnsureSuccessStatusCode();
        }

        await SendDueAtAsync(day, new TimeOnly(8, 5));

        var withJob = Assert.Single(await BellAsync(org.Members[0], org), row => row.Kind == "ResponsibilityReminder");
        Assert.Contains("Bring the PA", withJob.Body);
        Assert.Equal($"/engagement/{engagement.Id}/jobs", withJob.Route);

        var plain = Assert.Single(await BellAsync(org.Members[1], org), row => row.Kind == "EngagementDayReminder");
        Assert.Equal($"/engagement/{engagement.Id}", plain.Route);

        Assert.DoesNotContain(await BellAsync(org.Members[2], org), row =>
            row.Kind is "EngagementDayReminder" or "ResponsibilityReminder");

        // The organiser, not on the lineup, gets the day reminder as well (D-115).
        Assert.Single(await BellAsync(org.Owner, org), row => row.Kind == "EngagementDayReminder");

        Assert.Equal(0, await SendDueAtAsync(day, new TimeOnly(8, 10)));
    }

    /// <summary>
    /// Marking an event done takes its rehearsals off Home's "coming up" list
    /// but not off the calendar (D-090): the calendar is also a record of what
    /// happened. A cancelled event's rehearsals go from both.
    /// </summary>
    [Fact]
    public async Task ADoneEventKeepsItsRehearsalsOnTheCalendar()
    {
        var org = await NewOrganisationAsync("calendar-done", members: 1);
        var done = await NewDraftAsync(org, "Done gig", LocalToday().AddDays(5), startTime: null);
        var cancelled = await NewDraftAsync(org, "Called off", LocalToday().AddDays(6), startTime: null);
        foreach (var engagement in new[] { done, cancelled })
        {
            await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
            await RespondAsync(org, org.Members[0], engagement.Id, "Available");
            (await org.Owner.PostAsJsonAsync(
                $"{Engagements(org)}/{engagement.Id}/rehearsals",
                new SaveRehearsalRequest(null, LocalToday().AddDays(1), new TimeOnly(19, 0), null, "Hall", null))).EnsureSuccessStatusCode();
        }

        foreach (var status in new[] { "Tentative", "Confirmed", "Completed" })
        {
            (await org.Owner.PostAsJsonAsync(
                $"{Engagements(org)}/{done.Id}/transition",
                new TransitionEngagementRequest(status, null))).EnsureSuccessStatusCode();
        }
        (await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{cancelled.Id}/transition",
            new TransitionEngagementRequest("Cancelled", "Venue fell through"))).EnsureSuccessStatusCode();

        Assert.Empty(await UpcomingAsync(org.Owner, org));

        var from = LocalToday().AddMonths(-12).ToString("yyyy-MM-dd");
        foreach (var client in new[] { org.Owner, org.Members[0] })
        {
            var calendar = await client.GetFromJsonAsync<MyCalendarResponse>($"/api/me/calendar?from={from}");
            Assert.Equal(done.Id, Assert.Single(calendar!.Rehearsals).EngagementId);
            Assert.Contains(calendar.Events, row => row.Id == done.Id && row.Status == "Completed");
            Assert.DoesNotContain(calendar.Events, row => row.Id == cancelled.Id);
        }
    }

    // ---- Helpers -----------------------------------------------------------

    private static DateOnly LocalToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(Zone)).DateTime);

    /// <summary>
    /// "Two hours before" (D-118): a 19:00 rehearsal is reminded about at
    /// 17:00, not at 8:00 — and once, "Today".
    /// </summary>
    [Fact]
    public async Task ARehearsalCanBeRemindedHoursBefore()
    {
        var org = await NewOrganisationAsync("remind-2h", members: 1);
        await SetRemindersAsync(org.Owner, org, "morning", "2h");
        var engagement = await NewDraftAsync(org, "Gig", LocalToday().AddDays(5), startTime: null);
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var day = LocalToday().AddDays(1);
        (await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/rehearsals",
            new SaveRehearsalRequest(null, day, new TimeOnly(19, 0), null, "Hall", null))).EnsureSuccessStatusCode();

        await SendDueAtAsync(day, new TimeOnly(8, 5));
        Assert.DoesNotContain(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalReminder");

        await SendDueAtAsync(day, new TimeOnly(17, 5));
        var reminder = Assert.Single(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalReminder");
        Assert.StartsWith("Today:", reminder.Title);
        Assert.Equal(0, await SendDueAtAsync(day, new TimeOnly(18, 0)));
    }

    /// <summary>
    /// "The day before" (D-118): an event is reminded about at 18:00 the
    /// evening before, as "Tomorrow", and not again on the day.
    /// </summary>
    [Fact]
    public async Task AnEventCanBeRemindedTheDayBefore_AndOnlyOnce()
    {
        var org = await NewOrganisationAsync("remind-eve", members: 1);
        await SetRemindersAsync(org.Owner, org, "day-before", "morning");
        var day = LocalToday().AddDays(2);
        var engagement = await NewDraftAsync(org, "Wedding", day, new TimeOnly(19, 0));
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org, org.Members[0], engagement.Id, "Available");
        foreach (var status in new[] { "Tentative", "Confirmed" })
        {
            (await org.Owner.PostAsJsonAsync(
                $"{Engagements(org)}/{engagement.Id}/transition",
                new TransitionEngagementRequest(status, null))).EnsureSuccessStatusCode();
        }

        await SendDueAtAsync(day.AddDays(-1), new TimeOnly(17, 55));
        Assert.DoesNotContain(await BellAsync(org.Members[0], org), row => row.Kind == "EngagementDayReminder");

        await SendDueAtAsync(day.AddDays(-1), new TimeOnly(18, 5));
        var reminder = Assert.Single(await BellAsync(org.Members[0], org), row => row.Kind == "EngagementDayReminder");
        Assert.StartsWith("Tomorrow:", reminder.Title);

        Assert.Equal(0, await SendDueAtAsync(day, new TimeOnly(8, 5)));
    }

    [Fact]
    public async Task RemindersCanBeSwitchedOff_ByOrganisersOnly()
    {
        var org = await NewOrganisationAsync("remind-off", members: 1);

        var byMember = await org.Members[0].PutAsJsonAsync(
            $"/api/organisations/{org.Id}/reminders", new SetRemindersRequest("off", "off"));
        Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);
        var nonsense = await org.Owner.PutAsJsonAsync(
            $"/api/organisations/{org.Id}/reminders", new SetRemindersRequest("whenever", "off"));
        Assert.Equal(HttpStatusCode.BadRequest, nonsense.StatusCode);

        await SetRemindersAsync(org.Owner, org, "morning", "off");
        var settings = await org.Owner.GetFromJsonAsync<OrganisationResponse>($"/api/organisations/{org.Id}");
        Assert.Equal(("morning", "off"), (settings!.EventReminder, settings.RehearsalReminder));

        var engagement = await NewDraftAsync(org, "Quiet", LocalToday().AddDays(5), startTime: null);
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var day = LocalToday().AddDays(1);
        (await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/rehearsals",
            new SaveRehearsalRequest(null, day, new TimeOnly(19, 0), null, null, null))).EnsureSuccessStatusCode();

        await SendDueAtAsync(day, new TimeOnly(8, 5));
        Assert.DoesNotContain(await BellAsync(org.Members[0], org), row => row.Kind == "RehearsalReminder");
    }

    /// <summary>
    /// Unanswered availability is nudged on its own (D-120): two days after
    /// asking, then a last call the day before — never somebody who answered,
    /// and not at all when the organisation turns it off.
    /// </summary>
    [Fact]
    public async Task UnansweredAvailabilityIsNudged_ThenGivenALastCall()
    {
        var org = await NewOrganisationAsync("nudge", members: 2);
        var eventDay = LocalToday().AddDays(6);
        var engagement = await NewDraftAsync(org, "Mehndi", eventDay, new TimeOnly(19, 0));
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org, org.Members[1], engagement.Id, "Available");

        static int Nudges(List<NotificationResponse> bell) => bell.Count(row => row.Kind == "AvailabilityReminder");

        await SendDueAtAsync(LocalToday().AddDays(1), new TimeOnly(12, 0));
        Assert.Equal(0, Nudges(await BellAsync(org.Members[0], org)));

        // Overdue at night still waits for the morning.
        await SendDueAtAsync(LocalToday().AddDays(3), new TimeOnly(23, 0));
        Assert.Equal(0, Nudges(await BellAsync(org.Members[0], org)));

        await SendDueAtAsync(LocalToday().AddDays(3), new TimeOnly(12, 0));
        Assert.Equal(1, Nudges(await BellAsync(org.Members[0], org)));
        await SendDueAtAsync(LocalToday().AddDays(4), new TimeOnly(12, 0));
        Assert.Equal(1, Nudges(await BellAsync(org.Members[0], org)));

        await SendDueAtAsync(eventDay.AddDays(-1), new TimeOnly(10, 5));
        var lastCall = (await BellAsync(org.Members[0], org)).Where(row => row.Kind == "AvailabilityReminder").ToList();
        Assert.Equal(2, lastCall.Count);
        Assert.Contains(lastCall, row => row.Title.StartsWith("Tomorrow:"));

        // The one who answered is never chased.
        Assert.Equal(0, Nudges(await BellAsync(org.Members[1], org)));
    }

    [Fact]
    public async Task NudgesCanBeTurnedOff()
    {
        var org = await NewOrganisationAsync("nudge-off", members: 1);
        var response = await org.Owner.PutAsJsonAsync(
            $"/api/organisations/{org.Id}/reminders", new SetRemindersRequest("morning", "morning", "off"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var settings = await org.Owner.GetFromJsonAsync<OrganisationResponse>($"/api/organisations/{org.Id}");
        Assert.Equal("off", settings!.AvailabilityNudge);

        var eventDay = LocalToday().AddDays(6);
        var engagement = await NewDraftAsync(org, "Quiet", eventDay, new TimeOnly(19, 0));
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        await SendDueAtAsync(LocalToday().AddDays(3), new TimeOnly(12, 0));
        await SendDueAtAsync(eventDay.AddDays(-1), new TimeOnly(10, 5));
        Assert.DoesNotContain(await BellAsync(org.Members[0], org), row => row.Kind == "AvailabilityReminder");
    }

    private static async Task SetRemindersAsync(HttpClient owner, TestOrganisation org, string events, string rehearsals)
    {
        var response = await owner.PutAsJsonAsync(
            $"/api/organisations/{org.Id}/reminders", new SetRemindersRequest(events, rehearsals));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>Runs one scheduler pass as if it were this local time in Sydney.</summary>
    private async Task<int> SendDueAtAsync(DateOnly day, TimeOnly time)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(Zone);
        var local = day.ToDateTime(time);
        var clock = factory.Services.GetRequiredService<TestClock>();
        clock.Now = new DateTimeOffset(local, zone.GetUtcOffset(local));
        try
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ReminderService>()
                .SendDueAsync(CancellationToken.None);
        }
        finally
        {
            clock.Now = null;
        }
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static async Task<List<NotificationResponse>> BellAsync(HttpClient client, TestOrganisation org) =>
        (await client.GetFromJsonAsync<List<NotificationResponse>>(
            $"/api/organisations/{org.Id}/notifications"))!;

    private static async Task<List<UpcomingRehearsalResponse>> UpcomingAsync(HttpClient client, TestOrganisation org) =>
        (await client.GetFromJsonAsync<List<UpcomingRehearsalResponse>>(
            $"/api/organisations/{org.Id}/rehearsals/upcoming"))!;

    private static async Task RequestAvailabilityAsync(TestOrganisation org, Guid engagementId, IReadOnlyList<Guid> userIds)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/availability/requests",
            new RequestAvailabilityRequest(userIds));
        response.EnsureSuccessStatusCode();
    }

    private static async Task RespondAsync(TestOrganisation org, HttpClient member, Guid engagementId, string answer)
    {
        var response = await member.PutAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/availability/me",
            new RespondAvailabilityRequest(answer));
        response.EnsureSuccessStatusCode();
    }

    private async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title,
        DateOnly? startDate,
        TimeOnly? startTime)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(title, startDate, null, startTime, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EngagementResponse>())!;
    }

    private async Task<TestOrganisation> NewOrganisationAsync(string prefix, int members)
    {
        var owner = CreateClient($"auth0|{prefix}-owner", $"{prefix}-owner@example.test");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, Zone));
        created.EnsureSuccessStatusCode();
        var organisation = (await created.Content.ReadFromJsonAsync<OrganisationResponse>())!;

        var invite = await owner.PostAsJsonAsync(
            $"/api/organisations/{organisation.Id}/invitations",
            new CreateInvitationRequest(null));
        invite.EnsureSuccessStatusCode();
        var invitation = (await invite.Content.ReadFromJsonAsync<InvitationResponse>())!;

        var clients = new List<HttpClient>();
        for (var index = 0; index < members; index++)
        {
            var member = CreateClient($"auth0|{prefix}-member-{index}", $"{prefix}-member-{index}@example.test");
            (await member.PostAsync($"/api/invitations/{invitation.Token}/accept", content: null))
                .EnsureSuccessStatusCode();
            clients.Add(member);
        }

        var directory = (await owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{organisation.Id}/members"))!;

        // The directory lists the owner first, then members in join order.
        return new TestOrganisation(
            organisation.Id,
            owner,
            clients,
            directory.Skip(1).Select(row => row.UserId).ToList());
    }

    private HttpClient CreateClient(string subject, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailHeader, email);
        return client;
    }
}
