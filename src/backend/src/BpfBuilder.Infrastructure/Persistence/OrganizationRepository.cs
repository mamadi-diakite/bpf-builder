using Microsoft.EntityFrameworkCore;
using BpfBuilder.Application.Organizations;
using BpfBuilder.Domain;

namespace BpfBuilder.Infrastructure.Persistence;

public sealed class OrganizationRepository(BpfBuilderDbContext db) : IOrganizationRepository
{
    public Task<Organization?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.Organizations.SingleOrDefaultAsync(o => o.OwnerId == ownerId, cancellationToken);

    public void Add(Organization organization) => db.Organizations.Add(organization);

    public async Task<IReadOnlyList<FiscalYear>> ListFiscalYearsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await db.FiscalYears
            .Where(y => y.OrganizationId == organizationId)
            .OrderByDescending(y => y.StartDate)
            .ToListAsync(cancellationToken);

    public Task<FiscalYear?> FindFiscalYearAsync(Guid organizationId, Guid fiscalYearId, CancellationToken cancellationToken) =>
        db.FiscalYears.SingleOrDefaultAsync(y => y.Id == fiscalYearId && y.OrganizationId == organizationId, cancellationToken);

    public void AddFiscalYear(FiscalYear fiscalYear) => db.FiscalYears.Add(fiscalYear);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
