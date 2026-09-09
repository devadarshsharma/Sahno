using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// The engagement lifecycle (ENGAGEMENT_STATE_MACHINE.md). The transitions
/// that are refused matter as much as the ones that work: they are what stops
/// an engagement moving in a way its history could not explain afterwards.
/// </summary>
public sealed class EngagementEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task ADraftNeedsOnlyATitle()
    {
        var org = await NewOrganisationAsync("draft-title");

        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest("Wedding enquiry", null, null, null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(created);
        Assert.Equal("Draft", created.Status);
        Assert.Null(created.StartDate);
        Assert.Null(created.Venue);
        Assert.False(created.IsSharedWithMembers);
        Assert.True(created.CanBeDiscarded);
    }

    [Fact]
    public async Task ABlankTitle_IsRejected()
    {
        var org = await NewOrganisationAsync("draft-blank");

        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest("   ", null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VenueTimeAndDates_MayStayUnknown()
    {
        var org = await NewOrganisationAsync("draft-tbc");
        var engagement = await NewDraftAsync(org, "Corporate enquiry");

        var patch = await org.Owner.PatchAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}",
            new UpdateEngagementRequest("Corporate enquiry (updated)", null, null));

        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);

        var reread = await GetAsync(org, engagement.Id);
        Assert.Equal("Corporate enquiry (updated)", reread.Title);
        Assert.Null(reread.StartTime);
        Assert.Null(reread.Venue);
    }

    /// <summary>
    /// Availability is a question about a date, so there has to be one to ask
    /// about.
    /// </summary>
    [Fact]
    public async Task RequestingAvailabilityWithoutADate_IsRefused()
    {
        var org = await NewOrganisationAsync("no-date");
        var engagement = await NewDraftAsync(org, "Undated enquiry");

        var attempt = await TransitionAsync(org, engagement.Id, "CheckingAvailability");

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        Assert.Equal("Draft", (await GetAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task WithADate_TheDraftCanStartCheckingAvailability()
    {
        var org = await NewOrganisationAsync("with-date");
        var engagement = await NewDatedDraftAsync(org, "Dated enquiry");

        var move = await TransitionAsync(org, engagement.Id, "CheckingAvailability");

        Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);
        var reread = await GetAsync(org, engagement.Id);
        Assert.Equal("CheckingAvailability", reread.Status);
        Assert.True(reread.IsSharedWithMembers);
        Assert.False(reread.CanBeDiscarded);
    }

    /// <summary>A booking that arrives already confirmed skips the middle (D-030).</summary>
    [Fact]
    public async Task ADraftCanGoStraightToConfirmed()
    {
        var org = await NewOrganisationAsync("straight-confirm");
        var engagement = await NewDatedDraftAsync(org, "Already agreed");

        var move = await TransitionAsync(org, engagement.Id, "Confirmed");

        Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);
        Assert.Equal("Confirmed", (await GetAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task TheFullNormalPath_Runs()
    {
        var org = await NewOrganisationAsync("happy-path");
        var engagement = await NewDatedDraftAsync(org, "Full path");

        foreach (var step in new[]
                 {
                     "CheckingAvailability", "Tentative", "Confirmed", "Completed",
                 })
        {
            var move = await TransitionAsync(org, engagement.Id, step);
            Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);
            Assert.Equal(step, (await GetAsync(org, engagement.Id)).Status);
        }
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Postponed")]
    public async Task CancellingAndPostponing_RequireAReason(string target)
    {
        var org = await NewOrganisationAsync($"reason-{target.ToLowerInvariant()}");
        var engagement = await NewSharedAsync(org, "Needs a reason");

        var withoutReason = await TransitionAsync(org, engagement.Id, target);
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);
        Assert.Equal(
            "CheckingAvailability",
            (await GetAsync(org, engagement.Id)).Status);

        var withReason = await TransitionAsync(
            org,
            engagement.Id,
            target,
            "The customer changed their plans.");
        Assert.Equal(HttpStatusCode.NoContent, withReason.StatusCode);
        Assert.Equal(target, (await GetAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task ReopeningACancellation_RequiresAReasonAndPicksTheRealState()
    {
        var org = await NewOrganisationAsync("reopen");
        var engagement = await NewSharedAsync(org, "Came back");
        await TransitionAsync(org, engagement.Id, "Cancelled", "Customer withdrew.");

        var withoutReason = await TransitionAsync(org, engagement.Id, "Confirmed");
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var reopened = await TransitionAsync(
            org,
            engagement.Id,
            "Confirmed",
            "Customer came back and confirmed.");
        Assert.Equal(HttpStatusCode.NoContent, reopened.StatusCode);
        Assert.Equal("Confirmed", (await GetAsync(org, engagement.Id)).Status);
    }

    /// <summary>Completion can be undone, but only deliberately (D-037).</summary>
    [Fact]
    public async Task AMistakenCompletion_CanBeReversedWithAReason()
    {
        var org = await NewOrganisationAsync("uncomplete");
        var engagement = await NewSharedAsync(org, "Closed too soon");
        await TransitionAsync(org, engagement.Id, "Confirmed");
        await TransitionAsync(org, engagement.Id, "Completed");

        var withoutReason = await TransitionAsync(org, engagement.Id, "Confirmed");
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var reversed = await TransitionAsync(
            org,
            engagement.Id,
            "Confirmed",
            "Marked complete by mistake.");
        Assert.Equal(HttpStatusCode.NoContent, reversed.StatusCode);
        Assert.Equal("Confirmed", (await GetAsync(org, engagement.Id)).Status);
    }

    [Theory]
    [InlineData("Draft", "Tentative")]
    [InlineData("Draft", "Completed")]
    [InlineData("Draft", "Cancelled")]
    [InlineData("CheckingAvailability", "Draft")]
    [InlineData("CheckingAvailability", "Completed")]
    [InlineData("Confirmed", "Tentative")]
    [InlineData("Completed", "Cancelled")]
    public async Task MovesTheLifecycleDoesNotAllow_AreRefused(string from, string to)
    {
        var org = await NewOrganisationAsync(
            $"refuse-{from}-{to}".ToLowerInvariant());
        var engagement = await NewDatedDraftAsync(org, $"{from} to {to}");
        await DriveToAsync(org, engagement.Id, from);

        var attempt = await TransitionAsync(org, engagement.Id, to, "Trying anyway.");

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        Assert.Equal(from, (await GetAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task AllowedTransitions_AreReportedForTheCurrentState()
    {
        var org = await NewOrganisationAsync("allowed-list");
        var engagement = await NewDatedDraftAsync(org, "Advertises its moves");

        var draft = await GetAsync(org, engagement.Id);
        Assert.Equal(
            new[] { "CheckingAvailability", "Confirmed" },
            draft.AllowedTransitions.Order().ToArray());

        await TransitionAsync(org, engagement.Id, "Confirmed");

        var confirmed = await GetAsync(org, engagement.Id);
        Assert.Equal(
            new[] { "Cancelled", "Completed", "Postponed" },
            confirmed.AllowedTransitions.Order().ToArray());
    }

    [Fact]
    public async Task ADraftsDateMovesFreely()
    {
        var org = await NewOrganisationAsync("draft-dates");
        var engagement = await NewDraftAsync(org, "Still deciding");

        var set = await SetDatesAsync(org, engagement.Id, new DateOnly(2027, 3, 14));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        Assert.Equal(
            new DateOnly(2027, 3, 14),
            (await GetAsync(org, engagement.Id)).StartDate);

        var again = await SetDatesAsync(org, engagement.Id, new DateOnly(2027, 4, 2));
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);

        // Nothing was shared, so nothing needed recording.
        var history = await ActivityAsync(org, engagement.Id);
        Assert.DoesNotContain(history, entry => entry.Type == "DateChanged");
    }

    /// <summary>
    /// The rule that protects Members: once they hold a date, it cannot be
    /// swapped underneath them (D-038).
    /// </summary>
    [Fact]
    public async Task OnceSharedTheDateMovesOnlyByPostponing()
    {
        var org = await NewOrganisationAsync("shared-dates");
        var engagement = await NewSharedAsync(org, "Members know about this");

        var direct = await SetDatesAsync(org, engagement.Id, new DateOnly(2027, 9, 9));
        Assert.Equal(HttpStatusCode.BadRequest, direct.StatusCode);

        await TransitionAsync(
            org,
            engagement.Id,
            "Postponed",
            "Venue fell through.");

        var rescheduled = await SetDatesAsync(
            org,
            engagement.Id,
            new DateOnly(2027, 9, 9));
        Assert.Equal(HttpStatusCode.NoContent, rescheduled.StatusCode);

        var reread = await GetAsync(org, engagement.Id);
        Assert.Equal(new DateOnly(2027, 9, 9), reread.StartDate);
        Assert.Equal("Postponed", reread.Status);
    }

    [Fact]
    public async Task PostponingKeepsTheOriginalDateInHistory()
    {
        var org = await NewOrganisationAsync("postpone-history");
        var engagement = await NewSharedAsync(org, "Was going to be in May");
        var originalDate = (await GetAsync(org, engagement.Id)).StartDate;

        await TransitionAsync(org, engagement.Id, "Postponed", "Customer illness.");
        await SetDatesAsync(org, engagement.Id, new DateOnly(2027, 12, 1));

        var history = await ActivityAsync(org, engagement.Id);
        var postponement = history.Single(entry => entry.ToStatus == "Postponed");

        Assert.Equal(originalDate, postponement.FromStartDate);
        Assert.Equal("Customer illness.", postponement.Reason);

        // The replacement date is its own entry, so both survive.
        var reschedule = history.Single(entry => entry.Type == "DateChanged");
        Assert.Equal(originalDate, reschedule.FromStartDate);
        Assert.Equal(new DateOnly(2027, 12, 1), reschedule.ToStartDate);
    }

    [Fact]
    public async Task EveryStatusChange_IsKeptWithItsReason()
    {
        var org = await NewOrganisationAsync("history");
        var engagement = await NewSharedAsync(org, "Long story");
        await TransitionAsync(org, engagement.Id, "Cancelled", "Double booked.");
        await TransitionAsync(org, engagement.Id, "Confirmed", "Freed up again.");

        var history = await ActivityAsync(org, engagement.Id);

        Assert.Contains(history, entry => entry.Type == "Created");
        Assert.Contains(
            history,
            entry => entry.ToStatus == "Cancelled" && entry.Reason == "Double booked.");
        // Reopening does not erase the cancellation that came before it.
        Assert.Contains(
            history,
            entry => entry.ToStatus == "Confirmed" && entry.Reason == "Freed up again.");
    }

    [Fact]
    public async Task ADraftIsDiscarded_ButAnythingSharedIsNot()
    {
        var org = await NewOrganisationAsync("discard");
        var draft = await NewDatedDraftAsync(org, "Never went anywhere");

        var discarded = await org.Owner.DeleteAsync($"{Engagements(org)}/{draft.Id}");
        Assert.Equal(HttpStatusCode.NoContent, discarded.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await org.Owner.GetAsync($"{Engagements(org)}/{draft.Id}")).StatusCode);

        var shared = await NewSharedAsync(org, "Members were told");
        var refused = await org.Owner.DeleteAsync($"{Engagements(org)}/{shared.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    /// <summary>
    /// A Member's list is scoped to the engagements they are on, so one they
    /// were never selected for is simply absent (D-020). Creating and moving
    /// engagements stays organisers' work (D-019).
    /// </summary>
    [Fact]
    public async Task MembersSeeNoEngagementsTheyAreNotOn_AndCannotCreateOrMoveAny()
    {
        var org = await NewOrganisationAsync("member-blocked", withMember: true);
        var engagement = await NewDatedDraftAsync(org, "Organisers only");

        var visible = await org.Member!.GetFromJsonAsync<List<EngagementResponse>>(
            Engagements(org));
        Assert.NotNull(visible);
        Assert.Empty(visible);

        var peek = await org.Member!.GetAsync($"{Engagements(org)}/{engagement.Id}");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);

        var create = await org.Member!.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest("Sneaky", null, null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var move = await org.Member!.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/transition",
            new TransitionEngagementRequest("Confirmed", null));
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
    }

    [Fact]
    public async Task EngagementsOfAnotherOrganisation_AreOutOfReach()
    {
        var mine = await NewOrganisationAsync("reach-mine");
        var theirs = await NewOrganisationAsync("reach-theirs");
        var hidden = await NewDatedDraftAsync(theirs, "Not yours");

        var attempt = await mine.Owner.GetAsync($"{Engagements(mine)}/{hidden.Id}");

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
    }

    private sealed record TestOrganisation(
        Guid Id,
        string Name,
        HttpClient Owner,
        HttpClient? Member);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private async Task<TestOrganisation> NewOrganisationAsync(
        string prefix,
        bool withMember = false)
    {
        var owner = CreateClient($"auth0|{prefix}-owner");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

        HttpClient? member = null;
        if (withMember)
        {
            var inviteResponse = await owner.PostAsJsonAsync(
                $"/api/organisations/{organisation.Id}/invitations",
                new CreateInvitationRequest(null));
            inviteResponse.EnsureSuccessStatusCode();
            var invitation =
                await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

            member = CreateClient($"auth0|{prefix}-member");
            var accept = await member.PostAsync(
                $"/api/invitations/{invitation.Token}/accept",
                content: null);
            accept.EnsureSuccessStatusCode();
        }

        return new TestOrganisation(organisation.Id, organisation.Name, owner, member);
    }

    private async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(title, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    private async Task<EngagementResponse> NewDatedDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(
                title,
                new DateOnly(2027, 5, 20),
                null,
                null,
                null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    /// <summary>A dated engagement Members have been told about.</summary>
    private async Task<EngagementResponse> NewSharedAsync(
        TestOrganisation org,
        string title)
    {
        var engagement = await NewDatedDraftAsync(org, title);
        var move = await TransitionAsync(org, engagement.Id, "CheckingAvailability");
        move.EnsureSuccessStatusCode();
        return engagement;
    }

    /// <summary>Walks an engagement to a state, for tests that start there.</summary>
    private async Task DriveToAsync(TestOrganisation org, Guid id, string target)
    {
        var route = target switch
        {
            "Draft" => Array.Empty<string>(),
            "CheckingAvailability" => ["CheckingAvailability"],
            "Tentative" => ["CheckingAvailability", "Tentative"],
            "Confirmed" => ["CheckingAvailability", "Tentative", "Confirmed"],
            "Completed" =>
                ["CheckingAvailability", "Tentative", "Confirmed", "Completed"],
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null),
        };

        foreach (var step in route)
        {
            var move = await TransitionAsync(org, id, step);
            move.EnsureSuccessStatusCode();
        }
    }

    private Task<HttpResponseMessage> TransitionAsync(
        TestOrganisation org,
        Guid id,
        string status,
        string? reason = null)
    {
        return org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{id}/transition",
            new TransitionEngagementRequest(status, reason));
    }

    private Task<HttpResponseMessage> SetDatesAsync(
        TestOrganisation org,
        Guid id,
        DateOnly? startDate,
        DateOnly? endDate = null)
    {
        return org.Owner.PutAsJsonAsync(
            $"{Engagements(org)}/{id}/dates",
            new SetEngagementDatesRequest(startDate, endDate));
    }

    private static async Task<EngagementResponse> GetAsync(
        TestOrganisation org,
        Guid id)
    {
        var engagement = await org.Owner.GetFromJsonAsync<EngagementResponse>(
            $"{Engagements(org)}/{id}");
        Assert.NotNull(engagement);
        return engagement;
    }

    private static async Task<List<EngagementActivityResponse>> ActivityAsync(
        TestOrganisation org,
        Guid id)
    {
        var history = await org.Owner
            .GetFromJsonAsync<List<EngagementActivityResponse>>(
                $"{Engagements(org)}/{id}/activity");
        Assert.NotNull(history);
        return history;
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
