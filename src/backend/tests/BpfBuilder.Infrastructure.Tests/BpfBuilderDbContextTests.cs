using BpfBuilder.Domain;
using Microsoft.EntityFrameworkCore;

namespace BpfBuilder.Infrastructure.Tests;

public sealed class BpfBuilderDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_an_organization_profile()
    {
        var owner = await AddUserAsync("owner@example.com");
        var organization = new Organization(owner.Id, "Northwind Training", Now);
        organization.UpdateIdentification(
            "Northwind Training", "11999999999", "99900000000011", "85.59A", "SAS",
            "12 Example Street", "75001", "Paris", true, "01 00 00 00 00", "contact@example.com", Now);
        organization.UpdateSignatory("Alex Morgan", "Director", "Sam Lee", "Manager", "Paris", "sam@example.com", null, Now);

        await using (var db = _database.CreateContext())
        {
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Organizations.Include(o => o.Owner).SingleAsync();

            Assert.Equal("99900000000011", loaded.Siret);
            Assert.Equal("85.59A", loaded.NafCode);
            Assert.True(loaded.PublishAddress);
            Assert.Equal("Sam Lee", loaded.SignatoryName);
            Assert.Null(loaded.SignatoryPhone);
            Assert.Equal(owner.Email, loaded.Owner!.Email);
            Assert.Equal(DateTimeKind.Utc, loaded.UpdatedAtUtc.Kind);
        }
    }

    [Fact]
    public async Task Saves_and_reads_fiscal_years()
    {
        var organization = await AddOrganizationAsync();

        await using (var db = _database.CreateContext())
        {
            db.FiscalYears.Add(FiscalYear.CalendarYear(organization.Id, 2025, 180_000.50m, Now));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var year = await db.FiscalYears.SingleAsync();

            Assert.Equal(new DateOnly(2025, 1, 1), year.StartDate);
            Assert.Equal(new DateOnly(2025, 12, 31), year.EndDate);
            Assert.Equal(180_000.50m, year.TotalTurnover);
        }
    }

    [Fact]
    public async Task Two_fiscal_years_cannot_start_on_the_same_day()
    {
        var organization = await AddOrganizationAsync();

        await using var db = _database.CreateContext();
        db.FiscalYears.Add(FiscalYear.CalendarYear(organization.Id, 2025, 0m, Now));
        db.FiscalYears.Add(FiscalYear.CalendarYear(organization.Id, 2025, 0m, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_user_owns_one_organization_at_most()
    {
        var organization = await AddOrganizationAsync();

        await using var db = _database.CreateContext();
        db.Organizations.Add(new Organization(organization.OwnerId, "Second", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_an_organization_deletes_its_fiscal_years()
    {
        var organization = await AddOrganizationAsync();
        await using (var db = _database.CreateContext())
        {
            db.FiscalYears.Add(FiscalYear.CalendarYear(organization.Id, 2025, 0m, Now));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            db.Organizations.Remove(await db.Organizations.SingleAsync());
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        Assert.Equal(0, await check.FiscalYears.CountAsync());
    }

    [Fact]
    public async Task Email_is_unique()
    {
        await AddUserAsync("owner@example.com");

        await Assert.ThrowsAsync<DbUpdateException>(() => AddUserAsync("Owner@Example.com"));
    }

    private async Task<User> AddUserAsync(string email)
    {
        var user = new User(email, "Owner", "hash", Now);
        await using var db = _database.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<Organization> AddOrganizationAsync()
    {
        var owner = await AddUserAsync("owner@example.com");
        var organization = new Organization(owner.Id, "Northwind Training", Now);
        await using var db = _database.CreateContext();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        return organization;
    }
}
