namespace BpfBuilder.Application.Imports;

public interface IActivityImportRepository
{
    Task<ActivityImport?> FindByFiscalYearAsync(Guid fiscalYearId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid fiscalYearId, CancellationToken cancellationToken);

    /// <returns>Import date by fiscal year id, for the given years that have imported data.</returns>
    Task<IReadOnlyDictionary<Guid, DateTime>> ListImportDatesAsync(
        IReadOnlyCollection<Guid> fiscalYearIds,
        CancellationToken cancellationToken);

    void Add(ActivityImport import);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
