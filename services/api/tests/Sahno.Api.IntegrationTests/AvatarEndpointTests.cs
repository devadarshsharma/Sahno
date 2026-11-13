using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Organisations;
using Sahno.Contracts.Users;

namespace Sahno.Api.IntegrationTests;

/// <summary>Profile photos (D-129).</summary>
public sealed class AvatarEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    /// <summary>A tiny but genuine JPEG header followed by filler: enough to be recognised as one.</summary>
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, .. new byte[500], 0xFF, 0xD9];

    [Fact]
    public async Task APhoto_IsSeenByTheGroup_AndNobodyElse()
    {
        using var owner = CreateClient("auth0|avatar-owner");
        using var member = CreateClient("auth0|avatar-member");
        using var stranger = CreateClient("auth0|avatar-stranger");
        var organisation = await CreateOrganisationAsync(owner, "Photo Org");
        await JoinAsync(owner, member, organisation.Id);

        var upload = await UploadAsync(member, Jpeg);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var version = await upload.Content.ReadFromJsonAsync<AvatarVersionResponse>();
        Assert.NotNull(version);

        var listed = await owner.GetFromJsonAsync<List<AvatarVersionResponse>>($"/api/organisations/{organisation.Id}/avatars");
        Assert.Contains(listed!, row => row.UserId == version.UserId && row.Version == version.Version);

        var seen = await owner.GetAsync($"/api/users/{version.UserId}/avatar?v={version.Version}");
        Assert.Equal(HttpStatusCode.OK, seen.StatusCode);
        Assert.Equal("image/jpeg", seen.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Jpeg, await seen.Content.ReadAsByteArrayAsync());
        Assert.Contains("immutable", seen.Headers.CacheControl?.ToString());

        // Not in any group together: not even told there is one.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/users/{version.UserId}/avatar")).StatusCode);

        // Removed, it is gone for everyone.
        Assert.Equal(HttpStatusCode.NoContent, (await member.DeleteAsync("/api/me/avatar")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/users/{version.UserId}/avatar")).StatusCode);
    }

    [Fact]
    public async Task OnlyImages_AndOnlySmallOnes()
    {
        using var someone = CreateClient("auth0|avatar-picky");

        var text = await UploadAsync(someone, "definitely not a photo, just words"u8.ToArray());
        Assert.Equal(HttpStatusCode.BadRequest, text.StatusCode);

        var huge = await UploadAsync(someone, [0xFF, 0xD8, 0xFF, .. new byte[400 * 1024]]);
        Assert.True(huge.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "photo", "photo.jpg");
        return client.PutAsync("/api/me/avatar", form);
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }

    private static async Task<OrganisationResponse> CreateOrganisationAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/organisations", new CreateOrganisationRequest(name, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
    }

    private static async Task JoinAsync(HttpClient owner, HttpClient joiner, Guid organisationId)
    {
        var invite = await owner.PostAsJsonAsync($"/api/organisations/{organisationId}/invitations", new CreateInvitationRequest(null));
        invite.EnsureSuccessStatusCode();
        var invitation = (await invite.Content.ReadFromJsonAsync<InvitationResponse>())!;
        (await joiner.PostAsync($"/api/invitations/{invitation.Token}/accept", content: null)).EnsureSuccessStatusCode();
    }
}
