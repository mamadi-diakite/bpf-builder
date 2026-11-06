using Microsoft.EntityFrameworkCore;
using BpfBuilder.Domain;
using BpfBuilder.Infrastructure.Security;
using BpfBuilder.Infrastructure.Seeding;

namespace BpfBuilder.Infrastructure.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly IdentityPasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_one_account_that_accepts_the_demo_password()
    {
        Assert.True(await SeedAsync());

        await using var db = _database.CreateContext();
        var user = await db.Users.SingleAsync();

        Assert.Equal(DemoDataSeeder.DemoEmail, user.Email);
        Assert.Equal(Now, user.CreatedAtUtc);
        Assert.True(_hasher.Verify(user.PasswordHash, DemoDataSeeder.DemoPassword));
    }

    [Fact]
    public async Task Seeds_a_fictional_organization_owned_by_the_demo_account()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var organization = await db.Organizations.Include(o => o.Owner).SingleAsync();

        Assert.Equal(DemoDataSeeder.DemoEmail, organization.Owner!.Email);
        Assert.Equal(DemoDataSeeder.DemoOrganizationName, organization.Name);
        Assert.Equal("99900000000011", organization.Siret);
        Assert.Equal("11999999999", organization.DeclarationNumber);
        Assert.Equal("85.59A", organization.NafCode);
        Assert.Equal("Camille Durand", organization.SignatoryName);
        Assert.EndsWith("@example.com", organization.ContactEmail);
    }

    [Fact]
    public async Task Second_run_adds_nothing()
    {
        await SeedAsync();

        Assert.False(await SeedAsync());

        await using var db = _database.CreateContext();
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.Organizations.CountAsync());
    }

    [Fact]
    public async Task Skips_a_database_that_already_has_users()
    {
        await using (var db = _database.CreateContext())
        {
            db.Users.Add(new User("someone@example.com", "Someone", "hash", Now));
            await db.SaveChangesAsync();
        }

        Assert.False(await SeedAsync());

        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Users.CountAsync());
        Assert.Equal(0, await check.Organizations.CountAsync());
    }

    private async Task<bool> SeedAsync()
    {
        await using var db = _database.CreateContext();

        return await new DemoDataSeeder(db, _hasher, new FixedTimeProvider(Now)).SeedAsync();
    }
}
