using backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace backend.API.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController(IOptions<DatabaseOptions> dbOptions) : ControllerBase
{
    private readonly IOptions<DatabaseOptions> _dbOptions = dbOptions;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dbConnected = false;
        string? error = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(_dbOptions.Value.ConnectionString))
            {
                await using var conn = new SqlConnection(_dbOptions.Value.ConnectionString);
                await conn.OpenAsync(cancellationToken);
                dbConnected = true;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return Ok(new
        {
            status = "Healthy",
            database = dbConnected ? "Connected" : "Disconnected",
            databaseError = error,
            serverTimeUtc = DateTime.UtcNow
        });
    }
}
