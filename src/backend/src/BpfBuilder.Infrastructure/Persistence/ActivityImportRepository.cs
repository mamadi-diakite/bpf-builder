namespace BpfBuilder.Infrastructure.Persistence;

public sealed class ActivityImportRepository(BpfBuilderDbContext db) : IActivityImportRepository
{
    public Task<ActivityImport?> FindByFiscalYearAsync(Guid fiscalYearId, CancellationToken cancellationToken) =>
        db.ActivityImports.SingleOrDefaultAsync(i => i.FiscalYearId == fiscalYearId, cancellationToken);

    public Task<bool> ExistsAsync(Guid fiscalYearId, CancellationToken cancellationToken) =>
        db.ActivityImports.AnyAsync(i => i.FiscalYearId == fiscalYearId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> ListImportDatesAsync(
        IReadOnlyCollection<Guid> fiscalYearIds,
        CancellationToken cancellationToken) =>
        await db.ActivityImports
            .Where(i => fiscalYearIds.Contains(i.FiscalYearId))
            .Select(i => new { i.FiscalYearId, i.ImportedAtUtc })
            .ToDictionaryAsync(i => i.FiscalYearId, i => i.ImportedAtUtc, cancellationToken);

    public void Add(ActivityImport import) => db.ActivityImports.Add(import);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
