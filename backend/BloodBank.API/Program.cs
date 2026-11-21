using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Dapper;
using BloodBank.Application.Contracts;
using BloodBank.Application.Services;
using BloodBank.API.Data;
using BloodBank.API.Repositories;
using BloodBank.API.Services;
using BloodBank.Infrastructure.Repositories;
using BloodBank.Infrastructure.Security;
using BloodBank.Infrastructure.Migrations;

// 1. MUST EXECUTE BEFORE BUILDER INITIALIZATION:
// Disables ASP.NET Core legacy WS-Federation/Identity claim mapping so 'role' remains 'role'
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Dynamically listen on cloud host PORT environment variable (Render, Railway, Cloud Run)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://+:{portEnv}");
}

// 2. Database Configuration (EF Core with SQL Server & Smart SQLite Failover)
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnectionString) || rawConnectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
{
    rawConnectionString = "Data Source=bloodbank.db";
}

bool useSqlite = false;
string activeDbProvider = "SqlServer";

if (rawConnectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) || rawConnectionString.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    useSqlite = true;
    activeDbProvider = "Sqlite";
}
else
{
    useSqlite = false;
    activeDbProvider = "SqlServer";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite("Data Source=bloodbank.db");
    }
    else
    {
        options.UseSqlServer(rawConnectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    }
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// 3. Register Dapper SQL Factory, Repositories & Domain Services
var dapperConnectionString = useSqlite ? "Data Source=bloodbank.db" : rawConnectionString;
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(dapperConnectionString, useSqlite));
builder.Services.AddScoped<IBloodBankRepository, DapperBloodBankRepository>();
builder.Services.AddScoped<ILoginAuditRepository, DapperLoginAuditRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IBloodBankService, BloodBankService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IBloodInventoryService, BloodInventoryService>();
builder.Services.AddScoped<IDonorDonationService, DonorDonationService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IDonorIdGenerator>(services =>
    new DapperDonorIdGenerator(
        () => services.GetRequiredService<IDbConnectionFactory>().CreateConnection()));
builder.Services.AddScoped<IDonorDonationRepository>(services =>
    new DapperDonorDonationRepository(
        () => services.GetRequiredService<IDbConnectionFactory>().CreateConnection(),
        services.GetRequiredService<IDonorIdGenerator>()));
builder.Services.AddSingleton<MasterDataSchemaInitializer>(services =>
{
    var connectionFactory = services.GetRequiredService<IDbConnectionFactory>();
    return new MasterDataSchemaInitializer(connectionFactory.CreateConnection, useSqlite);
});
builder.Services.AddScoped<IMasterDataRepository>(services =>
    new DapperMasterDataRepository(
        () => services.GetRequiredService<IDbConnectionFactory>().CreateConnection(),
        useSqlite));
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

// 4. Authentication & Security Configuration
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? "SuperSecretProductionKey_BloodBankA_2026_LiveKey_FreeDeployment!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BloodBankSaaS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BloodBankClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role",
        NameClaimType = "name"
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                var role = identity.FindFirst("role")?.Value 
                    ?? identity.FindFirst(ClaimTypes.Role)?.Value 
                    ?? identity.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                if (!string.IsNullOrEmpty(role))
                {
                    if (!identity.HasClaim(c => c.Type == "role"))
                        identity.AddClaim(new Claim("role", role));
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    if (!identity.HasClaim(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
                        identity.AddClaim(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 5. CORS Configuration (Allows Localhost, Vercel Production & Previews)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            var uri = new Uri(origin);
            return uri.Host == "localhost"
                || uri.Host == "127.0.0.1"
                || uri.Host.EndsWith("vercel.app") 
                || uri.Host.EndsWith("onrender.com");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 6. Controllers & API Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = 
            System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddEndpointsApiExplorer();

// 7. Swagger/OpenAPI Configuration with OpenApiSecuritySchemeReference("Bearer", document)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blood Bank A Management System API",
        Version = "v1",
        Description = "Single-facility Blood Bank A Management System with 3 distinct controllers for Blood Bank, Donor, and Hospital."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: Bearer <your-token>"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// 8. Auto-Migrations and Seeding on application startup
await DbInitializer.InitializeAsync(app.Services, app.Logger);
await app.Services.GetRequiredService<MasterDataSchemaInitializer>()
    .InitializeAsync(app.Lifetime.ApplicationStopping);

// 9. HTTP Request Pipeline Configuration
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blood Bank A API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontendApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", async (IDbConnectionFactory connectionFactory) =>
{
    string? connectionError = null;
    bool canConnect = false;
    int userCount = -1;

    try
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        canConnect = true;
        userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users");
    }
    catch (Exception ex)
    {
        connectionError = $"{ex.GetType().Name}: {ex.Message}" + (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "");
    }

    return Results.Ok(new
    {
        status = canConnect ? "Healthy" : "Degraded",
        databaseConnected = canConnect,
        provider = activeDbProvider,
        userCount = userCount,
        connectionError = connectionError,
        serverUtc = DateTime.UtcNow
    });
}).AllowAnonymous();

app.MapControllers();

if (args.Contains("--seed-data"))
{
    using var seedScope = app.Services.CreateScope();
    var seedRepository = seedScope.ServiceProvider.GetRequiredService<IBloodBankRepository>();
    await DbInitializer.SeedLiveDataAsync(seedRepository);
    Console.WriteLine("SEED_COMPLETE_SUCCESS");
    return;
}

app.Run();
