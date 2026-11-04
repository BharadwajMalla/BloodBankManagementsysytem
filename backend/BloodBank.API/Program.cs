using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using BloodBank.API.Data;
using BloodBank.API.Repositories;
using BloodBank.API.Services;

// 1. MUST EXECUTE BEFORE BUILDER INITIALIZATION:
// Disables ASP.NET Core legacy WS-Federation/Identity claim mapping so 'role' remains 'role'
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// 2. Database Configuration (EF Core with SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
{
    connectionString = "Server=db78011.public.databaseasp.net; Database=db78011; User Id=db78011; Password=Mr3_@2EqRh7=; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    // Suppress PendingModelChangesWarning for dynamic runtime synchronization
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// 3. Register Domain Services & Repositories
builder.Services.AddScoped<IBloodBankRepository, EfBloodBankRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBloodBankService, BloodBankService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IBloodInventoryService, BloodInventoryService>();

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

app.MapGet("/api/health", async (ApplicationDbContext db) =>
{
    string? connectionError = null;
    bool canConnect = false;
    int userCount = -1;

    try
    {
        using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        canConnect = true;
        userCount = await db.Users.CountAsync();
    }
    catch (Exception ex)
    {
        connectionError = $"{ex.GetType().Name}: {ex.Message}" + (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "");
    }

    return Results.Ok(new
    {
        status = canConnect ? "Healthy" : "Degraded",
        databaseConnected = canConnect,
        userCount = userCount,
        connectionError = connectionError,
        serverUtc = DateTime.UtcNow
    });
}).AllowAnonymous();

app.MapControllers();

app.Run();
