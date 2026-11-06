namespace BpfBuilder.Infrastructure.Persistence;

public sealed class BpfBuilderDbContext(DbContextOptions<BpfBuilderDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    public DbSet<ActivityImport> ActivityImports => Set<ActivityImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BpfBuilderDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
