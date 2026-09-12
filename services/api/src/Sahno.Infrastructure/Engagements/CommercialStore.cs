using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class CommercialStore(SahnoDbContext dbContext) : ICommercialStore
{
    public Task<EngagementCustomer?> FindCustomerAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return dbContext.EngagementCustomers.FirstOrDefaultAsync(
            customer => customer.EngagementId == engagementId,
            cancellationToken);
    }

    public Task<EngagementFinance?> FindFinanceAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return dbContext.EngagementFinance.FirstOrDefaultAsync(
            finance => finance.EngagementId == engagementId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PerformerPayment>> ListPaymentsAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PerformerPayments
            .AsNoTracking()
            .Where(payment => payment.EngagementId == engagementId)
            .OrderBy(payment => payment.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<PerformerPayment?> FindPaymentAsync(
        Guid engagementId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        return dbContext.PerformerPayments.FirstOrDefaultAsync(
            payment => payment.Id == paymentId && payment.EngagementId == engagementId,
            cancellationToken);
    }

    public void Add(EngagementCustomer customer) => dbContext.EngagementCustomers.Add(customer);

    public void Add(EngagementFinance finance) => dbContext.EngagementFinance.Add(finance);

    public void Add(PerformerPayment payment) => dbContext.PerformerPayments.Add(payment);

    public Task RemovePaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return dbContext.PerformerPayments
            .Where(payment => payment.Id == paymentId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> OutstandingForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var engagements = dbContext.Engagements
            .AsNoTracking()
            .Where(engagement => engagement.OrganisationId == organisationId)
            .Select(engagement => engagement.Id);

        var unpaid = await dbContext.PerformerPayments
            .AsNoTracking()
            .Where(payment => payment.PaidOn == null && engagements.Contains(payment.EngagementId))
            .GroupBy(payment => payment.EngagementId)
            .Select(group => new { EngagementId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        // The balance rule is a derived property, so the rows come back and
        // the test runs here rather than being taught to SQL.
        var finance = await dbContext.EngagementFinance
            .AsNoTracking()
            .Where(row => engagements.Contains(row.EngagementId))
            .ToListAsync(cancellationToken);

        var outstanding = unpaid.ToDictionary(row => row.EngagementId, row => row.Count);
        foreach (var row in finance.Where(row => row.IsCustomerBalanceOutstanding))
        {
            outstanding[row.EngagementId] = outstanding.GetValueOrDefault(row.EngagementId) + 1;
        }

        return outstanding;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
