using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Organisations;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

[ApiController]
[Route("api/organisations")]
[Authorize]
public sealed class OrganisationsController(
    EnsureUserService ensureUserService,
    OrganisationService organisationService,
    InvitationService invitationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrganisationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganisationResponse>> Create(
        CreateOrganisationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "An organisation name is required." });
        }

        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var created = await organisationService.CreateAsync(
            user.Id,
            request.Name,
            request.GroupType,
            request.TimeZoneId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Organisation.Id },
            ToResponse(created));
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrganisationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganisationResponse>>> List(
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var rows = await organisationService.ListForUserAsync(
            user.Id,
            cancellationToken);

        return Ok(rows.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrganisationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganisationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var row = await organisationService.GetForMemberAsync(
            id,
            user.Id,
            cancellationToken);

        // Non-members get 404 so organisation existence is not disclosed.
        return row is null ? NotFound() : Ok(ToResponse(row));
    }

    [HttpPost("{id:guid}/setup-checklist/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissSetupChecklist(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dismissed = await organisationService.DismissSetupChecklistAsync(
            id,
            user.Id,
            cancellationToken);

        return dismissed ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/invitations")]
    [ProducesResponseType<InvitationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitationResponse>> CreateInvitation(
        Guid id,
        CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var membership = await authorization.FindMembershipAsync(
            id,
            user.Id,
            cancellationToken);
        if (membership is null)
        {
            return NotFound();
        }

        if (!OrganisationAuthorizationService.IsOrganiser(membership))
        {
            return Forbid();
        }

        var invitation = await invitationService.CreateLinkAsync(
            id,
            user.Id,
            request.ExpiresAtUtc,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(invitation));
    }

    [HttpGet("{id:guid}/invitations")]
    [ProducesResponseType<IReadOnlyList<InvitationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitationResponse>>> ListInvitations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var membership = await authorization.FindMembershipAsync(
            id,
            user.Id,
            cancellationToken);
        if (membership is null)
        {
            return NotFound();
        }

        if (!OrganisationAuthorizationService.IsOrganiser(membership))
        {
            return Forbid();
        }

        var invitations = await invitationService.ListAsync(id, cancellationToken);
        return Ok(invitations.Select(ToResponse).ToList());
    }

    [HttpDelete("{id:guid}/invitations/{invitationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvitation(
        Guid id,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var membership = await authorization.FindMembershipAsync(
            id,
            user.Id,
            cancellationToken);
        if (membership is null)
        {
            return NotFound();
        }

        if (!OrganisationAuthorizationService.IsOrganiser(membership))
        {
            return Forbid();
        }

        var revoked = await invitationService.RevokeAsync(
            id,
            invitationId,
            cancellationToken);

        return revoked ? NoContent() : NotFound();
    }

    private async Task<Domain.Users.User?> CurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return null;
        }

        return await ensureUserService.EnsureAsync(identity, cancellationToken);
    }

    private static OrganisationResponse ToResponse(OrganisationMembership row)
    {
        return new OrganisationResponse(
            row.Organisation.Id,
            row.Organisation.Name,
            row.Organisation.GroupType,
            row.Organisation.TimeZoneId,
            row.Organisation.LogoUrl,
            row.Membership.Role.ToString(),
            ShowSetupChecklist:
                row.Membership.Role == MembershipRole.Owner
                && row.Membership.SetupChecklistDismissedAtUtc is null);
    }

    private static InvitationResponse ToResponse(Invitation invitation)
    {
        return new InvitationResponse(
            invitation.Id,
            invitation.Token,
            invitation.Type.ToString(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.RevokedAtUtc);
    }
}
