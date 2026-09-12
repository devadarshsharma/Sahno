using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// Customer, finance, and performer payments (Slice 11).
///
/// Two permission lines, drawn by the service, not here: <c>customer</c> is
/// for every organiser; <c>finance</c> and <c>payments</c> are for financial
/// access only (D-016). A member gets 403 from all of them — they are on the
/// event, so 404 would be a lie, and the refusal itself gives nothing away.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}")]
[Authorize]
public sealed class CommercialController(
    EnsureUserService ensureUserService,
    CommercialService commercialService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet("customer")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, customer) = await commercialService.GetCustomerAsync(
            caller,
            engagementId,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => Ok(ToResponse(customer!)),
            EngagementResult.Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpPut("customer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(
        Guid organisationId,
        Guid engagementId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await commercialService.UpdateCustomerAsync(
            caller,
            engagementId,
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.PrivateNotes,
            cancellationToken);

        return FromResult(result);
    }

    [HttpGet("finance")]
    [ProducesResponseType<FinanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinanceResponse>> GetFinance(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, finance) = await commercialService.GetFinanceAsync(
            caller,
            engagementId,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => Ok(ToResponse(finance!)),
            EngagementResult.Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpPut("finance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFinance(
        Guid organisationId,
        Guid engagementId,
        UpdateFinanceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await commercialService.UpdateFinanceAsync(
            caller,
            engagementId,
            request.QuotedFee,
            request.AgreedFee,
            request.DepositAmount,
            request.DepositReceivedOn,
            request.BalanceReceivedOn,
            request.Notes,
            cancellationToken);

        return FromResult(result, "An amount cannot be negative.");
    }

    [HttpGet("payments")]
    [ProducesResponseType<List<PerformerPaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PerformerPaymentResponse>>> ListPayments(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, payments) = await commercialService.ListPaymentsAsync(
            caller,
            engagementId,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => Ok(payments.Select(ToResponse).ToList()),
            EngagementResult.Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpPost("payments")]
    [ProducesResponseType<PerformerPaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerformerPaymentResponse>> AddPayment(
        Guid organisationId,
        Guid engagementId,
        CreatePerformerPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await commercialService.AddPaymentAsync(
            caller,
            engagementId,
            request.UserId,
            request.Amount,
            request.Notes,
            cancellationToken);

        if (result != EngagementResult.Success || created is null)
        {
            return result switch
            {
                EngagementResult.Forbidden => Forbid(),
                EngagementResult.NotFound => NotFound(),
                _ => ValidationProblem(
                    "A payment needs a positive amount and a performer who is on this event."),
            };
        }

        return CreatedAtAction(
            nameof(ListPayments),
            new { organisationId, engagementId },
            ToResponse(new PaymentRow(created, null)));
    }

    [HttpPut("payments/{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePayment(
        Guid organisationId,
        Guid engagementId,
        Guid paymentId,
        UpdatePerformerPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await commercialService.UpdatePaymentAsync(
            caller,
            engagementId,
            paymentId,
            request.Amount,
            request.Notes,
            request.PaidOn,
            cancellationToken);

        return FromResult(result, "A payment needs a positive amount.");
    }

    [HttpDelete("payments/{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePayment(
        Guid organisationId,
        Guid engagementId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await commercialService.RemovePaymentAsync(
            caller,
            engagementId,
            paymentId,
            cancellationToken);

        return FromResult(result);
    }

    private static CustomerResponse ToResponse(EngagementCustomer customer)
    {
        return new CustomerResponse(
            customer.Name,
            customer.ContactName,
            customer.Phone,
            customer.Email,
            customer.PrivateNotes,
            customer.UpdatedAtUtc);
    }

    private static FinanceResponse ToResponse(EngagementFinance finance)
    {
        return new FinanceResponse(
            finance.QuotedFee,
            finance.AgreedFee,
            finance.DepositAmount,
            finance.DepositReceivedOn,
            finance.Balance,
            finance.BalanceReceivedOn,
            finance.IsCustomerBalanceOutstanding,
            finance.Notes,
            finance.UpdatedAtUtc);
    }

    private static PerformerPaymentResponse ToResponse(PaymentRow row)
    {
        return new PerformerPaymentResponse(
            row.Payment.Id,
            row.Payment.UserId,
            row.DisplayName,
            row.Payment.Amount,
            row.Payment.Notes,
            row.Payment.PaidOn,
            row.Payment.IsPaid,
            row.Payment.CreatedAtUtc);
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

    private IActionResult FromResult(
        EngagementResult result,
        string invalidMessage = "That change is not allowed.")
    {
        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem(invalidMessage),
        };
    }
}
