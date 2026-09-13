using Microsoft.EntityFrameworkCore;
using Sahno.Application.Organisations;
using Sahno.Domain.Organisations;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Organisations;

public sealed class CustomerStore(SahnoDbContext dbContext) : ICustomerStore
{
    public async Task<IReadOnlyList<Customer>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.OrganisationId == organisationId)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Customer?> FindAsync(
        Guid organisationId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.Id == customerId && customer.OrganisationId == organisationId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerEngagement>> ListEngagementsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngagementCustomers
            .AsNoTracking()
            .Where(link => link.CustomerId == customerId)
            .Join(
                dbContext.Engagements,
                link => link.EngagementId,
                engagement => engagement.Id,
                (_, engagement) => engagement)
            .OrderByDescending(engagement => engagement.StartDate)
            .ThenByDescending(engagement => engagement.CreatedAtUtc)
            .Select(engagement => new CustomerEngagement(
                engagement.Id,
                engagement.Title,
                engagement.Status.ToString(),
                engagement.StartDate,
                engagement.Venue))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> BookingCountsAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.EngagementCustomers
            .AsNoTracking()
            .Where(link => link.CustomerId != null)
            .Join(
                dbContext.Customers.Where(customer => customer.OrganisationId == organisationId),
                link => link.CustomerId,
                customer => customer.Id,
                (link, _) => link.CustomerId!.Value)
            .GroupBy(id => id)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Key, row => row.Count);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Add(customer);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
