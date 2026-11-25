using backend.Application.Authentication;
using backend.Application.Features.Accounts;
using backend.Application.Features.Media;
using backend.Application.Interfaces.Analysis;
using backend.Application.Interfaces.Authentication;
using backend.Application.Interfaces.Media;
using backend.Application.Interfaces.Storage;
using backend.Application.Interfaces.Users;
using backend.Infrastructure.Analysis;
using backend.Infrastructure.Authentication;
using backend.Infrastructure.Data;
using backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<JwtOptions>? configureJwt = null,
        Action<DatabaseOptions>? configureDatabase = null,
        Action<BlobStorageOptions>? configureBlobStorage = null)
    {
        if (configureJwt is not null)
        {
            services.Configure(configureJwt);
        }

        if (configureDatabase is not null)
        {
            services.Configure(configureDatabase);
        }

        if (configureBlobStorage is not null)
        {
            services.Configure(configureBlobStorage);
        }

        // Database services
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshSessionStore, RefreshSessionStore>();
        services.AddScoped<IMediaRepository, MediaRepository>();

        // Storage & Analysis
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IMediaAnalysisRepository, MediaRepository>();
        services.AddSingleton<IPlateRecognitionService, OnnxPlateRecognitionService>();
        services.AddSingleton<IVideoFrameExtractor, FfmpegVideoFrameExtractor>();

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
        services.AddScoped<UploadMedia>();

        return services;
    }
}
