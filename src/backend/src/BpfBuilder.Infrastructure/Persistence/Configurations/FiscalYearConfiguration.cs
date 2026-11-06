namespace BpfBuilder.Infrastructure.Persistence.Configurations;

internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears");
        builder.HasKey(y => y.Id);

        builder.Property(y => y.TotalTurnover).HasPrecision(14, 2);
        builder.HasIndex(y => new { y.OrganizationId, y.StartDate }).IsUnique();
    }
}
