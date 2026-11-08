using backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddInfrastructure(options =>
{
    options.Issuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? string.Empty;
    options.Audience = builder.Configuration["Authentication:Jwt:Audience"] ?? string.Empty;
    options.SigningKey = builder.Configuration["Authentication:Jwt:SigningKey"] ?? string.Empty;
});
builder.Services.AddSingleton(TimeProvider.System);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
