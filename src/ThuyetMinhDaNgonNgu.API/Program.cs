using Microsoft.Data.Sqlite;
using ThuyetMinhDaNgonNgu.DAL.ExternalServices;
using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Services;
using ThuyetMinhDaNgonNgu.DAL.Persistence;
using ThuyetMinhDaNgonNgu.DAL.Repositories;
using ThuyetMinhDaNgonNgu.DAL.SeedData;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Resolve the SQLite database path relative to the API project, not the shell's working directory.
var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/multilingual-narration.db";
var sqliteConnectionString = new SqliteConnectionStringBuilder(configuredConnectionString);
if (!Path.IsPathRooted(sqliteConnectionString.DataSource))
{
    sqliteConnectionString.DataSource = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, sqliteConnectionString.DataSource));
}

var databaseDirectory = Path.GetDirectoryName(sqliteConnectionString.DataSource);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
{
    Directory.CreateDirectory(databaseDirectory);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionString.ConnectionString));

// Data Access Layer: repository implementations now use SQLite instead of in-memory lists.
builder.Services.AddScoped<ILocationRepository, SqliteLocationRepository>();
builder.Services.AddScoped<ILanguageRepository, SqliteLanguageRepository>();

// Business Logic Layer.
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<INarrationService, NarrationService>();


builder.Services.AddHttpClient<IAiNarrationProvider, AIProvider>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<IAudioNarrationProvider, GeminiAudioProvider>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

var app = builder.Build();


// Create the database and seed sample data only when each table is empty.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseSeeder.Initialize(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
