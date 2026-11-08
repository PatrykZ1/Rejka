using backend.Application.Interfaces.Authentication;
using backend.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<JwtOptions>? configureJwt = null)
    {
        if (configureJwt is not null)
        {
            services.Configure(configureJwt);
        }

        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 220_000;
        });
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();

        return services;
    }
}