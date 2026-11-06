namespace BpfBuilder.Api.IntegrationTests;

public sealed class DatabaseStartupTests(BpfBuilderApiFactory factory) : IClassFixture<BpfBuilderApiFactory>
{
    [Fact]
    public async Task Startup_applies_all_migrations()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BpfBuilderDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }
}
