using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class EngagementCustomerConfiguration
    : IEntityTypeConfiguration<EngagementCustomer>
{
    public void Configure(EntityTypeBuilder<EngagementCustomer> builder)
    {
        builder.ToTable("engagement_customers");

        // One per engagement: the engagement id is the key.
        builder.HasKey(customer => customer.EngagementId);

        builder.Property(customer => customer.EngagementId)
            .HasColumnName("engagement_id")
            .ValueGeneratedNever();

        builder.Property(customer => customer.Name)
            .HasColumnName("name")
            .HasMaxLength(EngagementCustomer.NameMaxLength);

        builder.Property(customer => customer.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(EngagementCustomer.ContactMaxLength);

        builder.Property(customer => customer.Phone)
            .HasColumnName("phone")
            .HasMaxLength(EngagementCustomer.PhoneMaxLength);

        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(EngagementCustomer.EmailMaxLength);

        builder.Property(customer => customer.PrivateNotes)
            .HasColumnName("private_notes")
            .HasMaxLength(EngagementCustomer.NotesMaxLength);

        builder.Property(customer => customer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}

public sealed class EngagementFinanceConfiguration
    : IEntityTypeConfiguration<EngagementFinance>
{
    public void Configure(EntityTypeBuilder<EngagementFinance> builder)
    {
        builder.ToTable("engagement_finance");

        builder.HasKey(finance => finance.EngagementId);

        builder.Property(finance => finance.EngagementId)
            .HasColumnName("engagement_id")
            .ValueGeneratedNever();

        // numeric(12,2): up to 9,999,999,999.99 — nobody books a qawwali
        // party for more, and it keeps arithmetic exact.
        builder.Property(finance => finance.QuotedFee)
            .HasColumnName("quoted_fee")
            .HasPrecision(12, 2);

        builder.Property(finance => finance.AgreedFee)
            .HasColumnName("agreed_fee")
            .HasPrecision(12, 2);

        builder.Property(finance => finance.DepositAmount)
            .HasColumnName("deposit_amount")
            .HasPrecision(12, 2);

        builder.Property(finance => finance.DepositReceivedOn)
            .HasColumnName("deposit_received_on");

        builder.Property(finance => finance.BalanceReceivedOn)
            .HasColumnName("balance_received_on");

        builder.Property(finance => finance.Notes)
            .HasColumnName("notes")
            .HasMaxLength(EngagementFinance.NotesMaxLength);

        builder.Property(finance => finance.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(finance => finance.Balance);
        builder.Ignore(finance => finance.IsCustomerBalanceOutstanding);
    }
}

public sealed class PerformerPaymentConfiguration
    : IEntityTypeConfiguration<PerformerPayment>
{
    public void Configure(EntityTypeBuilder<PerformerPayment> builder)
    {
        builder.ToTable("performer_payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(payment => payment.EngagementId)
            .HasColumnName("engagement_id")
            .IsRequired();

        builder.Property(payment => payment.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(payment => payment.Notes)
            .HasColumnName("notes")
            .HasMaxLength(PerformerPayment.NotesMaxLength);

        builder.Property(payment => payment.PaidOn)
            .HasColumnName("paid_on");

        builder.Property(payment => payment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Ignore(payment => payment.IsPaid);

        builder.HasIndex(payment => payment.EngagementId);
    }
}
