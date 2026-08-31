using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public sealed class InvitationsController(
    EnsureUserService ensureUserService,
    InvitationService invitationService,
    OrganisationService organisationService)
    : ControllerBase
{
    /// <summary>
    /// The pre-join preview (D-056): the organisation's identity and nothing
    /// more. Unknown, revoked, and expired tokens are indistinguishable.
    /// </summary>
    [HttpGet("{token}")]
    [ProducesResponseType<InvitationPreviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitationPreviewResponse>> Preview(
        string token,
        CancellationToken cancellationToken)
    {
        var preview = await invitationService.PreviewAsync(token, cancellationToken);
        if (preview is null)
        {
            return NotFound();
        }

        return Ok(new InvitationPreviewResponse(
            preview.Organisation.Name,
            preview.Organisation.GroupType,
            preview.Organisation.LogoUrl));
    }

    [HttpPost("{token}/accept")]
    [ProducesResponseType<AcceptInvitationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AcceptInvitationResponse>> Accept(
        string token,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return Unauthorized();
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        var (result, membership) = await invitationService.AcceptAsync(
            token,
            user.Id,
            cancellationToken);

        if (result == AcceptInvitationResult.NotUsable || membership is null)
        {
            return NotFound();
        }

        var row = await organisationService.GetForMemberAsync(
            membership.OrganisationId,
            user.Id,
            cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(new AcceptInvitationResponse(
            row.Organisation.Id,
            row.Organisation.Name,
            row.Membership.Role.ToString()));
    }
}
