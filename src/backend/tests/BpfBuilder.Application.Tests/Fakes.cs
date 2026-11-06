namespace BpfBuilder.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public void Add(User user) => Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemoryOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Organizations { get; } = [];

    public List<FiscalYear> FiscalYears { get; } = [];

    public int SaveCount { get; private set; }

    public Task<Organization?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.SingleOrDefault(o => o.OwnerId == ownerId));

    public void Add(Organization organization) => Organizations.Add(organization);

    public Task<IReadOnlyList<FiscalYear>> ListFiscalYearsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FiscalYear>>(
            [.. FiscalYears.Where(y => y.OrganizationId == organizationId).OrderByDescending(y => y.StartDate)]);

    public Task<FiscalYear?> FindFiscalYearAsync(Guid organizationId, Guid fiscalYearId, CancellationToken cancellationToken) =>
        Task.FromResult(FiscalYears.SingleOrDefault(y => y.Id == fiscalYearId && y.OrganizationId == organizationId));

    public void AddFiscalYear(FiscalYear fiscalYear) => FiscalYears.Add(fiscalYear);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryActivityImportRepository : IActivityImportRepository
{
    public List<ActivityImport> Imports { get; } = [];

    public int SaveCount { get; private set; }

    public Task<ActivityImport?> FindByFiscalYearAsync(Guid fiscalYearId, CancellationToken cancellationToken) =>
        Task.FromResult(Imports.SingleOrDefault(i => i.FiscalYearId == fiscalYearId));

    public Task<bool> ExistsAsync(Guid fiscalYearId, CancellationToken cancellationToken) =>
        Task.FromResult(Imports.Any(i => i.FiscalYearId == fiscalYearId));

    public Task<IReadOnlyDictionary<Guid, DateTime>> ListImportDatesAsync(
        IReadOnlyCollection<Guid> fiscalYearIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, DateTime>>(
            Imports.Where(i => fiscalYearIds.Contains(i.FiscalYearId)).ToDictionary(i => i.FiscalYearId, i => i.ImportedAtUtc));

    public void Add(ActivityImport import) => Imports.Add(import);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Returns the given workbook, or null to stand for an unreadable file.</summary>
internal sealed class FakeWorkbookReader(ActivityWorkbook? workbook) : IActivityWorkbookReader
{
    public int ReadCount { get; private set; }

    public ActivityWorkbook? Read(Stream content)
    {
        ReadCount++;
        return workbook;
    }
}
