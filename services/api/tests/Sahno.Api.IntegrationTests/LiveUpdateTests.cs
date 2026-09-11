using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Api.Live;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Live updates (Slice 10 increment C; TECHNICAL_ARCHITECTURE realtime).
///
/// The signal carries nothing, so there is only one thing to prove about its
/// content — that it arrives — and two about who gets it: members of the
/// organisation that changed, and nobody else.
/// </summary>
public sealed class LiveUpdateTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AChangeInTheOrganisationReachesAConnectedMember()
    {
        var org = await NewOrganisationAsync("live-member", members: 1);
        await using var connection = await ConnectAsync(org.Id, $"auth0|live-member-member-0");
        var changed = new TaskCompletionSource();
        connection.On(LiveHub.ChangedEvent, () => changed.TrySetResult());

        await NewDraftAsync(org, "Something new");

        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AChangeElsewhereDoesNotReachThem()
    {
        var mine = await NewOrganisationAsync("live-mine", members: 1);
        var theirs = await NewOrganisationAsync("live-theirs");
        await using var connection = await ConnectAsync(mine.Id, "auth0|live-mine-member-0");
        var changed = new TaskCompletionSource();
        connection.On(LiveHub.ChangedEvent, () => changed.TrySetResult());

        await NewDraftAsync(theirs, "Not your organisation");

        var winner = await Task.WhenAny(changed.Task, Task.Delay(1500));
        Assert.NotSame(changed.Task, winner);
    }

    /// <summary>
    /// The same silence a non-member gets from every other endpoint of the
    /// organisation. The connection is dropped rather than left listening.
    /// </summary>
    [Fact]
    public async Task ANonMemberIsRefusedTheConnection()
    {
        var org = await NewOrganisationAsync("live-outsider");
        var outsider = factory.CreateClient();

        var connection = Build(org.Id, "auth0|live-outsider-stranger");
        await connection.StartAsync();

        // The hub aborts inside OnConnectedAsync; the client sees that as a
        // closed connection a moment later.
        var closed = new TaskCompletionSource();
        connection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        if (connection.State != HubConnectionState.Disconnected)
        {
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.Equal(HubConnectionState.Disconnected, connection.State);
        outsider.Dispose();
    }

    private HubConnection Build(Guid organisationId, string subject)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                $"http://localhost{LiveHub.Path}?organisationId={organisationId}",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    options.Headers[TestAuthDefaults.SubjectHeader] = subject;
                    // The test server has no websocket; long polling proves
                    // the same hub, group, and broadcast path.
                    options.Transports = HttpTransportType.LongPolling;
                })
            .Build();
    }

    private async Task<HubConnection> ConnectAsync(Guid organisationId, string subject)
    {
        var connection = Build(organisationId, subject);
        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);
        return connection;
    }

    private sealed record TestOrganisation(Guid Id, HttpClient Owner);

    private async Task<TestOrganisation> NewOrganisationAsync(string prefix, int members = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

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
            }
        }

        return new TestOrganisation(organisation.Id, owner);
    }

    private static async Task NewDraftAsync(TestOrganisation org, string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(title, new DateOnly(2027, 8, 14), null, null, null));
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
