using Microsoft.EntityFrameworkCore;
using DocumentManagementSystem.Data;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Repositories;

var builder = WebApplication.CreateBuilder(args);

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL (nimmt ConnectionString "DefaultConnection")
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

// Statische Dateien aktivieren (für Frontend)
app.UseStaticFiles();

// Swagger IMMER aktivieren (auch im Container/Production)
app.UseSwagger();
app.UseSwaggerUI();

// DB-Migrationen beim Start ausführen
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        await db.Database.MigrateAsync();
        Console.WriteLine("Database created successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
    }
}

// Controller-Routing
app.MapControllers();

// Frontend routing - Root redirected zu index.html
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/health", () => "Healthy");

Console.WriteLine("API started on http://localhost:8080");
Console.WriteLine("Frontend available at: http://localhost:8080/");

app.Run();