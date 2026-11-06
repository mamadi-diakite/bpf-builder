namespace BpfBuilder.Infrastructure.Tests;

public sealed class OrganizationRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner = new("alex.martin@example.com", "Alex Martin", "hash", Now);
    private readonly User _otherOwner = new("lou.perrin@example.com", "Lou Perrin", "hash", Now);

    public OrganizationRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Users.AddRange(_owner, _otherOwner);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Adds_and_finds_an_organization_by_owner()
    {
        var organization = new Organization(_owner.Id, "Northwind Training", Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new OrganizationRepository(db);
            repository.Add(organization);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var checkRepository = new OrganizationRepository(check);
        Assert.Equal(organization.Id, (await checkRepository.FindByOwnerAsync(_owner.Id, default))?.Id);
        Assert.Null(await checkRepository.FindByOwnerAsync(_otherOwner.Id, default));
    }

    [Fact]
    public async Task Lists_and_finds_only_the_organization_fiscal_years()
    {
        var organization = new Organization(_owner.Id, "Northwind Training", Now);
        var other = new Organization(_otherOwner.Id, "Other Training", Now);
        var older = FiscalYear.CalendarYear(organization.Id, 2024, 10m, Now);
        var newer = FiscalYear.CalendarYear(organization.Id, 2025, 20m, Now);
        var foreign = FiscalYear.CalendarYear(other.Id, 2025, 30m, Now);
        await using (var db = _database.CreateContext())
        {
            db.Organizations.AddRange(organization, other);
            var repository = new OrganizationRepository(db);
            repository.AddFiscalYear(older);
            repository.AddFiscalYear(newer);
            repository.AddFiscalYear(foreign);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var checkRepository = new OrganizationRepository(check);

        var years = await checkRepository.ListFiscalYearsAsync(organization.Id, default);
        Assert.Equal([newer.Id, older.Id], years.Select(y => y.Id));
        Assert.Equal(older.Id, (await checkRepository.FindFiscalYearAsync(organization.Id, older.Id, default))?.Id);
        Assert.Null(await checkRepository.FindFiscalYearAsync(organization.Id, foreign.Id, default));
    }
}
