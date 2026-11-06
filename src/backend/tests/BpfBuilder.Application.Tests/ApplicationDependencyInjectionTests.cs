using Microsoft.Extensions.DependencyInjection;
using BpfBuilder.Application.Auth;
using BpfBuilder.Application.FiscalYears;
using BpfBuilder.Application.Organizations;
using BpfBuilder.Application.Security;
using BpfBuilder.Application.Templates;
using BpfBuilder.Application.Users;

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
            .AddSingleton<IActivityWorkbookWriter, NoWorkbookWriter>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<OrganizationService>());
        Assert.NotNull(scope.ServiceProvider.GetService<FiscalYearService>());
        Assert.NotNull(scope.ServiceProvider.GetService<ActivityTemplateService>());
    }

    private sealed class NoWorkbookWriter : IActivityWorkbookWriter
    {
        public void Write(Stream output, bool withSample) => throw new NotSupportedException();
    }
}
