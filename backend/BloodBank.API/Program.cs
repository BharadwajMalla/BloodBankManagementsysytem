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
using BloodBank.API.Middleware;
using Google.GenAI;
using Google.GenAI.Types;

// 1. MUST EXECUTE BEFORE BUILDER INITIALIZATION:
// Disables ASP.NET Core legacy WS-Federation/Identity claim mapping so 'role' remains 'role'
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Dynamically listen on cloud host PORT environment variable (Render, Railway, Cloud Run)
var portEnv = System.Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://+:{portEnv}");
}

// 2. Database Configuration (SQL Server only)
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection must contain a SQL Server connection string.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(rawConnectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// 3. Register Dapper SQL Factory, Repositories & Domain Services
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(rawConnectionString));
builder.Services.AddSingleton<ISqlExceptionLogger, SqlExceptionLogger>();
var geminiOptions = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
    ?? new GeminiOptions();
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? builder.Configuration["GEMINI_API_KEY"]
    ?? System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrWhiteSpace(geminiApiKey))
{
    throw new InvalidOperationException(
        "Gemini API key is required. Set Gemini:ApiKey with .NET User Secrets or provide the GEMINI_API_KEY environment variable.");
}
if (string.IsNullOrWhiteSpace(geminiOptions.GenerationModel)
    || string.IsNullOrWhiteSpace(geminiOptions.EmbeddingModel)
    || geminiOptions.EmbeddingBatchDelayMilliseconds < 0)
{
    throw new InvalidOperationException(
        "Gemini generation/embedding model names must be configured and embedding batch delay cannot be negative.");
}

// Rotate any previously exposed Gemini key and keep credentials in User Secrets or environment variables only.
builder.Services.AddSingleton(geminiOptions);
builder.Services.AddSingleton<Client>(_ => new Client(
    apiKey: geminiApiKey,
    httpOptions: new HttpOptions
    {
        RetryOptions = new HttpRetryOptions
        {
            Attempts = 4,
            InitialDelay = 1.0f,
            MaxDelay = 20.0f,
            ExpBase = 2.0f,
            Jitter = 0.25f,
            HttpStatusCodes = [408, 429, 500, 502, 503, 504]
        }
    }));
builder.Services.AddSingleton<IGenerativeAiService, GeminiGenerativeAiService>();
builder.Services.AddScoped<IBloodBankRepository, DapperBloodBankRepository>();
builder.Services.AddScoped<ILoginAuditRepository, DapperLoginAuditRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IInventoryChatService, InventoryChatService>();
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
    return new MasterDataSchemaInitializer(connectionFactory.CreateConnection);
});
builder.Services.AddScoped<IMasterDataRepository>(services =>
    new DapperMasterDataRepository(
        () => services.GetRequiredService<IDbConnectionFactory>().CreateConnection()));
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
        OnAuthenticationFailed = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
            var exceptionLogger = context.HttpContext.RequestServices.GetRequiredService<ISqlExceptionLogger>();
            await exceptionLogger.LogAsync(context.Exception, "JwtBearer authentication");
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

app.UseMiddleware<SqlExceptionHandlingMiddleware>();

// 8. Auto-Migrations and Seeding on application startup
await DbInitializer.InitializeAsync(app.Services, app.Logger);
try
{
    await app.Services.GetRequiredService<MasterDataSchemaInitializer>()
        .InitializeAsync(app.Lifetime.ApplicationStopping);
}
catch (Exception ex)
{
    await app.Services.GetRequiredService<ISqlExceptionLogger>()
        .LogAsync(ex, "Master data schema initialization");
    throw;
}

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

app.MapGet("/api/health", async (
    IDbConnectionFactory connectionFactory,
    ISqlExceptionLogger exceptionLogger) =>
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
        await exceptionLogger.LogAsync(ex, "Health check");
        connectionError = $"{ex.GetType().Name}: {ex.Message}" + (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "");
    }

    return Results.Ok(new
    {
        status = canConnect ? "Healthy" : "Degraded",
        databaseConnected = canConnect,
        provider = "SqlServer",
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
    try
    {
        await DbInitializer.SeedLiveDataAsync(seedRepository);
    }
    catch (Exception ex)
    {
        await app.Services.GetRequiredService<ISqlExceptionLogger>()
            .LogAsync(ex, "Seed live data");
        throw;
    }
    Console.WriteLine("SEED_COMPLETE_SUCCESS");
    return;
}

app.Run();
