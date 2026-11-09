using backend.API;
using backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? string.Empty;

builder.Services.AddInfrastructure(
    configureJwt: options =>
    {
        options.Issuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "RejkaApi";
        options.Audience = builder.Configuration["Authentication:Jwt:Audience"] ?? "RejkaApp";
        options.SigningKey = builder.Configuration["Authentication:Jwt:SigningKey"] ?? "dGhpc19pc19hX3Zlcnlfc3Ryb25nX3NlY3JldF9rZXlfZm9yX2p3dF90b2tlbl8yNTZiaXRzIQ==";
    },
    configureDatabase: options =>
    {
        options.ConnectionString = connectionString;
    });

builder.Services.AddHostedService<DatabaseMigrationBackgroundService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rejka API v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthorization();

app.MapControllers();

app.Run();
