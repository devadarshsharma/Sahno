using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// The customer directory (D-022). A returning customer is picked, not
/// retyped; the directory shows how often each one has booked; and a member
/// never reaches any of it.
/// </summary>
public sealed class CustomerEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task ADirectoryCountsHowOftenEachCustomerHasBooked()
    {
        var org = await NewOrganisationAsync("cust-count");
        var uoc = await NewCustomerAsync(org, "University of Canberra");
        var patels = await NewCustomerAsync(org, "The Patel family");

        // Two bookings in UOC's name, one for the Patels, one for nobody.
        await NewDraftAsync(org, "O-week", uoc.Id);
        await NewDraftAsync(org, "Graduation", uoc.Id);
        await NewDraftAsync(org, "Wedding", patels.Id);
        await NewDraftAsync(org, "Unassigned", null);

        var directory = await org.Owner.GetFromJsonAsync<List<CustomerResponse>>(Customers(org));
        Assert.NotNull(directory);
        Assert.Equal(2, directory.Count);
        Assert.Equal(2, directory.Single(row => row.Id == uoc.Id).BookingCount);
        Assert.Equal(1, directory.Single(row => row.Id == patels.Id).BookingCount);

        var detail = await org.Owner.GetFromJsonAsync<CustomerDetailResponse>(
            $"{Customers(org)}/{uoc.Id}");
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Bookings.Count);
        Assert.Contains(detail.Bookings, booking => booking.Title == "O-week");
        Assert.Contains(detail.Bookings, booking => booking.Title == "Graduation");
    }

    [Fact]
    public async Task ABookingCreatedInACustomersNameStartsLinked()
    {
        var org = await NewOrganisationAsync("cust-create");
        var uoc = await NewCustomerAsync(org, "University of Canberra");

        var engagement = await NewDraftAsync(org, "Open day", uoc.Id);

        var link = await org.Owner.GetFromJsonAsync<EngagementCustomerResponse>(
            $"/api/organisations/{org.Id}/engagements/{engagement.Id}/customer");
        Assert.NotNull(link);
        Assert.NotNull(link.Customer);
        Assert.Equal(uoc.Id, link.Customer.Id);
    }

    [Fact]
    public async Task ABookingCannotBeCreatedForAStrangersCustomer()
    {
        var ours = await NewOrganisationAsync("cust-stranger-a");
        var theirs = await NewOrganisationAsync("cust-stranger-b");
        var stranger = await NewCustomerAsync(theirs, "Not ours");

        var response = await ours.Owner.PostAsJsonAsync(
            $"/api/organisations/{ours.Id}/engagements",
            new CreateEngagementRequest("Nope", null, null, null, null, stranger.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnOrganiserEditsACustomerAndANameIsRequired()
    {
        var org = await NewOrganisationAsync("cust-edit");
        var customer = await NewCustomerAsync(org, "Patel");

        var renamed = await org.Owner.PutAsJsonAsync(
            $"{Customers(org)}/{customer.Id}",
            new SaveCustomerRequest("The Patel family", "Meera Patel", "0400 222 333", null, "Prefers evenings."));
        Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);

        var detail = await org.Owner.GetFromJsonAsync<CustomerDetailResponse>(
            $"{Customers(org)}/{customer.Id}");
        Assert.NotNull(detail);
        Assert.Equal("The Patel family", detail.Customer.Name);
        Assert.Equal("Meera Patel", detail.Customer.ContactName);
        Assert.Equal("Prefers evenings.", detail.Customer.Notes);

        var blank = await org.Owner.PutAsJsonAsync(
            $"{Customers(org)}/{customer.Id}",
            new SaveCustomerRequest("   ", null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        var created = await org.Owner.PostAsJsonAsync(
            Customers(org),
            new SaveCustomerRequest("", null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
    }

    /// <summary>D-022: the directory is organiser-only, every route of it.</summary>
    [Fact]
    public async Task AMemberIsRefusedTheWholeDirectory()
    {
        var org = await NewOrganisationAsync("cust-member", members: 1);
        var customer = await NewCustomerAsync(org, "Hidden");
        var member = org.Members[0];

        var list = await member.GetAsync(Customers(org));
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        var one = await member.GetAsync($"{Customers(org)}/{customer.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, one.StatusCode);

        var create = await member.PostAsJsonAsync(
            Customers(org),
            new SaveCustomerRequest("Mine", null, null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var edit = await member.PutAsJsonAsync(
            $"{Customers(org)}/{customer.Id}",
            new SaveCustomerRequest("Renamed", null, null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }

    [Fact]
    public async Task OneOrganisationNeverSeesAnothersCustomers()
    {
        var a = await NewOrganisationAsync("cust-iso-a");
        var b = await NewOrganisationAsync("cust-iso-b");
        var theirs = await NewCustomerAsync(b, "B's customer");

        var directory = await a.Owner.GetFromJsonAsync<List<CustomerResponse>>(Customers(a));
        Assert.NotNull(directory);
        Assert.Empty(directory);

        var peek = await a.Owner.GetAsync($"{Customers(a)}/{theirs.Id}");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members);

    private static string Customers(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/customers";

    private static async Task<CustomerResponse> NewCustomerAsync(TestOrganisation org, string name)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Customers(org),
            new SaveCustomerRequest(name, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer;
    }

    private static async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title,
        Guid? customerId)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(title, new DateOnly(2027, 8, 14), null, null, null, customerId));
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

        return new TestOrganisation(organisation.Id, owner, clients);
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
