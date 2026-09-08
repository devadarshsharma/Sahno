using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Organisations;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// The member directory and its management (Slice 3). Every action starts from
/// the caller's own membership record, which is the only source of authority
/// (D-063) — never a role supplied by the client.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/members")]
[Authorize]
public sealed class MembersController(
    EnsureUserService ensureUserService,
    MembershipService membershipService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    /// <summary>
    /// The directory every member can see. Names are shared; email addresses
    /// are withheld from ordinary Members by default (D-018), and a person can
    /// always see their own.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> List(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await membershipService.ListAsync(organisationId, cancellationToken);

        var callerIsOrganiser = OrganisationAuthorizationService.IsOrganiser(caller);

        return Ok(rows
            .Select(row => ToResponse(row, caller, callerIsOrganiser))
            .ToList());
    }

    /// <summary>
    /// Changes a member's role, or an Admin's financial permission. The rules
    /// behind each live in <see cref="MembershipService"/>.
    /// </summary>
    [HttpPatch("{membershipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid membershipId,
        UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var fieldsSent =
            (request.Role is null ? 0 : 1)
            + (request.CanManageFinances is null ? 0 : 1)
            + (request.InternalNotes is null ? 0 : 1);
        if (fieldsSent != 1)
        {
            ModelState.AddModelError(
                nameof(request.Role),
                "Send exactly one of role, canManageFinances, or internalNotes.");
            return ValidationProblem(ModelState);
        }

        MembershipChangeResult result;

        if (request.Role is not null)
        {
            if (!Enum.TryParse<MembershipRole>(request.Role, out var role))
            {
                ModelState.AddModelError(nameof(request.Role), "Unknown role.");
                return ValidationProblem(ModelState);
            }

            result = await membershipService.ChangeRoleAsync(
                caller,
                membershipId,
                role,
                cancellationToken);
        }
        else if (request.CanManageFinances is not null)
        {
            result = await membershipService.SetFinancialAccessAsync(
                caller,
                membershipId,
                request.CanManageFinances.Value,
                cancellationToken);
        }
        else
        {
            result = await membershipService.SetInternalNotesAsync(
                caller,
                membershipId,
                request.InternalNotes,
                cancellationToken);
        }

        return FromResult(result);
    }

    /// <summary>
    /// Updates the caller's own function and contact-sharing choice. Separate
    /// from the management route because it needs no authority over anyone —
    /// and because sharing your details is yours to decide, not an organiser's
    /// (D-018).
    /// </summary>
    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOwn(
        Guid organisationId,
        UpdateOwnMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (request.Function is null && request.SharesContactDetails is null)
        {
            ModelState.AddModelError(
                nameof(request.Function),
                "Send a function or a sharing choice.");
            return ValidationProblem(ModelState);
        }

        var result = await membershipService.UpdateOwnProfileAsync(
            caller,
            request.Function,
            request.SharesContactDetails,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Removes a member. The Owner is not removable — ownership is transferred
    /// first — and Admins reach ordinary Members only.
    /// </summary>
    [HttpDelete("{membershipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid organisationId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await membershipService.RemoveAsync(
            caller,
            membershipId,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Hands ownership to another member (D-014). Its own route rather than a
    /// role edit, because it is a deliberate act with different consequences.
    /// </summary>
    [HttpPost("transfer-ownership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOwnership(
        Guid organisationId,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await membershipService.TransferOwnershipAsync(
            caller,
            request.MembershipId,
            cancellationToken);

        return FromResult(result);
    }

    private async Task<Membership?> CallerAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return null;
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        return await authorization.FindMembershipAsync(
            organisationId,
            user.Id,
            cancellationToken);
    }

    private IActionResult FromResult(MembershipChangeResult result)
    {
        return result switch
        {
            MembershipChangeResult.Success => NoContent(),
            MembershipChangeResult.NotFound => NotFound(),
            MembershipChangeResult.Forbidden => Forbid(),
            _ => ValidationProblem("That change is not allowed."),
        };
    }

    /// <summary>
    /// Applies D-018 to one row. Name and function are shared with everyone in
    /// the organisation; contact details are not, unless the caller is an
    /// organiser, is looking at their own row, or the person has chosen to
    /// share them here. Internal notes never leave the organiser view — not
    /// even to the member they describe.
    /// </summary>
    private static MemberResponse ToResponse(
        OrganisationMember row,
        Membership caller,
        bool callerIsOrganiser)
    {
        var isYou = row.Membership.UserId == caller.UserId;
        var seesContactDetails =
            isYou || callerIsOrganiser || row.Membership.SharesContactDetails;

        return new MemberResponse(
            row.Membership.Id,
            row.Membership.UserId,
            row.DisplayName,
            row.Membership.Function,
            seesContactDetails ? row.Email : null,
            seesContactDetails ? row.PhoneNumber : null,
            row.Membership.Role.ToString(),
            row.Membership.HasFinancialAccess,
            row.Membership.SharesContactDetails,
            callerIsOrganiser ? row.Membership.InternalNotes : null,
            isYou,
            row.Membership.JoinedAtUtc);
    }
}
