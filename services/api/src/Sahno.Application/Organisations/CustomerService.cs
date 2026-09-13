using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

public enum CustomerResult
{
    Success,
    NotFound,
    Forbidden,
    Invalid,
}

/// <summary>A directory row: the customer and how often they have booked.</summary>
public sealed record CustomerRow(Customer Customer, int BookingCount);

/// <summary>
/// The organisation's customer directory: the families, venues and companies
/// who book the group, kept across bookings so a returning customer is picked
/// rather than retyped, and so an organiser can see "they have had us four
/// times" at a glance.
///
/// Organisers only, all of it (D-022). Members have no route that reaches a
/// customer.
/// </summary>
public sealed class CustomerService(ICustomerStore customers)
{
    public async Task<(CustomerResult Result, IReadOnlyList<CustomerRow> Rows)> ListAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (CustomerResult.Forbidden, []);
        }

        var directory = await customers.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);
        var counts = await customers.BookingCountsAsync(actor.OrganisationId, cancellationToken);

        return (
            CustomerResult.Success,
            directory
                .Select(customer => new CustomerRow(customer, counts.GetValueOrDefault(customer.Id)))
                .ToList());
    }

    public async Task<(CustomerResult Result, Customer? Customer, IReadOnlyList<CustomerEngagement> History)> GetAsync(
        Membership actor,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (CustomerResult.Forbidden, null, []);
        }

        var customer = await customers.FindAsync(actor.OrganisationId, customerId, cancellationToken);
        if (customer is null)
        {
            return (CustomerResult.NotFound, null, []);
        }

        var history = await customers.ListEngagementsAsync(customerId, cancellationToken);
        return (CustomerResult.Success, customer, history);
    }

    public async Task<(CustomerResult Result, Customer? Created)> CreateAsync(
        Membership actor,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (CustomerResult.Forbidden, null);
        }

        Customer customer;
        try
        {
            customer = Customer.Create(
                actor.OrganisationId,
                name,
                contactName,
                phone,
                email,
                notes,
                actor.UserId);
        }
        catch (ArgumentException)
        {
            return (CustomerResult.Invalid, null);
        }

        await customers.AddAsync(customer, cancellationToken);
        return (CustomerResult.Success, customer);
    }

    public async Task<CustomerResult> UpdateAsync(
        Membership actor,
        Guid customerId,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return CustomerResult.Forbidden;
        }

        var customer = await customers.FindAsync(actor.OrganisationId, customerId, cancellationToken);
        if (customer is null)
        {
            return CustomerResult.NotFound;
        }

        try
        {
            customer.Update(name, contactName, phone, email, notes);
        }
        catch (ArgumentException)
        {
            return CustomerResult.Invalid;
        }

        await customers.SaveAsync(cancellationToken);
        return CustomerResult.Success;
    }
}
