using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>A performer payment with the performer's name resolved.</summary>
public sealed record PaymentRow(PerformerPayment Payment, string? DisplayName);

/// <summary>
/// Customer details, money, and performer payments (Slice 11, D-008, D-016,
/// D-022).
///
/// Two permission lines run through this service and they are not the same
/// line. The customer — who the booking is for, and the organiser's private
/// notes — is for every organiser. The money — fees, deposits, what each
/// performer is owed — is for the Owner and only those Admins the Owner has
/// explicitly granted financial access. Members reach neither, ever.
///
/// D-008's boundary holds throughout: these are operational facts an
/// organiser needs to hand, not a ledger. No tax, no invoicing, no processing.
/// </summary>
public sealed class CommercialService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    ICommercialStore commercial,
    ICustomerStore customers,
    IMembershipStore memberships)
{
    // ---- Customer: every organiser --------------------------------------

    /// <summary>
    /// The booking's customer link with the customer record resolved. The
    /// link is present even when nobody has been chosen; the customer is null
    /// then, or when the linked record has since been deleted.
    /// </summary>
    public async Task<(EngagementResult Result, EngagementCustomer? Link, CustomerRow? Customer)> GetCustomerAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (EngagementResult.Forbidden, null, null);
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, null, null);
        }

        // Absent means never filled in, which the caller sees as an empty
        // link rather than a missing one.
        var link = await commercial.FindCustomerAsync(engagementId, cancellationToken)
            ?? EngagementCustomer.Empty(engagementId);

        CustomerRow? row = null;
        if (link.CustomerId is { } customerId)
        {
            var customer = await customers.FindAsync(actor.OrganisationId, customerId, cancellationToken);
            if (customer is not null)
            {
                // "They have had us four times" belongs next to the name here too.
                var history = await customers.ListEngagementsAsync(customerId, cancellationToken);
                row = new CustomerRow(customer, history.Count);
            }
        }

        return (EngagementResult.Success, link, row);
    }

    /// <summary>
    /// Points the booking at a customer from the organisation's directory (or
    /// at nobody) and records the organiser's notes about this booking.
    /// A customer from another organisation is refused as not found.
    /// </summary>
    public async Task<EngagementResult> UpdateCustomerAsync(
        Membership actor,
        Guid engagementId,
        Guid? customerId,
        string? privateNotes,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        if (customerId is { } id
            && await customers.FindAsync(actor.OrganisationId, id, cancellationToken) is null)
        {
            return EngagementResult.NotFound;
        }

        var link = await commercial.FindCustomerAsync(engagementId, cancellationToken);
        if (link is null)
        {
            link = EngagementCustomer.Empty(engagementId);
            commercial.Add(link);
        }

        link.Update(customerId, privateNotes);
        await commercial.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    // ---- Finance: financial access only (D-016) ------------------------

    public async Task<(EngagementResult Result, EngagementFinance? Finance)> GetFinanceAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return (EngagementResult.Forbidden, null);
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, null);
        }

        var finance = await commercial.FindFinanceAsync(engagementId, cancellationToken)
            ?? EngagementFinance.Empty(engagementId);

        return (EngagementResult.Success, finance);
    }

    public async Task<EngagementResult> UpdateFinanceAsync(
        Membership actor,
        Guid engagementId,
        decimal? quotedFee,
        decimal? agreedFee,
        decimal? depositAmount,
        DateOnly? depositReceivedOn,
        DateOnly? balanceReceivedOn,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var finance = await commercial.FindFinanceAsync(engagementId, cancellationToken);
        if (finance is null)
        {
            finance = EngagementFinance.Empty(engagementId);
            commercial.Add(finance);
        }

        try
        {
            finance.Update(
                quotedFee,
                agreedFee,
                depositAmount,
                depositReceivedOn,
                balanceReceivedOn,
                notes);
        }
        catch (ArgumentException)
        {
            return EngagementResult.Invalid;
        }

        await commercial.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    // ---- Performer payments: financial access only ---------------------

    public async Task<(EngagementResult Result, IReadOnlyList<PaymentRow> Payments)> ListPaymentsAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return (EngagementResult.Forbidden, []);
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, []);
        }

        var payments = await commercial.ListPaymentsAsync(engagementId, cancellationToken);
        var directory = await memberships.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);
        var names = directory.ToDictionary(
            row => row.Membership.UserId,
            row => row.DisplayName);

        return (
            EngagementResult.Success,
            payments
                .Select(payment => new PaymentRow(payment, names.GetValueOrDefault(payment.UserId)))
                .ToList());
    }

    /// <summary>
    /// Records what a performer is owed. They must be on the lineup: a payment
    /// to somebody who was never part of the event is a data-entry mistake,
    /// and refusing it is kinder than storing it.
    /// </summary>
    public async Task<(EngagementResult Result, PerformerPayment? Created)> AddPaymentAsync(
        Membership actor,
        Guid engagementId,
        Guid userId,
        decimal amount,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return (EngagementResult.Forbidden, null);
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, null);
        }

        var participant = await participants.FindAsync(engagementId, userId, cancellationToken);
        if (participant is null)
        {
            return (EngagementResult.Invalid, null);
        }

        PerformerPayment payment;
        try
        {
            payment = PerformerPayment.Create(engagementId, userId, amount, notes);
        }
        catch (ArgumentException)
        {
            return (EngagementResult.Invalid, null);
        }

        commercial.Add(payment);
        await commercial.SaveAsync(cancellationToken);
        return (EngagementResult.Success, payment);
    }

    public async Task<EngagementResult> UpdatePaymentAsync(
        Membership actor,
        Guid engagementId,
        Guid paymentId,
        decimal amount,
        string? notes,
        DateOnly? paidOn,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var payment = await commercial.FindPaymentAsync(engagementId, paymentId, cancellationToken);
        if (payment is null)
        {
            return EngagementResult.NotFound;
        }

        try
        {
            payment.Update(amount, notes);
        }
        catch (ArgumentException)
        {
            return EngagementResult.Invalid;
        }

        payment.SetPaid(paidOn);
        await commercial.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> RemovePaymentAsync(
        Membership actor,
        Guid engagementId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        if (!actor.HasFinancialAccess)
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var payment = await commercial.FindPaymentAsync(engagementId, paymentId, cancellationToken);
        if (payment is null)
        {
            return EngagementResult.NotFound;
        }

        await commercial.RemovePaymentAsync(paymentId, cancellationToken);
        return EngagementResult.Success;
    }

    private async Task<bool> ExistsAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);

        return engagement is not null;
    }
}
