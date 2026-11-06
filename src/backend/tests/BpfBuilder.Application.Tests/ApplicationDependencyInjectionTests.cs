namespace BpfBuilder.Application.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void Registers_the_application_services()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IUserRepository, InMemoryUserRepository>()
            .AddSingleton<IPasswordHasher, FakePasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeTokenIssuer>()
            .AddSingleton<IOrganizationRepository, InMemoryOrganizationRepository>()
            .AddSingleton<IActivityWorkbookWriter, NoWorkbookWriter>()
            .AddSingleton<IActivityImportRepository, InMemoryActivityImportRepository>()
            .AddSingleton<IActivityWorkbookReader>(new FakeWorkbookReader(null));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<OrganizationService>());
        Assert.NotNull(scope.ServiceProvider.GetService<FiscalYearService>());
        Assert.NotNull(scope.ServiceProvider.GetService<ActivityTemplateService>());
        Assert.NotNull(scope.ServiceProvider.GetService<ActivityImportService>());
    }

    private sealed class NoWorkbookWriter : IActivityWorkbookWriter
    {
        public void Write(Stream output, bool withSample) => throw new NotSupportedException();
    }
}
