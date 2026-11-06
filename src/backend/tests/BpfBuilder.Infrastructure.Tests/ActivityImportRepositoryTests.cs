namespace BpfBuilder.Infrastructure.Tests;

public sealed class ActivityImportRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly FiscalYear _year2025;
    private readonly FiscalYear _year2024;

    public ActivityImportRepositoryTests()
    {
        var owner = new User("alex.martin@example.com", "Alex Martin", "hash", Now);
        var organization = new Organization(owner.Id, "Northwind Training", Now);
        _year2025 = FiscalYear.CalendarYear(organization.Id, 2025, 0m, Now);
        _year2024 = FiscalYear.CalendarYear(organization.Id, 2024, 0m, Now);
        using var db = _database.CreateContext();
        db.Users.Add(owner);
        db.Organizations.Add(organization);
        db.FiscalYears.AddRange(_year2025, _year2024);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_an_import_with_all_its_rows()
    {
        var session = new ImportedSession(
            "S-1", "Web developer certificate", new DateOnly(2025, 3, 3), new DateOnly(2025, 6, 27), 399.5m, "Direct", "Partial", "RNCP", "5", "326");
        await using (var db = _database.CreateContext())
        {
            var repository = new ActivityImportRepository(db);
            repository.Add(new ActivityImport(
                _year2025.Id,
                "activity.xlsx",
                Now,
                [session],
                [new("Student 01", "S-1", "Employee", 21m), new("Student 01", "S-1", "Employee", 21m)],
                [new("Trainer 01", "S-1", "Internal", 210m)],
                [new("11", 600.25m, new DateOnly(2025, 12, 15), null, "Course material sold")],
                [new("Other costs", 2310m, null)]));
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var checkRepository = new ActivityImportRepository(check);
        var import = (await checkRepository.FindByFiscalYearAsync(_year2025.Id, default))!;

        Assert.Equal("activity.xlsx", import.FileName);
        Assert.Equal(DateTimeKind.Utc, import.ImportedAtUtc.Kind);
        Assert.Equal(session, Assert.Single(import.Sessions));
        Assert.Equal(2, import.Learners.Count);
        Assert.Equal(210m, Assert.Single(import.Trainers).HoursDelivered);
        Assert.Equal(new ImportedRevenue("11", 600.25m, new DateOnly(2025, 12, 15), null, "Course material sold"), Assert.Single(import.Revenue));
        Assert.Equal(new ImportedCost("Other costs", 2310m, null), Assert.Single(import.Costs));
        Assert.Null(await checkRepository.FindByFiscalYearAsync(_year2024.Id, default));
        Assert.True(await checkRepository.ExistsAsync(_year2025.Id, default));
        Assert.False(await checkRepository.ExistsAsync(_year2024.Id, default));
        Assert.Equal(
            new Dictionary<Guid, DateTime> { [_year2025.Id] = Now },
            await checkRepository.ListImportDatesAsync([_year2025.Id, _year2024.Id], default));
    }

    [Fact]
    public async Task A_fiscal_year_has_one_import_at_most_and_loses_it_when_deleted()
    {
        await using (var db = _database.CreateContext())
        {
            db.ActivityImports.Add(Empty(_year2025.Id));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            db.ActivityImports.Add(Empty(_year2025.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using (var db = _database.CreateContext())
        {
            db.FiscalYears.Remove(await db.FiscalYears.SingleAsync(y => y.Id == _year2025.Id));
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        Assert.Equal(0, await check.ActivityImports.CountAsync());
    }

    private static ActivityImport Empty(Guid fiscalYearId) => new(fiscalYearId, "activity.xlsx", Now, [], [], [], [], []);
}
