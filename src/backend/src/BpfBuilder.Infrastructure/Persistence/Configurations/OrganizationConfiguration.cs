namespace BpfBuilder.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(Organization.NameMaxLength).IsRequired();
        builder.Property(o => o.DeclarationNumber).HasMaxLength(Organization.DeclarationNumberLength);
        builder.Property(o => o.Siret).HasMaxLength(Organization.SiretLength);
        builder.Property(o => o.NafCode).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.LegalForm).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.AddressLine).HasMaxLength(Organization.AddressLineMaxLength);
        builder.Property(o => o.PostalCode).HasMaxLength(Organization.PostalCodeMaxLength);
        builder.Property(o => o.City).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.Phone).HasMaxLength(Organization.PhoneMaxLength);
        builder.Property(o => o.ContactEmail).HasMaxLength(Organization.EmailMaxLength);

        builder.Property(o => o.ManagerName).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.ManagerTitle).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.SignatoryName).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.SignatoryTitle).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.SignaturePlace).HasMaxLength(Organization.TextMaxLength);
        builder.Property(o => o.SignatoryEmail).HasMaxLength(Organization.EmailMaxLength);
        builder.Property(o => o.SignatoryPhone).HasMaxLength(Organization.PhoneMaxLength);

        builder.HasOne(o => o.Owner)
            .WithOne()
            .HasForeignKey<Organization>(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<FiscalYear>()
            .WithOne()
            .HasForeignKey(y => y.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
