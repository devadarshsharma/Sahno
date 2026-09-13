using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;
using Sahno.Contracts.Repertoire;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// The repertoire and set lists (D-079). The repertoire is the group's: any
/// member adds and edits pieces and lyrics; only organisers delete, and only
/// organisers see the notes. A set list is read by the lineup and arranged
/// by organisers, and it counts as "repertoire ready" for readiness.
/// </summary>
public sealed class RepertoireEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AnyMemberAddsAPieceAndEditsItsLyrics()
    {
        var org = await NewOrganisationAsync("rep-member-edit", members: 1);
        var member = org.Members[0];

        var created = await member.PostAsJsonAsync(
            Repertoire(org),
            new SavePieceRequest("Tumhe Dillagi", "Nusrat", "Urdu", "Raag Bhairavi", 18, "Tumhe dillagi bhool jani paregi"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var piece = await created.Content.ReadFromJsonAsync<PieceResponse>();
        Assert.NotNull(piece);
        Assert.True(piece.HasLyrics);

        var edited = await member.PutAsJsonAsync(
            $"{Repertoire(org)}/{piece.Id}",
            new SavePieceRequest("Tumhe Dillagi", "Nusrat Fateh Ali Khan", "Urdu", "Bhairavi", 18, "Tumhe dillagi bhool jani paregi\nMohabbat ki raahon mein aa kar to dekho"));
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);

        var detail = await org.Owner.GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Nusrat Fateh Ali Khan", detail.Piece.Attribution);
        Assert.Contains("Mohabbat ki raahon", detail.Piece.Lyrics);
    }

    [Fact]
    public async Task TheListTravelsWithoutLyricsAndTheDetailWithThem()
    {
        var org = await NewOrganisationAsync("rep-list-shape");
        var piece = await NewPieceAsync(org, "Allah Hoo", lyrics: "Allah hoo, Allah hoo");

        var list = await org.Owner.GetFromJsonAsync<List<PieceResponse>>(Repertoire(org));
        Assert.NotNull(list);
        var row = Assert.Single(list);
        Assert.True(row.HasLyrics);
        Assert.Null(row.Lyrics);

        var detail = await org.Owner.GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Allah hoo, Allah hoo", detail.Piece.Lyrics);
    }

    [Fact]
    public async Task OrganiserNotesNeverReachAMember()
    {
        var org = await NewOrganisationAsync("rep-notes", members: 1);
        var piece = await NewPieceAsync(org, "Mast Qalandar");

        var memberWrites = await org.Members[0].PutAsJsonAsync(
            $"{Repertoire(org)}/{piece.Id}/notes",
            new UpdatePieceNotesRequest("mine"));
        Assert.Equal(HttpStatusCode.Forbidden, memberWrites.StatusCode);

        var ownerWrites = await org.Owner.PutAsJsonAsync(
            $"{Repertoire(org)}/{piece.Id}/notes",
            new UpdatePieceNotesRequest("Drop the third verse on a short set."));
        Assert.Equal(HttpStatusCode.NoContent, ownerWrites.StatusCode);

        var asOwner = await org.Owner.GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.Equal("Drop the third verse on a short set.", asOwner!.Piece.Notes);

        var asMember = await org.Members[0].GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.Null(asMember!.Piece.Notes);

        var raw = await org.Members[0].GetStringAsync(Repertoire(org));
        Assert.DoesNotContain("third verse", raw);
    }

    [Fact]
    public async Task OnlyAnOrganiserDeletesAPieceAndItLeavesEverySetList()
    {
        var org = await NewOrganisationAsync("rep-delete", members: 1);
        var piece = await NewPieceAsync(org, "Short-lived");
        var engagement = await NewDraftAsync(org, "Gig");
        await AddToSetListAsync(org, engagement.Id, piece.Id);

        var memberDeletes = await org.Members[0].DeleteAsync($"{Repertoire(org)}/{piece.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, memberDeletes.StatusCode);

        var ownerDeletes = await org.Owner.DeleteAsync($"{Repertoire(org)}/{piece.Id}");
        Assert.Equal(HttpStatusCode.NoContent, ownerDeletes.StatusCode);

        var setList = await org.Owner.GetFromJsonAsync<List<SetListEntryResponse>>(SetList(org, engagement.Id));
        Assert.NotNull(setList);
        Assert.Empty(setList);
    }

    [Fact]
    public async Task LinksNeedAWebAddress()
    {
        var org = await NewOrganisationAsync("rep-links");
        var piece = await NewPieceAsync(org, "Linked");

        var bad = await org.Owner.PostAsJsonAsync(
            $"{Repertoire(org)}/{piece.Id}/links",
            new AddPieceLinkRequest("Recording", "not a url"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await org.Owner.PostAsJsonAsync(
            $"{Repertoire(org)}/{piece.Id}/links",
            new AddPieceLinkRequest(null, "https://youtube.com/watch?v=abc"));
        Assert.Equal(HttpStatusCode.Created, good.StatusCode);
        var link = await good.Content.ReadFromJsonAsync<PieceLinkResponse>();
        Assert.Equal("youtube.com", link!.Title);

        var detail = await org.Owner.GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.Single(detail!.Piece.Links);

        var removed = await org.Owner.DeleteAsync($"{Repertoire(org)}/{piece.Id}/links/{link.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact]
    public async Task ASetListIsArrangedByOrganisersAndReadByTheLineup()
    {
        var org = await NewOrganisationAsync("rep-setlist", members: 2);
        var opener = await NewPieceAsync(org, "Opener");
        var closer = await NewPieceAsync(org, "Closer");
        var engagement = await NewDraftAsync(org, "Concert");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);

        // A member cannot arrange it.
        var memberAdds = await org.Members[0].PostAsJsonAsync(
            SetList(org, engagement.Id),
            new AddSetListEntryRequest(opener.Id, null));
        Assert.Equal(HttpStatusCode.Forbidden, memberAdds.StatusCode);

        var first = await AddToSetListAsync(org, engagement.Id, closer.Id, "Encore if they ask");
        var second = await AddToSetListAsync(org, engagement.Id, opener.Id);

        // The organiser puts them in the right order.
        var reorder = await org.Owner.PutAsJsonAsync(
            $"{SetList(org, engagement.Id)}/reorder",
            new ReorderSetListRequest([second.Id, first.Id]));
        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);

        // Somebody on the lineup reads it, in order, with the note.
        var asLineup = await org.Members[0].GetFromJsonAsync<List<SetListEntryResponse>>(SetList(org, engagement.Id));
        Assert.NotNull(asLineup);
        Assert.Equal(["Opener", "Closer"], asLineup.Select(entry => entry.Title).ToArray());
        Assert.Equal("Encore if they ask", asLineup[1].Note);

        // Somebody not on the lineup does not see the event at all.
        var outsider = await org.Members[1].GetAsync(SetList(org, engagement.Id));
        Assert.Equal(HttpStatusCode.NotFound, outsider.StatusCode);

        // A reorder that drops an entry is refused.
        var partial = await org.Owner.PutAsJsonAsync(
            $"{SetList(org, engagement.Id)}/reorder",
            new ReorderSetListRequest([first.Id]));
        Assert.Equal(HttpStatusCode.BadRequest, partial.StatusCode);

        // Removing one closes the gap.
        var removed = await org.Owner.DeleteAsync($"{SetList(org, engagement.Id)}/{second.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        var remaining = await org.Owner.GetFromJsonAsync<List<SetListEntryResponse>>(SetList(org, engagement.Id));
        Assert.Equal(0, Assert.Single(remaining!).Position);
    }

    [Fact]
    public async Task AStrangersPieceCannotGoOnASetList()
    {
        var ours = await NewOrganisationAsync("rep-stranger-a");
        var theirs = await NewOrganisationAsync("rep-stranger-b");
        var stranger = await NewPieceAsync(theirs, "Not ours");
        var engagement = await NewDraftAsync(ours, "Ours");

        var response = await ours.Owner.PostAsJsonAsync(
            SetList(ours, engagement.Id),
            new AddSetListEntryRequest(stranger.Id, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var peek = await ours.Owner.GetAsync($"{Repertoire(ours)}/{stranger.Id}");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
    }

    [Fact]
    public async Task ASetListCountsAsRepertoireReadyAndShowsInThePiecesHistory()
    {
        var org = await NewOrganisationAsync("rep-ready");
        var piece = await NewPieceAsync(org, "Counted");
        var engagement = await NewDraftAsync(org, "Ready");

        var before = await org.Owner.GetFromJsonAsync<EngagementResponse>(Engagement(org, engagement.Id));
        Assert.Contains("Resources", before!.ReadinessMissing);

        await AddToSetListAsync(org, engagement.Id, piece.Id);
        await AddToSetListAsync(org, engagement.Id, piece.Id); // an encore is still one booking

        var after = await org.Owner.GetFromJsonAsync<EngagementResponse>(Engagement(org, engagement.Id));
        Assert.DoesNotContain("Resources", after!.ReadinessMissing);

        var list = await org.Owner.GetFromJsonAsync<List<PieceResponse>>(Repertoire(org));
        Assert.Equal(1, Assert.Single(list!).UseCount);

        var detail = await org.Owner.GetFromJsonAsync<PieceDetailResponse>($"{Repertoire(org)}/{piece.Id}");
        Assert.Equal("Ready", Assert.Single(detail!.Bookings).Title);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Repertoire(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/repertoire";

    private static string Engagement(TestOrganisation org, Guid engagementId) =>
        $"/api/organisations/{org.Id}/engagements/{engagementId}";

    private static string SetList(TestOrganisation org, Guid engagementId) =>
        $"{Engagement(org, engagementId)}/setlist";

    private static async Task<PieceResponse> NewPieceAsync(
        TestOrganisation org,
        string title,
        string? lyrics = null)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Repertoire(org),
            new SavePieceRequest(title, null, null, null, null, lyrics));
        response.EnsureSuccessStatusCode();
        var piece = await response.Content.ReadFromJsonAsync<PieceResponse>();
        Assert.NotNull(piece);
        return piece;
    }

    private static async Task<SetListEntryResponse> AddToSetListAsync(
        TestOrganisation org,
        Guid engagementId,
        Guid pieceId,
        string? note = null)
    {
        var response = await org.Owner.PostAsJsonAsync(
            SetList(org, engagementId),
            new AddSetListEntryRequest(pieceId, note));
        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<SetListEntryResponse>();
        Assert.NotNull(entry);
        return entry;
    }

    private static async Task RequestAvailabilityAsync(
        TestOrganisation org,
        Guid engagementId,
        IReadOnlyList<Guid> userIds)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagementId)}/availability/requests",
            new RequestAvailabilityRequest(userIds));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<EngagementResponse> NewDraftAsync(TestOrganisation org, string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(title, new DateOnly(2027, 8, 14), null, null, null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    private async Task<TestOrganisation> NewOrganisationAsync(string prefix, int members = 0)
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

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
