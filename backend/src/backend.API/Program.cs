using System.Text;
using backend.API;
using backend.Infrastructure;
using backend.Infrastructure.Analysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<PlateRecognitionOptions>(builder.Configuration.GetSection("PlateRecognition"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:8000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? string.Empty;

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "RejkaApi";
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "RejkaApp";
var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"] ?? "dGhpc19pc19hX3Zlcnlfc3Ryb25nX3NlY3JldF9rZXlfZm9yX2p3dF90b2tlbl8yNTZiaXRzIQ==";

var blobConnectionString = builder.Configuration["BlobStorage:ConnectionString"]
    ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
var blobContainerName = builder.Configuration["BlobStorage:ContainerName"] ?? "rejka-media";

builder.Services.AddInfrastructure(
    configureJwt: options =>
    {
        options.Issuer = jwtIssuer;
        options.Audience = jwtAudience;
        options.SigningKey = jwtSigningKey;
    },
    configureDatabase: options =>
    {
        options.ConnectionString = connectionString;
    },
    configureBlobStorage: options =>
    {
        options.ConnectionString = blobConnectionString;
        options.ContainerName = blobContainerName;
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHostedService<DatabaseMigrationBackgroundService>();
builder.Services.AddHostedService<MediaAnalysisWorker>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Rejka API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wprowadź token JWT (np. z /account/login)"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rejka API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
