using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahno.Domain.Organisations;

namespace Sahno.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(customer => customer.OrganisationId)
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(customer => customer.Name)
            .HasColumnName("name")
            .HasMaxLength(Customer.NameMaxLength)
            .IsRequired();

        builder.Property(customer => customer.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(Customer.ContactMaxLength);

        builder.Property(customer => customer.Phone)
            .HasColumnName("phone")
            .HasMaxLength(Customer.PhoneMaxLength);

        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(Customer.EmailMaxLength);

        builder.Property(customer => customer.Notes)
            .HasColumnName("notes")
            .HasMaxLength(Customer.NotesMaxLength);

        builder.Property(customer => customer.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(customer => customer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(customer => new { customer.OrganisationId, customer.Name });
    }
}
