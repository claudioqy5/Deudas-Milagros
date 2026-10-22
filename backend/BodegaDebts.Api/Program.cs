using BodegaDebts.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB Service
builder.Services.AddSingleton<MongoDbService>();

// Configure CORS for Vue Vite frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed initial database data if needed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
        await DataSeeder.SeedAsync(dbService);
        Console.WriteLine("[BodegaDebts] Database initialized and verified successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[BodegaDebts] Database initialization warning: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
