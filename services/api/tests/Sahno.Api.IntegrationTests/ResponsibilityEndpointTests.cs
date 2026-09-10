using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Who is doing or bringing what (Slice 8, D-047 §3).
///
/// The rules under test are all about authority. Organisers decide what a job
/// is and whose it is; the person holding it decides whether it is done. These
/// tests exist mostly to prove neither side can do the other's part.
/// </summary>
public sealed class ResponsibilityEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AnOrganiserWritesDownAJobBeforeAnybodyHasTakenIt()
    {
        var org = await NewOrganisationAsync("resp-unassigned");
        var engagement = await NewDraftAsync(org, "Sound gear");

        var created = await CreateAsync(org, engagement.Id, "Bring the harmonium");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var jobs = await ListAsync(org.Owner, org, engagement.Id);
        var job = Assert.Single(jobs);
        Assert.Equal("Bring the harmonium", job.Title);
        Assert.Null(job.AssignedUserId);
        Assert.False(job.IsDone);
    }

    /// <summary>
    /// A job handed to someone who was never selected would sit on a screen
    /// they cannot open, so it is refused rather than quietly lost.
    /// </summary>
    [Fact]
    public async Task AJobCannotGoToSomebodyWhoIsNotOnTheEvent()
    {
        var org = await NewOrganisationAsync("resp-offlineup", members: 2);
        var engagement = await NewDraftAsync(org, "Selective");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);

        var refused = await CreateAsync(
            org,
            engagement.Id,
            "Drive the van",
            assignedUserId: org.MemberIds[1]);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        var accepted = await CreateAsync(
            org,
            engagement.Id,
            "Drive the van",
            assignedUserId: org.MemberIds[0]);

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task ParticipantsSeeTheWholeListAndWhichRowIsTheirs()
    {
        var org = await NewOrganisationAsync("resp-visible", members: 2);
        var engagement = await NewDraftAsync(org, "Shared list");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await CreateAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);
        await CreateAsync(org, engagement.Id, "Harmonium", org.MemberIds[1]);

        var asFirst = await ListAsync(org.Members[0], org, engagement.Id);

        Assert.Equal(2, asFirst.Count);
        Assert.True(asFirst.Single(job => job.Title == "Tabla").IsYours);
        Assert.False(asFirst.Single(job => job.Title == "Harmonium").IsYours);
    }

    [Fact]
    public async Task SomebodyNotOnTheEventSeesNothing()
    {
        var org = await NewOrganisationAsync("resp-outsider", members: 2);
        var engagement = await NewDraftAsync(org, "Not yours");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);
        await CreateAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        var response = await org.Members[1].GetAsync(
            $"{Responsibilities(org, engagement.Id)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TheAssigneeMarksTheirOwnJobDone()
    {
        var org = await NewOrganisationAsync("resp-progress", members: 1);
        var engagement = await NewDraftAsync(org, "Own work");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var job = await CreateJobAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        var progress = await org.Members[0].PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}/progress",
            new SetResponsibilityProgressRequest(true, "Borrowed Imran's."));

        Assert.Equal(HttpStatusCode.NoContent, progress.StatusCode);

        var updated = Assert.Single(await ListAsync(org.Owner, org, engagement.Id));
        Assert.True(updated.IsDone);
        Assert.Equal("Borrowed Imran's.", updated.Note);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    /// <summary>
    /// "Update own assignments" means their own. A member reaching for a
    /// colleague's row is refused, which is the whole point of the phrase.
    /// </summary>
    [Fact]
    public async Task AMemberCannotTickOffSomebodyElsesJob()
    {
        var org = await NewOrganisationAsync("resp-others", members: 2);
        var engagement = await NewDraftAsync(org, "Not your row");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var job = await CreateJobAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        var attempt = await org.Members[1].PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}/progress",
            new SetResponsibilityProgressRequest(true, null));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    /// <summary>
    /// Members do not get to rename the work, reassign it, or delete it —
    /// those change the event for everybody.
    /// </summary>
    [Fact]
    public async Task AMemberCannotRenameOrReassignOrDeleteAJob()
    {
        var org = await NewOrganisationAsync("resp-member-write", members: 1);
        var engagement = await NewDraftAsync(org, "Organiser only");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var job = await CreateJobAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        var rename = await org.Members[0].PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}",
            new UpdateResponsibilityRequest("Something else", null, org.MemberIds[0]));
        Assert.Equal(HttpStatusCode.Forbidden, rename.StatusCode);

        var create = await org.Members[0].PostAsJsonAsync(
            Responsibilities(org, engagement.Id),
            new CreateResponsibilityRequest("Mine now", null, org.MemberIds[0]));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var delete = await org.Members[0].DeleteAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    /// <summary>
    /// The note is the assignee's account of their own progress. Handing the
    /// job to someone else and leaving it there would put one person's words
    /// under another person's name.
    /// </summary>
    [Fact]
    public async Task ReassigningClearsThePreviousHoldersNote()
    {
        var org = await NewOrganisationAsync("resp-reassign", members: 2);
        var engagement = await NewDraftAsync(org, "Handover");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var job = await CreateJobAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        await org.Members[0].PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}/progress",
            new SetResponsibilityProgressRequest(true, "Got it covered."));

        await org.Owner.PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}",
            new UpdateResponsibilityRequest("Tabla", null, org.MemberIds[1]));

        var updated = Assert.Single(await ListAsync(org.Owner, org, engagement.Id));
        Assert.Equal(org.MemberIds[1], updated.AssignedUserId);
        Assert.Null(updated.Note);
    }

    /// <summary>
    /// Reopening has to clear the completion time too, or the row claims to be
    /// outstanding and finished at once.
    /// </summary>
    [Fact]
    public async Task ReopeningAJobForgetsWhenItWasFinished()
    {
        var org = await NewOrganisationAsync("resp-reopen", members: 1);
        var engagement = await NewDraftAsync(org, "Spoke too soon");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var job = await CreateJobAsync(org, engagement.Id, "Tabla", org.MemberIds[0]);

        var path = $"{Responsibilities(org, engagement.Id)}/{job.Id}/progress";
        await org.Members[0].PutAsJsonAsync(
            path,
            new SetResponsibilityProgressRequest(true, null));
        await org.Members[0].PutAsJsonAsync(
            path,
            new SetResponsibilityProgressRequest(false, "Fell through."));

        var updated = Assert.Single(await ListAsync(org.Owner, org, engagement.Id));
        Assert.False(updated.IsDone);
        Assert.Null(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task AJobNeedsATitle()
    {
        var org = await NewOrganisationAsync("resp-title");
        var engagement = await NewDraftAsync(org, "Nameless");

        var refused = await org.Owner.PostAsJsonAsync(
            Responsibilities(org, engagement.Id),
            new CreateResponsibilityRequest("   ", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    /// <summary>
    /// D-048's item is "responsibilities assigned". A list with an unclaimed
    /// job on it is exactly what an organiser still has to sort out, so it
    /// stays outstanding until every row has a name against it.
    /// </summary>
    [Fact]
    public async Task ReadinessTicksOnlyOnceEveryJobHasAName()
    {
        var org = await NewOrganisationAsync("resp-readiness", members: 1);
        var engagement = await NewDraftAsync(org, "Checklist");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        Assert.Equal(
            "Outstanding",
            await ReadinessStateAsync(org, engagement.Id, "Responsibilities"));

        var job = await CreateJobAsync(org, engagement.Id, "Tabla", assignedUserId: null);
        Assert.Equal(
            "Outstanding",
            await ReadinessStateAsync(org, engagement.Id, "Responsibilities"));

        await org.Owner.PutAsJsonAsync(
            $"{Responsibilities(org, engagement.Id)}/{job.Id}",
            new UpdateResponsibilityRequest("Tabla", null, org.MemberIds[0]));

        Assert.Equal(
            "Done",
            await ReadinessStateAsync(org, engagement.Id, "Responsibilities"));
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string Responsibilities(TestOrganisation org, Guid engagementId) =>
        $"{Engagements(org)}/{engagementId}/responsibilities";

    private static Task<HttpResponseMessage> CreateAsync(
        TestOrganisation org,
        Guid engagementId,
        string title,
        Guid? assignedUserId = null)
    {
        return org.Owner.PostAsJsonAsync(
            Responsibilities(org, engagementId),
            new CreateResponsibilityRequest(title, null, assignedUserId));
    }

    private static async Task<ResponsibilityResponse> CreateJobAsync(
        TestOrganisation org,
        Guid engagementId,
        string title,
        Guid? assignedUserId)
    {
        var response = await CreateAsync(org, engagementId, title, assignedUserId);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ResponsibilityResponse>();
        Assert.NotNull(created);
        return created;
    }

    private static async Task<List<ResponsibilityResponse>> ListAsync(
        HttpClient client,
        TestOrganisation org,
        Guid engagementId)
    {
        var rows = await client.GetFromJsonAsync<List<ResponsibilityResponse>>(
            Responsibilities(org, engagementId));
        Assert.NotNull(rows);
        return rows;
    }

    private static async Task<string> ReadinessStateAsync(
        TestOrganisation org,
        Guid engagementId,
        string item)
    {
        var readiness = await org.Owner
            .GetFromJsonAsync<List<ReadinessEntryResponse>>(
                $"{Engagements(org)}/{engagementId}/readiness");
        Assert.NotNull(readiness);
        return readiness.Single(entry => entry.Item == item).State;
    }

    private static async Task RequestAvailabilityAsync(
        TestOrganisation org,
        Guid engagementId,
        IReadOnlyList<Guid> userIds)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/availability/requests",
            new RequestAvailabilityRequest(userIds));
        response.EnsureSuccessStatusCode();
    }

    private async Task<TestOrganisation> NewOrganisationAsync(
        string prefix,
        int members = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

        var clients = new List<HttpClient>();
        if (members > 0)
        {
            var inviteResponse = await owner.PostAsJsonAsync(
                $"/api/organisations/{organisation.Id}/invitations",
                new CreateInvitationRequest(null));
            inviteResponse.EnsureSuccessStatusCode();
            var invitation =
                await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

            for (var index = 0; index < members; index++)
            {
                var member = CreateClient($"auth0|{prefix}-member-{index}");
                var accept = await member.PostAsync(
                    $"/api/invitations/{invitation.Token}/accept",
                    content: null);
                accept.EnsureSuccessStatusCode();
                clients.Add(member);
            }
        }

        var directory = await owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{organisation.Id}/members");
        Assert.NotNull(directory);

        return new TestOrganisation(
            organisation.Id,
            owner,
            clients,
            directory.Skip(1).Select(row => row.UserId).ToList());
    }

    private async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(
                title,
                new DateOnly(2027, 8, 14),
                null,
                null,
                null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
