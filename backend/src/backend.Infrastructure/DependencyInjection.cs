using backend.Application.Authentication;
using backend.Application.Features.Accounts;
using backend.Application.Interfaces.Authentication;
using backend.Application.Interfaces.Users;
using backend.Infrastructure.Authentication;
using backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<JwtOptions>? configureJwt = null,
        Action<DatabaseOptions>? configureDatabase = null)
    {
        if (configureJwt is not null)
        {
            services.Configure(configureJwt);
        }

        if (configureDatabase is not null)
        {
            services.Configure(configureDatabase);
        }

        // Database services
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshSessionStore, RefreshSessionStore>();

        // Authentication & hashing
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 220_000;
        });
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();

        // Application Use Cases
        services.AddSingleton(new AuthenticationTokenLifetime(
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(14)));

        services.AddScoped<RegisterAccount>();
        services.AddScoped<Login>();
        services.AddScoped<Logout>();
        services.AddScoped<RefreshSession>();

        return services;
    }
}
