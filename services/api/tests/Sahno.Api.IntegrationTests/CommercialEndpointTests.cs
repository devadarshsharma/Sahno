using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Customer, finance, and performer payments (Slice 11, D-008, D-016, D-022).
///
/// The permission lines are the substance, and each one is tested from the
/// side that must be refused: a member reaching for the customer, an Admin
/// without the grant reaching for the money, and the same Admin once the
/// Owner grants it. The rest is that an unpaid performer stays visible after
/// the event is over, because that is the whole reason the row exists.
/// </summary>
public sealed class CommercialEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AnOrganiserLinksABookingToADirectoryCustomerAndKeepsTheirOwnNotes()
    {
        var org = await NewOrganisationAsync("comm-customer");
        var engagement = await NewDraftAsync(org, "Mehndi night");
        var khans = await NewCustomerAsync(org, "The Khan family", "0400 000 000");

        var saved = await org.Owner.PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/customer",
            new UpdateEngagementCustomerRequest(
                khans.Id,
                "Came via Imran. Wants the long qaul first."));
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);

        var customer = await org.Owner.GetFromJsonAsync<EngagementCustomerResponse>(
            $"{Engagement(org, engagement.Id)}/customer");
        Assert.NotNull(customer);
        Assert.NotNull(customer.Customer);
        Assert.Equal("The Khan family", customer.Customer.Name);
        Assert.Equal("0400 000 000", customer.Customer.Phone);
        Assert.Equal(1, customer.Customer.BookingCount);
        Assert.Contains("Wants the long qaul first", customer.PrivateNotes);
    }

    /// <summary>A customer from some other organisation cannot be linked.</summary>
    [Fact]
    public async Task AnotherOrganisationsCustomerCannotBeLinked()
    {
        var ours = await NewOrganisationAsync("comm-cross-a");
        var theirs = await NewOrganisationAsync("comm-cross-b");
        var engagement = await NewDraftAsync(ours, "Ours");
        var stranger = await NewCustomerAsync(theirs, "Their customer", null);

        var saved = await ours.Owner.PutAsJsonAsync(
            $"{Engagement(ours, engagement.Id)}/customer",
            new UpdateEngagementCustomerRequest(stranger.Id, null));
        Assert.Equal(HttpStatusCode.NotFound, saved.StatusCode);
    }

    /// <summary>
    /// D-022: customer phone numbers, emails, and private notes are hidden
    /// from Members. On the event or not, the answer is the same.
    /// </summary>
    [Fact]
    public async Task AMemberOnTheEventStillCannotSeeTheCustomer()
    {
        var org = await NewOrganisationAsync("comm-member-customer", members: 1);
        var engagement = await NewDraftAsync(org, "Private");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        var read = await org.Members[0].GetAsync($"{Engagement(org, engagement.Id)}/customer");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var write = await org.Members[0].PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/customer",
            new UpdateEngagementCustomerRequest(null, "mine"));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    /// <summary>
    /// The engagement itself never carries the customer, so there is no field
    /// a member's screen could render by mistake.
    /// </summary>
    [Fact]
    public async Task TheEngagementResponseCarriesNoCustomerOrMoney()
    {
        var org = await NewOrganisationAsync("comm-shape", members: 1);
        var engagement = await NewDraftAsync(org, "Shape");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var secret = await NewCustomerAsync(org, "Secret Customer", "0400 111 222");
        await org.Owner.PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/customer",
            new UpdateEngagementCustomerRequest(secret.Id, "private"));
        await org.Owner.PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/finance",
            new UpdateFinanceRequest(2500m, 2200m, 500m, null, null, "net 7"));

        var raw = await org.Members[0].GetStringAsync(Engagement(org, engagement.Id));

        Assert.DoesNotContain("Secret Customer", raw);
        Assert.DoesNotContain("0400 111 222", raw);
        Assert.DoesNotContain("2200", raw);
        Assert.DoesNotContain("financeOutstanding\":1", raw);
    }

    /// <summary>
    /// D-016: the permission is off by default for a new Admin. Making
    /// somebody an Admin does not hand them the money.
    /// </summary>
    [Fact]
    public async Task AnAdminWithoutTheGrantIsRefusedTheMoney()
    {
        var org = await NewOrganisationAsync("comm-admin-nogrant", members: 1);
        var engagement = await NewDraftAsync(org, "Money");
        await MakeAdminAsync(org, org.MembershipIds[0], canManageFinances: null);

        var finance = await org.Members[0].GetAsync($"{Engagement(org, engagement.Id)}/finance");
        Assert.Equal(HttpStatusCode.Forbidden, finance.StatusCode);

        var payments = await org.Members[0].GetAsync($"{Engagement(org, engagement.Id)}/payments");
        Assert.Equal(HttpStatusCode.Forbidden, payments.StatusCode);

        // But the customer — non-financial — is theirs (D-022).
        var customer = await org.Members[0].GetAsync($"{Engagement(org, engagement.Id)}/customer");
        Assert.Equal(HttpStatusCode.OK, customer.StatusCode);
    }

    [Fact]
    public async Task TheOwnerGrantsFinancialAccessAndTheAdminCanThenSeeIt()
    {
        var org = await NewOrganisationAsync("comm-admin-grant", members: 1);
        var engagement = await NewDraftAsync(org, "Granted");
        await MakeAdminAsync(org, org.MembershipIds[0], canManageFinances: true);

        var saved = await org.Members[0].PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/finance",
            new UpdateFinanceRequest(3000m, 2800m, 800m, new DateOnly(2027, 7, 1), null, null));
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);

        var finance = await org.Members[0].GetFromJsonAsync<FinanceResponse>(
            $"{Engagement(org, engagement.Id)}/finance");
        Assert.NotNull(finance);
        Assert.Equal(2800m, finance.AgreedFee);
        Assert.Equal(2000m, finance.Balance);
        Assert.True(finance.IsCustomerBalanceOutstanding);
    }

    [Fact]
    public async Task ANegativeFeeIsRefused()
    {
        var org = await NewOrganisationAsync("comm-negative");
        var engagement = await NewDraftAsync(org, "Typo");

        var refused = await org.Owner.PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/finance",
            new UpdateFinanceRequest(null, -100m, null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task APaymentGoesToSomebodyOnTheEvent()
    {
        var org = await NewOrganisationAsync("comm-payment", members: 2);
        var engagement = await NewDraftAsync(org, "Paid gig");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);

        var onEvent = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments",
            new CreatePerformerPaymentRequest(org.MemberIds[0], 150m, "Tabla"));
        Assert.Equal(HttpStatusCode.Created, onEvent.StatusCode);

        var notOnEvent = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments",
            new CreatePerformerPaymentRequest(org.MemberIds[1], 150m, null));
        Assert.Equal(HttpStatusCode.BadRequest, notOnEvent.StatusCode);
    }

    /// <summary>
    /// The row that gives Slice 11 its point: a performer still owed after the
    /// event is over keeps appearing to whoever holds the money, until it is
    /// marked paid.
    /// </summary>
    [Fact]
    public async Task AnUnpaidPerformerStaysOutstandingAfterTheEventCompletes()
    {
        var org = await NewOrganisationAsync("comm-outstanding", members: 1);
        var engagement = await NewDraftAsync(org, "Done and dusted");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await TransitionAsync(org, engagement.Id, "Tentative");
        await TransitionAsync(org, engagement.Id, "Confirmed");

        var created = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments",
            new CreatePerformerPaymentRequest(org.MemberIds[0], 200m, null));
        var payment = await created.Content.ReadFromJsonAsync<PerformerPaymentResponse>();
        Assert.NotNull(payment);

        await TransitionAsync(org, engagement.Id, "Completed");

        Assert.Equal(1, await FinanceOutstandingAsync(org, engagement.Id));

        var settled = await org.Owner.PutAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments/{payment.Id}",
            new UpdatePerformerPaymentRequest(200m, null, new DateOnly(2027, 8, 20)));
        Assert.Equal(HttpStatusCode.NoContent, settled.StatusCode);

        Assert.Equal(0, await FinanceOutstandingAsync(org, engagement.Id));
    }

    /// <summary>
    /// The count is only for people who may know it. An Admin without the
    /// grant sees null — not zero, which would claim there is nothing owed.
    /// </summary>
    [Fact]
    public async Task TheOutstandingCountIsNullWithoutFinancialAccess()
    {
        var org = await NewOrganisationAsync("comm-null", members: 1);
        var engagement = await NewDraftAsync(org, "Hidden count");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments",
            new CreatePerformerPaymentRequest(org.MemberIds[0], 100m, null));
        await MakeAdminAsync(org, org.MembershipIds[0], canManageFinances: null);

        var asAdmin = await org.Members[0].GetFromJsonAsync<EngagementResponse>(
            Engagement(org, engagement.Id));

        Assert.NotNull(asAdmin);
        Assert.Null(asAdmin.FinanceOutstanding);
    }

    /// <summary>
    /// The day money changed hands is a fact. Marking paid twice keeps the
    /// first date; setting null takes it back.
    /// </summary>
    [Fact]
    public async Task TheFirstPaidDateStandsAndNullUnsettles()
    {
        var org = await NewOrganisationAsync("comm-paid-date", members: 1);
        var engagement = await NewDraftAsync(org, "Dates");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var created = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagement.Id)}/payments",
            new CreatePerformerPaymentRequest(org.MemberIds[0], 100m, null));
        var payment = await created.Content.ReadFromJsonAsync<PerformerPaymentResponse>();
        Assert.NotNull(payment);
        var path = $"{Engagement(org, engagement.Id)}/payments/{payment.Id}";

        await org.Owner.PutAsJsonAsync(path, new UpdatePerformerPaymentRequest(100m, null, new DateOnly(2027, 8, 20)));
        await org.Owner.PutAsJsonAsync(path, new UpdatePerformerPaymentRequest(100m, null, new DateOnly(2027, 8, 25)));

        var rows = await org.Owner.GetFromJsonAsync<List<PerformerPaymentResponse>>(
            $"{Engagement(org, engagement.Id)}/payments");
        Assert.NotNull(rows);
        Assert.Equal(new DateOnly(2027, 8, 20), Assert.Single(rows).PaidOn);

        await org.Owner.PutAsJsonAsync(path, new UpdatePerformerPaymentRequest(100m, null, null));
        rows = await org.Owner.GetFromJsonAsync<List<PerformerPaymentResponse>>(
            $"{Engagement(org, engagement.Id)}/payments");
        Assert.NotNull(rows);
        Assert.False(Assert.Single(rows).IsPaid);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds,
        IReadOnlyList<Guid> MembershipIds);

    private static string Engagement(TestOrganisation org, Guid engagementId) =>
        $"/api/organisations/{org.Id}/engagements/{engagementId}";

    private static async Task<int?> FinanceOutstandingAsync(TestOrganisation org, Guid engagementId)
    {
        var engagement = await org.Owner.GetFromJsonAsync<EngagementResponse>(
            Engagement(org, engagementId));
        Assert.NotNull(engagement);
        return engagement.FinanceOutstanding;
    }

    private static async Task MakeAdminAsync(
        TestOrganisation org,
        Guid membershipId,
        bool? canManageFinances)
    {
        var role = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{membershipId}",
            new UpdateMemberRequest("Admin", null, null));
        role.EnsureSuccessStatusCode();

        if (canManageFinances is not null)
        {
            var grant = await org.Owner.PatchAsJsonAsync(
                $"/api/organisations/{org.Id}/members/{membershipId}",
                new UpdateMemberRequest(null, canManageFinances, null));
            grant.EnsureSuccessStatusCode();
        }
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

    private static async Task RespondAsync(
        HttpClient member,
        TestOrganisation org,
        Guid engagementId,
        string response)
    {
        var result = await member.PutAsJsonAsync(
            $"{Engagement(org, engagementId)}/availability/me",
            new RespondAvailabilityRequest(response));
        result.EnsureSuccessStatusCode();
    }

    private static async Task TransitionAsync(TestOrganisation org, Guid engagementId, string target)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagement(org, engagementId)}/transition",
            new TransitionEngagementRequest(target, null, true));
        response.EnsureSuccessStatusCode();
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
            directory.Skip(1).Select(row => row.UserId).ToList(),
            directory.Skip(1).Select(row => row.MembershipId).ToList());
    }

    private static async Task<CustomerResponse> NewCustomerAsync(
        TestOrganisation org,
        string name,
        string? phone)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/customers",
            new SaveCustomerRequest(name, null, phone, null, null));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer;
    }

    private async Task<EngagementResponse> NewDraftAsync(TestOrganisation org, string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(title, new DateOnly(2027, 8, 14), null, null, null));
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
