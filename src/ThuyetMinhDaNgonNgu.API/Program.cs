using ThuyetMinhDaNgonNgu.DAL.ExternalServices;
using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Services;
using ThuyetMinhDaNgonNgu.DAL.Persistence;
using ThuyetMinhDaNgonNgu.DAL.Repositories;
using ThuyetMinhDaNgonNgu.DAL.SeedData;
using ThuyetMinhDaNgonNgu.DAL.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Supabase
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:DefaultConnection.");
}

string RequireSetting(string key)
{
    var value = builder.Configuration[key];
    return !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException($"Chưa cấu hình {key}.");
}

RequireSetting("Gemini:ApiKey");
RequireSetting("Supabase:Url");
RequireSetting("Supabase:ServiceRoleKey");
RequireSetting("Admin:Username");
RequireSetting("Admin:Password");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, postgres =>
    {
        postgres.CommandTimeout(30);
        postgres.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    }));

builder.Services.AddScoped<ILocationRepository, PostgresLocationRepository>();
builder.Services.AddScoped<ILanguageRepository, PostgresLanguageRepository>();
builder.Services.AddScoped<INarrationRepository, PostgresNarrationRepository>();
builder.Services.AddHttpClient<IAudioStorage, SupabaseAudioStorage>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

// Business Logic Layer.
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<INarrationService, NarrationService>();
builder.Services.AddScoped<IAdminNarrationService, AdminNarrationService>();


builder.Services.AddHttpClient<IAiNarrationProvider, AIProvider>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<IAudioNarrationProvider, GeminiAudioProvider>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

string signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:SigningKey.");
if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey phải có tối thiểu 32 byte.");
}

string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VietGuide";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VietGuideAdmin";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Narration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("AdminLogin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Apply versioned schema changes and seed only missing sample data.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.InitializeAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
