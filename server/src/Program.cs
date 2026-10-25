using Serilog;
using ArenaServer.Database;
using ArenaServer.Services;
using ArenaServer.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/arena-server-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not configured");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(connectionString));

// Game Services
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddSingleton<ArenaService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<MatchmakingService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<CombatService>();

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.Migrate();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// SignalR hubs
app.MapHub<LobbyHub>("/hubs/lobby");
app.MapHub<ArenaHub>("/hubs/arena");

// API routes
app.MapControllers();

// Health check
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
