using Microsoft.Extensions.DependencyInjection;
using BpfBuilder.Application.Auth;
using BpfBuilder.Application.FiscalYears;
using BpfBuilder.Application.Organizations;
using BpfBuilder.Application.Templates;

namespace BpfBuilder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<FiscalYearService>();
        services.AddScoped<ActivityTemplateService>();

        return services;
    }
}
