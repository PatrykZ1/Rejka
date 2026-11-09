using backend.Infrastructure.Data;

namespace backend.API;

public sealed class DatabaseMigrationBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationBackgroundService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<DatabaseMigrationBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database schema.");
        }
    }
}
