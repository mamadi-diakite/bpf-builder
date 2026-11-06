namespace BpfBuilder.Application.Organizations;

public interface IOrganizationRepository
{
    Task<Organization?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(Organization organization);

    /// <returns>The organization's fiscal years, latest first.</returns>
    Task<IReadOnlyList<FiscalYear>> ListFiscalYearsAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<FiscalYear?> FindFiscalYearAsync(Guid organizationId, Guid fiscalYearId, CancellationToken cancellationToken);

    void AddFiscalYear(FiscalYear fiscalYear);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
