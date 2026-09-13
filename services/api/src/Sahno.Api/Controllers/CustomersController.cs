using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Organisations;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// The organisation's customer directory. Organisers only; a Member gets 403
/// from every route here (D-022).
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/customers")]
[Authorize]
public sealed class CustomersController(
    EnsureUserService ensureUserService,
    CustomerService customerService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> List(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, rows) = await customerService.ListAsync(caller, cancellationToken);

        return result switch
        {
            CustomerResult.Success => Ok(
                rows.Select(row => ToResponse(row.Customer, row.BookingCount)).ToList()),
            CustomerResult.Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType<CustomerDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> Get(
        Guid organisationId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, customer, history) = await customerService.GetAsync(
            caller,
            customerId,
            cancellationToken);

        return result switch
        {
            CustomerResult.Success => Ok(new CustomerDetailResponse(
                ToResponse(customer!, history.Count),
                history
                    .Select(booking => new CustomerBookingResponse(
                        booking.EngagementId,
                        booking.Title,
                        booking.Status,
                        booking.StartDate,
                        booking.Venue))
                    .ToList())),
            CustomerResult.Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Create(
        Guid organisationId,
        SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await customerService.CreateAsync(
            caller,
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Notes,
            cancellationToken);

        return result switch
        {
            CustomerResult.Success => StatusCode(
                StatusCodes.Status201Created,
                ToResponse(created!, bookingCount: 0)),
            CustomerResult.Forbidden => Forbid(),
            CustomerResult.Invalid => ValidationProblem("A customer needs a name."),
            _ => NotFound(),
        };
    }

    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid customerId,
        SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await customerService.UpdateAsync(
            caller,
            customerId,
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Notes,
            cancellationToken);

        return result switch
        {
            CustomerResult.Success => NoContent(),
            CustomerResult.Forbidden => Forbid(),
            CustomerResult.Invalid => ValidationProblem("A customer needs a name."),
            _ => NotFound(),
        };
    }

    internal static CustomerResponse ToResponse(Customer customer, int bookingCount)
    {
        return new CustomerResponse(
            customer.Id,
            customer.Name,
            customer.ContactName,
            customer.Phone,
            customer.Email,
            customer.Notes,
            bookingCount,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
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
}
