using Microsoft.Extensions.DependencyInjection;
using BpfBuilder.Application.Auth;
using BpfBuilder.Application.FiscalYears;
using BpfBuilder.Application.Organizations;

namespace BpfBuilder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<FiscalYearService>();

        return services;
    }
}
