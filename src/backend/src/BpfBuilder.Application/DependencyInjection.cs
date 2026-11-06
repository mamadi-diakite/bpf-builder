namespace BpfBuilder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<FiscalYearService>();
        services.AddScoped<ActivityTemplateService>();
        services.AddScoped<ActivityImportService>();

        return services;
    }
}
