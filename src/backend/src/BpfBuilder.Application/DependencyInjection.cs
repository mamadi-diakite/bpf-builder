using Microsoft.Extensions.DependencyInjection;
using BpfBuilder.Application.Auth;

namespace BpfBuilder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();

        return services;
    }
}
